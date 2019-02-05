using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Middleware;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Responses;

namespace Microsoft.Bot.Solutions.Skills
{
    public class SkillDialog : Dialog
    {
        // Constants
        private const string ActiveSkillStateKey = "ActiveSkill";

        // Fields
        private Dictionary<string, SkillConfigurationBase> _skills;
        private ResponseManager _responseManager;
        private IStatePropertyAccessor<DialogState> _accessor;
        private EndpointService _endpointService;
        private IBotTelemetryClient _telemetryClient;
        private DialogSet _dialogs;
        private InProcAdapter _inProcAdapter;
        private IBot _activatedSkill;
        private bool _skillInitialized;
        private bool _useCachedTokens;

        public SkillDialog(Dictionary<string, SkillConfigurationBase> skills, IStatePropertyAccessor<DialogState> accessor, EndpointService endpointService, IBotTelemetryClient telemetryClient, bool useCachedTokens = true)
            : base(nameof(SkillDialog))
        {
            _skills = skills;
            _accessor = accessor;
            _endpointService = endpointService;
            _telemetryClient = telemetryClient;
            _useCachedTokens = useCachedTokens;
            _dialogs = new DialogSet(_accessor);
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var skillOptions = (SkillDialogOptions)options;

            // Save the active skill in state
            var skillDefinition = skillOptions.SkillDefinition;
            dc.ActiveDialog.State[ActiveSkillStateKey] = skillDefinition;

            var skillConfiguration = _skills[skillDefinition.Id];

            // Initialize authentication prompt
            _dialogs = _dialogs ?? new DialogSet(_accessor);
            _dialogs.Add(new MultiProviderAuthDialog(skillConfiguration));

            // Send parameters to skill in skillBegin event
            var userData = new Dictionary<string, object>();

            if (skillDefinition.Parameters != null)
            {
                foreach (var parameter in skillDefinition.Parameters)
                {
                    if (skillOptions.Parameters.TryGetValue(parameter, out var paramValue))
                    {
                        userData.TryAdd(parameter, paramValue);
                    }
                }
            }

            var activity = dc.Context.Activity;

            var skillBeginEvent = new Activity(
              type: ActivityTypes.Event,
              channelId: activity.ChannelId,
              from: new ChannelAccount(id: activity.From.Id, name: activity.From.Name),
              recipient: new ChannelAccount(id: activity.Recipient.Id, name: activity.Recipient.Name),
              conversation: new ConversationAccount(id: activity.Conversation.Id),
              name: Events.SkillBeginEventName,
              value: userData);

            // Send event to Skill/Bot
            return await ForwardToSkill(dc, skillBeginEvent);
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = dc.Context.Activity;
            var innerDc = await _dialogs.CreateContextAsync(dc.Context);

            // Add the oauth prompt to _dialogs if it is missing
            var dialog = _dialogs.Find(nameof(MultiProviderAuthDialog));
            if (dialog == null)
            {
                var skillDefinition = dc.ActiveDialog.State[ActiveSkillStateKey] as SkillDefinition;
                var skillConfiguration = _skills[skillDefinition.Id];

                _dialogs.Add(new MultiProviderAuthDialog(skillConfiguration));
            }

            // Check if we're in the oauth prompt
            if (innerDc.ActiveDialog != null)
            {
                // Handle magic code auth
                var result = await innerDc.ContinueDialogAsync();

                // forward the token response to the skill
                if (result.Status == DialogTurnStatus.Complete && result.Result is ProviderTokenResponse)
                {
                    activity.Type = ActivityTypes.Event;
                    activity.Name = Events.TokenResponseEventName;
                    activity.Value = result.Result as ProviderTokenResponse;
                }
                else
                {
                    return result;
                }
            }

            return await ForwardToSkill(dc, activity);
        }

