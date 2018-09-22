namespace Microsoft.Bot.Solutions.Skills
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Azure;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Solutions.Middleware;

    /// <summary>
    /// Custom Dialog implementation to hand Conversational Skill Semantics. We want to invoke skills "in process" rather than invoking through
    /// DirectLine which brings overhead/indirection including heavyweight security which is not desirable for this scenario.
    /// TODO - Consider refactoring to introduce a SkillConnector in partnership with a SkillDialog.
    /// </summary>
    public class SkillDialog : Dialog
    {
        // NOTE: There is no inner stack to a skill dialog
        // Therefore, there is null error on begin and continue, which expect an active inner dialog
        // How can this be refactored to include interruption
        private const string ActiveSkillStateKey = "ActiveSkill";
        private const string TokenRequestEventName = "tokens/request";
        private const string TokenResponseEventName = "tokens/response";
        private const string SkillBeginEventName = "skillBegin";

        private static CosmosDbStorageOptions _cosmosDbOptions;
        private TelemetryClient _telemetryClient;
        private InProcAdapter inProcAdapter;
        private IBot activatedSkill;
        private bool skillInitialized = false;

        // We need access to Settings in order to prime the CosmosDb Storage and avoid having to store in local state
        public SkillDialog(CosmosDbStorageOptions cosmosDbOptions, TelemetryClient telemetryClient)
            : base(nameof(SkillDialog))
        {
            _cosmosDbOptions = cosmosDbOptions;
            _telemetryClient = telemetryClient;
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken)
        {
            var skillDialogOptions = (SkillDialogOptions)options;

            // Set our active Skill so later methods know which Skill to use.
            dc.ActiveDialog.State[ActiveSkillStateKey] = skillDialogOptions.MatchedSkill;

            // If Parameters are requested by a Skill we try to resolve them from the UserInformation storage
            // If they aren't present (a valid scenario) we don't pass
            var parameters = new Dictionary<string, object>();
            if (skillDialogOptions.MatchedSkill.Parameters != null)
            {
                foreach (var parameter in skillDialogOptions.MatchedSkill.Parameters)
                {
                    if (skillDialogOptions.UserInfo.TryGetValue(parameter, out var paramValue))
                    {
                        parameters.Add(parameter, paramValue);
                    }
                }
            }

            var skillMetadata = new SkillMetadata(skillDialogOptions.LuisResult,
                skillDialogOptions.MatchedSkill.Configuration,
                parameters);

            // Send a skillBegin event to the down-stream Skill/Bot and pass the Luis result already performed by the dispatcher to
            // shortcut processing by the Skill but also enables the Skill to be used directly by the emulator
            // We also send a SkilLMetadata object in the Value property to exchange configuration and user information
            var dialogBeginEvent = new Activity(
                type: ActivityTypes.Event,
                channelId: dc.Context.Activity.ChannelId,
                from: new ChannelAccount(id: dc.Context.Activity.From.Id, name: dc.Context.Activity.From.Name),
                recipient: new ChannelAccount(id: dc.Context.Activity.Recipient.Id, name: dc.Context.Activity.Recipient.Name),
                conversation: new ConversationAccount(id: dc.Context.Activity.Conversation.Id),
                name: SkillBeginEventName,
                value: skillMetadata);

            // Send event to Skill/Bot
            await ForwardActivity(dc, dialogBeginEvent);

            return EndOfTurn;
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            // Initialise now called as part of the constructor, review pattern.
            return await ForwardActivity(dc, dc.Context.Activity);
        }

        private bool InititializeSkill(DialogContext dc)
        {
            if (dc.ActiveDialog.State.ContainsKey(ActiveSkillStateKey))
            {
                var skill = dc.ActiveDialog.State[ActiveSkillStateKey] as SkillRegistration;

                var cosmosDbOptions = _cosmosDbOptions;

                // Isolate from the main Bot's storage and put skill state into it's own collection
                cosmosDbOptions.CollectionId = skill.Name;

                // The Skill state will be stored in our parent Bot storage
                var cosmosDbStorage = new CosmosDbStorage(cosmosDbOptions);
                var conversationState = new ConversationState(cosmosDbStorage);

                // Reflection is used to enable dynamic lookup of Skills and keep it configuration based
                // A configuration driven approach is elegant but Reflection is pretty heavyweight, explore DI
                try
                {
                    // Create the skill and crucially pass the provided Conversation State in through a new constructor specific to skill activation
                    var skillType = Type.GetType(skill.Assembly);
                    activatedSkill = (IBot)Activator.CreateInstance(skillType, conversationState, $"{skill.Name}State", skill.Configuration);
                }
                catch (Exception e)
                {
                    var message = $"Skill ({skill.Name}) Type could not be created.";

                    throw new InvalidOperationException(message, e);
                }

                // Initialise the Adapter and Middlware used to invoke Skills. We leverage the same Storage account as configured by the parent Bot otherwise the Parent Bot would need
                // To know about all of the storage information for each skill and cause an explosion of Skill state stores. This approach keeps things simple and ensures Skill state is owned/managed
                // By the parent Bot which feels the right way forward.
                // We create our own "collection" for Skills state.
                inProcAdapter = new InProcAdapter
                {
                    OnTurnError = async (context, exception) =>
                    {
                        await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: exception.Message));
                        await context.SendActivityAsync(context.Activity.CreateReply($"Sorry, something went wrong trying to communicate with the skill. Please try again."));
                        await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Skill Error: {exception.Message} | {exception.StackTrace}"));
                        _telemetryClient.TrackException(exception);
                    },
                };

                inProcAdapter.Use(new EventDebuggerMiddleware());

                // TODO: Send property bag across to skill from registration
                inProcAdapter.Use(new AutoSaveStateMiddleware(conversationState));

                skillInitialized = true;
            }
            else
            {
                // No active skill?
            }

            return skillInitialized;
        }

        private async Task<DialogTurnResult> ForwardActivity(DialogContext dc, Activity activity)
        {
            if (!skillInitialized)
            {
                InititializeSkill(dc);
            }

            // Process the activity (pass through middleware) and then perform Skill processing
            inProcAdapter.ProcessActivity(activity, async (skillContext, cancellationToken) =>
            {
                await activatedSkill.OnTurnAsync(skillContext);
            }).Wait();

            // Incurs a lock each time but given we need to inspect each one for EOC and filter them out this saves another collection of activities. Swings and roundabouts
            var filteredActivities = new List<Activity>();
            var endOfConversation = false;

            var replyActivity = inProcAdapter.GetNextReply();
            while (replyActivity != null)
            {
                if (replyActivity.Type == ActivityTypes.EndOfConversation)
                {
                    endOfConversation = true;
                }
                else if (replyActivity.Type == ActivityTypes.Event && replyActivity.Name == TokenRequestEventName)
                {
                    // Send trace to emulator
                    await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"<--Received a Token Request from a skill"));

                    var skill = dc.ActiveDialog.State[ActiveSkillStateKey] as SkillRegistration;

                    // If you want to force signin interactively and not cached uncomment this
                    // var a = dialogContext.Context.Adapter as BotFrameworkAdapter;
                    // await a.SignOutUserAsync(dialogContext.Context, skill.AuthConnectionName, default(CancellationToken));

                    // Skills could support multiple token types, only allow one through config for now.
                    var prompt = new OAuthPrompt(
                       "SkillAuth",
                       new OAuthPromptSettings()
                       {
                           ConnectionName = skill.AuthConnectionName,
                           Text = $"Please signin to provide an authentication token for the {skill.Name} skill",
                           Title = "Skill Authentication",
                           Timeout = 300000, // User has 5 minutes to login
                       },
                       AuthPromptValidator);

                    var tokenResponse = await prompt.GetUserTokenAsync(dc.Context);
                    if (tokenResponse != null)
                    {
                        var response = replyActivity.CreateReply();
                        response.Type = ActivityTypes.Event;
                        response.Name = TokenResponseEventName;
                        response.Value = tokenResponse;

                        var result = await ForwardActivity(dc, response);

                        if (result.Status == DialogTurnStatus.Complete)
                        {
                            endOfConversation = true;
                        }
                    }
                    else
                    {
                        var dtr = await prompt.BeginDialogAsync(dc);
                    }
                }
                else
                {
                    filteredActivities.Add(replyActivity);
                }

                replyActivity = inProcAdapter.GetNextReply();
            }

            if (filteredActivities.Count > 0)
            {
                await dc.Context.SendActivitiesAsync(filteredActivities.ToArray());
            }

            // If we got an End of Conversation then close this skill dialog down
            if (endOfConversation)
            {
                var state = dc.Context.TurnState;
                state[ActiveSkillStateKey] = null;
                skillInitialized = false;

                // Send trace to emulator
                await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"<--Ending the skill conversation"));
                return await dc.EndDialogAsync();
            }

            return EndOfTurn;
        }

        private Task<bool> AuthPromptValidator(PromptValidatorContext<TokenResponse> promptContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
