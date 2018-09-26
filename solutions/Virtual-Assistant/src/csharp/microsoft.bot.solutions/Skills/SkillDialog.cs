using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Middleware;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Skills
{
    public class SkillDialog : Dialog
    {
        // Constants
        private const string ActiveSkillStateKey = "ActiveSkill";
        private const string TokenRequestEventName = "tokens/request";
        private const string TokenResponseEventName = "tokens/response";
        private const string SkillBeginEventName = "skillBegin";

        // Fields
        private InProcAdapter _inProcAdapter;
        private IBot _activatedSkill;
        private CosmosDbStorageOptions _cosmosDbOptions;
        private TelemetryClient _telemetryClient;
        private SkillService _skill;
        private OAuthPrompt _authPrompt;
        private bool skillInitialized = false;


        public SkillDialog(CosmosDbStorageOptions cosmosDbOptions, TelemetryClient telemetryClient)
            : base(nameof(SkillDialog))
        {
            _cosmosDbOptions = cosmosDbOptions;
            _telemetryClient = telemetryClient;
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var skillDialogOptions = (SkillDialogOptions)options;

            _skill = skillDialogOptions.MatchedSkill;

            // Set our active Skill so later methods know which Skill to use.
            dc.ActiveDialog.State[ActiveSkillStateKey] = skillDialogOptions.MatchedSkill;

            _authPrompt = new OAuthPrompt(nameof(OAuthPrompt), new OAuthPromptSettings()
            {
                ConnectionName = skillDialogOptions.MatchedSkill.AuthConnectionName,
                Title = "Skill Authentication",
                Text = $"Please login to access this feature.",
            });

            var parameters = new Dictionary<string, object>();
            if (skillDialogOptions.MatchedSkill.Parameters != null)
            {
                foreach (var parameter in skillDialogOptions.MatchedSkill.Parameters)
                {
                    if (skillDialogOptions.Parameters.TryGetValue(parameter, out var paramValue))
                    {
                        parameters.Add(parameter, paramValue);
                    }
                }
            }

            var skillMetadata = new SkillMetadata(
                skillDialogOptions.LuisResult,
                skillDialogOptions.LuisService,
                skillDialogOptions.MatchedSkill.Configuration,
                parameters);

            var dialogBeginEvent = new Activity(
                type: ActivityTypes.Event,
                channelId: dc.Context.Activity.ChannelId,
                from: new ChannelAccount(id: dc.Context.Activity.From.Id, name: dc.Context.Activity.From.Name),
                recipient: new ChannelAccount(id: dc.Context.Activity.Recipient.Id, name: dc.Context.Activity.Recipient.Name),
                conversation: new ConversationAccount(id: dc.Context.Activity.Conversation.Id),
                name: SkillBeginEventName,
                value: skillMetadata);

            return await ForwardToSkill(dc, dialogBeginEvent);
        }

        public override Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ForwardToSkill(dc, dc.Context.Activity);
        }

        private bool InitializeSkill(DialogContext dc)
        {
            if (dc.ActiveDialog.State.ContainsKey(ActiveSkillStateKey))
            {
                var skill = dc.ActiveDialog.State[ActiveSkillStateKey] as SkillService;

                var cosmosDbOptions = _cosmosDbOptions;
                cosmosDbOptions.CollectionId = skill.Name;
                var cosmosDbStorage = new CosmosDbStorage(cosmosDbOptions);
                var conversationState = new ConversationState(cosmosDbStorage);

                try
                {
                    var skillType = Type.GetType(skill.Assembly);
                    _activatedSkill = (IBot)Activator.CreateInstance(skillType, conversationState, $"{skill.Name}State", skill.Configuration);
                }
                catch (Exception e)
                {
                    var message = $"Skill ({skill.Name}) Type could not be created.";
                    throw new InvalidOperationException(message, e);
                }

                _inProcAdapter = new InProcAdapter
                {
                    OnTurnError = async (context, exception) =>
                    {
                        await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: exception.Message));
                        await context.SendActivityAsync(context.Activity.CreateReply($"Sorry, something went wrong trying to communicate with the skill. Please try again."));
                        await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Skill Error: {exception.Message} | {exception.StackTrace}"));
                        _telemetryClient.TrackException(exception);
                    },
                };

                _inProcAdapter.Use(new EventDebuggerMiddleware());
                _inProcAdapter.Use(new AutoSaveStateMiddleware(conversationState));

                skillInitialized = true;
            }
            else
            {
                // No active skill?
            }

            return skillInitialized;
        }

        public async Task<DialogTurnResult> ForwardToSkill(DialogContext dc, Activity activity)
        {
            if (!skillInitialized)
            {
                InitializeSkill(dc);
            }

            _inProcAdapter.ProcessActivity(activity, async (skillContext, ct) =>
            {
                await _activatedSkill.OnTurnAsync(skillContext);
            }).Wait();

            var queue = new List<Activity>();
            var endOfConversation = false;
            var skillResponse = _inProcAdapter.GetNextReply();

            while (skillResponse != null)
            {
                if (skillResponse.Type == ActivityTypes.EndOfConversation)
                {
                    endOfConversation = true;
                }
                else if (skillResponse?.Name == TokenRequestEventName)
                {
                    // Send trace to emulator
                    await dc.Context.SendActivityAsync(
                        new Activity(
                            type: ActivityTypes.Trace,
                            text: $"<--Received a Token Request from a skill"
                            ));

                    // Uncomment this line to prompt user for login every time the skill requests a token
                    // var a = dc.Context.Adapter as BotFrameworkAdapter;
                    // await a.SignOutUserAsync(dc.Context, _skill.AuthConnectionName, dc.Context.Activity.From.Id, default(CancellationToken));

                    var authResult = await _authPrompt.BeginDialogAsync(dc);
                    if (authResult.Result?.GetType() == typeof(TokenResponse))
                    {
                        var tokenEvent = skillResponse.CreateReply();
                        tokenEvent.Type = ActivityTypes.Event;
                        tokenEvent.Name = TokenResponseEventName;
                        tokenEvent.Value = authResult.Result;

                        return await ForwardToSkill(dc, tokenEvent);
                    }
                    else
                    {
                        return authResult;
                    }
                }
                else
                {
                    queue.Add(skillResponse);
                }

                skillResponse = _inProcAdapter.GetNextReply();
            }

            // send skill queue to User
            if (queue.Count > 0)
            {
                await dc.Context.SendActivitiesAsync(queue.ToArray());
            }

            // handle ending the skill conversation
            if (endOfConversation)
            {
                return await dc.EndDialogAsync();
            }
            else
            {
                return EndOfTurn;
            }
        }
    }
}