        private async Task InitializeSkill(DialogContext dc)
        {
            try
            {
                var skillDefinition = dc.ActiveDialog.State[ActiveSkillStateKey] as SkillDefinition;
                var skillConfiguration = _skills[skillDefinition.Id];

                IStorage storage;

                if (skillConfiguration.CosmosDbOptions != null)
                {
                    var cosmosDbOptions = skillConfiguration.CosmosDbOptions;
                    cosmosDbOptions.CollectionId = skillDefinition.Name;
                    storage = new CosmosDbStorage(cosmosDbOptions);
                }
                else
                {
                    storage = new MemoryStorage();
                }

                // Initialize skill state
                var userState = new UserState(storage);
                var conversationState = new ConversationState(storage);

                // Create skill instance
                try
                {
                    var skillType = Type.GetType(skillDefinition.Assembly);
                    _activatedSkill = (IBot)Activator.CreateInstance(skillType, skillConfiguration, conversationState, userState, _telemetryClient, null, null, true);
                }
                catch (Exception e)
                {
                    var message = $"Skill ({skillDefinition.Name}) could not be created.";
                    throw new InvalidOperationException(message, e);
                }

                _inProcAdapter = new InProcAdapter
                {
                    // set up skill turn error handling
                    OnTurnError = async (context, exception) =>
                    {
                        await context.SendActivityAsync(CommonResponses.ErrorMessage_SkillError);

                        await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Skill Error: {exception.Message} | {exception.StackTrace}"));

                        // Log exception in AppInsights
                        _telemetryClient.TrackExceptionEx(exception, context.Activity);
                    },
                };

                _inProcAdapter.Use(new EventDebuggerMiddleware());
                _inProcAdapter.Use(new SetLocaleMiddleware(dc.Context.Activity.Locale ?? "en-us"));
                _inProcAdapter.Use(new AutoSaveStateMiddleware(userState, conversationState));
                _skillInitialized = true;
            }
            catch
            {
                // something went wrong initializing the skill, so end dialog cleanly and throw so the error is logged
                _skillInitialized = false;
                await dc.EndDialogAsync();
                throw;
            }
        }

        private async Task<DialogTurnResult> ForwardToSkill(DialogContext dc, Activity activity)
        {
            try
            {
                if (!_skillInitialized)
                {
                    await InitializeSkill(dc);
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
                    else if (skillResponse?.Name == Events.TokenRequestEventName)
                    {
                        // Send trace to emulator
                        await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"<--Received a Token Request from a skill"));

                        if (!_useCachedTokens)
                        {
                            var adapter = dc.Context.Adapter as BotFrameworkAdapter;
                            var tokens = await adapter.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);

                            foreach (var token in tokens)
                            {
                                await adapter.SignOutUserAsync(dc.Context, token.ConnectionName, dc.Context.Activity.From.Id, default(CancellationToken));
                            }
                        }

                        var innerDc = await _dialogs.CreateContextAsync(dc.Context);
                        var authResult = await innerDc.BeginDialogAsync(nameof(MultiProviderAuthDialog));

                        if (authResult.Result?.GetType() == typeof(ProviderTokenResponse))
                        {
                            var tokenEvent = skillResponse.CreateReply();
                            tokenEvent.Type = ActivityTypes.Event;
                            tokenEvent.Name = Events.TokenResponseEventName;
                            tokenEvent.Value = authResult.Result as ProviderTokenResponse;

                            return await ForwardToSkill(dc, tokenEvent);
                        }
                        else
                        {
                            return authResult;
                        }
                    }
                    else
                    {
                        if (skillResponse.Type == ActivityTypes.Trace)
                        {
                            // Write out any trace messages from the skill to the emulator
                            await dc.Context.SendActivityAsync(skillResponse);
                        }
                        else
                        {
                            queue.Add(skillResponse);
                        }
                    }

                    skillResponse = _inProcAdapter.GetNextReply();
                }

                // send skill queue to User
                if (queue.Count > 0)
                {
                    var firstActivity = queue[0];
                    if (firstActivity.Conversation.Id == dc.Context.Activity.Conversation.Id)
                    {
                        // if the conversation id from the activity is the same as the context activity, it's reactive message
                        await dc.Context.SendActivitiesAsync(queue.ToArray());
                    }
                    else
                    {
                        // if the conversation id from the activity is differnt from the context activity, it's proactive message
                        await dc.Context.Adapter.ContinueConversationAsync(_endpointService.AppId, firstActivity.GetConversationReference(), CreateCallback(queue.ToArray()), default(CancellationToken));
                    }
                }

                // handle ending the skill conversation
                if (endOfConversation)
                {
                    await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"<--Ending the skill conversation"));

                    return await dc.EndDialogAsync();
                }
                else
                {
                    return EndOfTurn;
                }
            }
            catch
            {
                // something went wrong forwarding to the skill, so end dialog cleanly and throw so the error is logged.
                // NOTE: errors within the skill itself are handled by the OnTurnError handler on the adapter.
                await dc.EndDialogAsync();
                throw;
            }
        }

        private BotCallbackHandler CreateCallback(Activity[] activities)
        {
            return async (turnContext, token) =>
            {
                // Send back the activities in the proactive context
                await turnContext.SendActivitiesAsync(activities, token);
            };
        }

        private class Events
        {
            public const string SkillBeginEventName = "skillBegin";
            public const string TokenRequestEventName = "tokens/request";
            public const string TokenResponseEventName = "tokens/response";
        }
    }
}