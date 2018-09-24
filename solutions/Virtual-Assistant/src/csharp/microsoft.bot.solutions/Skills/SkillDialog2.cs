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
    public class SkillDialog2 : Dialog
    {
        // Constants
        private const string ActiveSkillStateKey = "ActiveSkill";
        private const string SkillBeginEventName = "SkillBegin";

        // Fields
        private InProcAdapter inProcAdapter;
        private IBot activatedSkill;
        private CosmosDbStorageOptions _cosmosDbOptions;
        private TelemetryClient _telemetryClient;

        public SkillDialog2() : base(nameof(SkillDialog2))
        {
            // Set up CosmosDB Storage
            // Set up Telemetry Client
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            InitializeSkill(dc);

            var skillDialogOptions = (SkillDialogOptions)options;

            // Set our active Skill so later methods know which Skill to use.
            dc.ActiveDialog.State[ActiveSkillStateKey] = skillDialogOptions.MatchedSkill;

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

        public void InitializeSkill(DialogContext dc)
        {
            var skill = dc.ActiveDialog.State[ActiveSkillStateKey] as SkillRegistration;

            var cosmosDbOptions = _cosmosDbOptions;
            cosmosDbOptions.CollectionId = skill.Name;
            var cosmosDbStorage = new CosmosDbStorage(cosmosDbOptions);
            var conversationState = new ConversationState(cosmosDbStorage);

            try
            {
                var skillType = Type.GetType(skill.Assembly);
                activatedSkill = (IBot)Activator.CreateInstance(skillType, conversationState, $"{skill.Name}State", skill.Configuration);
            }
            catch (Exception e)
            {
                var message = $"Skill ({skill.Name}) Type could not be created.";
                throw new InvalidOperationException(message, e);
            }

            inProcAdapter = new InProcAdapter
            {
                OnTurnError = async (context, exception) =>
                {
                    await context.SendActivityAsync(context.Activity.CreateReply($"Sorry, something went wrong trying to communicate with the skill. Please try again."));
                    _telemetryClient.TrackException(exception);
                },
            };

            inProcAdapter.Use(new EventDebuggerMiddleware());
            inProcAdapter.Use(new AutoSaveStateMiddleware(conversationState));
        }

        public async Task<DialogTurnResult> ForwardToSkill(DialogContext dc, Activity activity)
        {
            inProcAdapter.ProcessActivity(activity, async (skillContext, ct) =>
            {
                await activatedSkill.OnTurnAsync(skillContext);
            }).Wait();

            var queue = new List<Activity>();
            var endOfConversation = false;
            var skillResponse = inProcAdapter.GetNextReply();

            while (skillResponse != null)
            {
                if (skillResponse.Type == ActivityTypes.EndOfConversation)
                {
                    endOfConversation = true;
                }
                else if (skillResponse.Type == ActivityTypes.Event && skillResponse.Name == TokenRequestEventName)
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
                    queue.Add(skillResponse);
                }

                skillResponse = inProcAdapter.GetNextReply();
            }

            // send skill queue to User
            if (queue.Count > 0)
            {
                await dc.Context.SendActivitiesAsync(queue.ToArray());
            }

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
