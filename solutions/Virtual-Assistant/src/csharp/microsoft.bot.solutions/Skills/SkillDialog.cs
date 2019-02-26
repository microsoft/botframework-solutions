﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Middleware;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Bot.Solutions.Telemetry;

namespace Microsoft.Bot.Solutions.Skills
{
    public class SkillDialog : ComponentDialog
    {
        // Fields
        private SkillDefinition _skillDefinition;
        private SkillConfigurationBase _skillConfiguration;
        private ResponseManager _responseManager;
        private EndpointService _endpointService;
        private ProactiveState _proactiveState;
        private IBotTelemetryClient _telemetryClient;
        private IBackgroundTaskQueue _backgroundTaskQueue;
        private InProcAdapter _inProcAdapter;
        private IBot _activatedSkill;
        private bool _skillInitialized;
        private bool _useCachedTokens;

        public SkillDialog(SkillDefinition skillDefinition, SkillConfigurationBase skillConfiguration, ProactiveState proactiveState, EndpointService endpointService, IBotTelemetryClient telemetryClient, IBackgroundTaskQueue backgroundTaskQueue, bool useCachedTokens = true)
            : base(skillDefinition.Id)
        {
            _skillDefinition = skillDefinition;
            _skillConfiguration = skillConfiguration;
            _proactiveState = proactiveState;
            _endpointService = endpointService;
            _telemetryClient = telemetryClient;
            _backgroundTaskQueue = backgroundTaskQueue;
            _useCachedTokens = useCachedTokens;

            var supportedLanguages = skillConfiguration.LocaleConfigurations.Keys.ToArray();
            _responseManager = new ResponseManager(supportedLanguages, new SkillResponses());

            AddDialog(new MultiProviderAuthDialog(skillConfiguration));
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var skillOptions = (SkillDialogOptions)options;

            // Send parameters to skill in skillBegin event
            var userData = new Dictionary<string, object>();

            if (_skillDefinition.Parameters != null)
            {
                foreach (var parameter in _skillDefinition.Parameters)
                {
                    if (skillOptions.Parameters.TryGetValue(parameter, out var paramValue))
                    {
                        userData.TryAdd(parameter, paramValue);
                    }
                }
            }

            var activity = innerDc.Context.Activity;

            var skillBeginEvent = new Activity(
              type: ActivityTypes.Event,
              channelId: activity.ChannelId,
              from: new ChannelAccount(id: activity.From.Id, name: activity.From.Name),
              recipient: new ChannelAccount(id: activity.Recipient.Id, name: activity.Recipient.Name),
              conversation: new ConversationAccount(id: activity.Conversation.Id),
              name: Events.SkillBeginEventName,
              value: userData);

            // Send event to Skill/Bot
            return await ForwardToSkill(innerDc, skillBeginEvent);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = innerDc.Context.Activity;

            if (innerDc.ActiveDialog?.Id == nameof(MultiProviderAuthDialog))
            {
                // Handle magic code auth
                var result = await innerDc.ContinueDialogAsync(cancellationToken);

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

            return await ForwardToSkill(innerDc, activity);
        }

        protected override Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            return outerDc.EndDialogAsync(result, cancellationToken);
        }

        private async Task InitializeSkill(DialogContext dc)
        {
            try
            {
                IStorage storage;

                if (_skillConfiguration.CosmosDbOptions != null)
                {
                    var cosmosDbOptions = _skillConfiguration.CosmosDbOptions;
                    cosmosDbOptions.CollectionId = _skillDefinition.Name;
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
                    var skillType = Type.GetType(_skillDefinition.Assembly);

                    // Have to use refined BindingFlags to allow for optional parameters on constructors.
                    _activatedSkill = (IBot)Activator.CreateInstance(
                        skillType,
                        BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance | BindingFlags.OptionalParamBinding,
                        default(Binder),
                        new object[] { _skillConfiguration, _endpointService, conversationState, userState, _proactiveState, _telemetryClient, _backgroundTaskQueue, true },
                        CultureInfo.CurrentCulture);
                }
                catch (Exception e)
                {
                    var message = $"Skill ({_skillDefinition.Name}) could not be created.";
                    throw new InvalidOperationException(message, e);
                }

                _inProcAdapter = new InProcAdapter
                {
                    // set up skill turn error handling
                    OnTurnError = async (context, exception) =>
                    {
                        await context.SendActivityAsync(_responseManager.GetResponse(SkillResponses.ErrorMessageSkillError));

                        await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Skill Error: {exception.Message} | {exception.StackTrace}"));

                        // Log exception in AppInsights
                        _telemetryClient.TrackExceptionEx(exception, context.Activity);
                    },
                    BackgroundTaskQueue = _backgroundTaskQueue
                };

                _inProcAdapter.Use(new EventDebuggerMiddleware());

                // change this to use default locale from appsettings when we have dependency injection
                var locale = "en-us";
                if (!string.IsNullOrWhiteSpace(dc.Context.Activity.Locale))
                {
                    locale = dc.Context.Activity.Locale;
                }

                _inProcAdapter.Use(new SetLocaleMiddleware(locale));

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

        private async Task<DialogTurnResult> ForwardToSkill(DialogContext innerDc, Activity activity)
        {
            try
            {
                if (!_skillInitialized)
                {
                    await InitializeSkill(innerDc);
                }

                _inProcAdapter.ProcessActivity(activity, async (skillContext, ct) =>
                {
                    await _activatedSkill.OnTurnAsync(skillContext);
                }, async (activities) =>
                {
                    foreach (var response in activities)
                    {
                        await innerDc.Context.Adapter.ContinueConversationAsync(_endpointService.AppId, response.GetConversationReference(), CreateCallback(response), default(CancellationToken));
                    }
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
                        await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"<--Received a Token Request from a skill"));

                        if (!_useCachedTokens)
                        {
                            var adapter = innerDc.Context.Adapter as BotFrameworkAdapter;
                            var tokens = await adapter.GetTokenStatusAsync(innerDc.Context, innerDc.Context.Activity.From.Id);

                            foreach (var token in tokens)
                            {
                                await adapter.SignOutUserAsync(innerDc.Context, token.ConnectionName, innerDc.Context.Activity.From.Id, default(CancellationToken));
                            }
                        }

                        var authResult = await innerDc.BeginDialogAsync(nameof(MultiProviderAuthDialog));

                        if (authResult.Result?.GetType() == typeof(ProviderTokenResponse))
                        {
                            var tokenEvent = skillResponse.CreateReply();
                            tokenEvent.Type = ActivityTypes.Event;
                            tokenEvent.Name = Events.TokenResponseEventName;
                            tokenEvent.Value = authResult.Result as ProviderTokenResponse;

                            return await ForwardToSkill(innerDc, tokenEvent);
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
                            await innerDc.Context.SendActivityAsync(skillResponse);
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
                    // if the conversation id from the activity is the same as the context activity, it's reactive message
                    await innerDc.Context.SendActivitiesAsync(queue.ToArray());
                }

                // handle ending the skill conversation
                if (endOfConversation)
                {
                    await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"<--Ending the skill conversation"));

                    return await innerDc.EndDialogAsync();
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
                await innerDc.EndDialogAsync();
                throw;
            }
        }

        private BotCallbackHandler CreateCallback(Activity activity)
        {
            return async (turnContext, token) =>
            {
                EnsureActivity(activity);

                // Send back the activities in the proactive context
                await turnContext.SendActivityAsync(activity, token);
            };
        }

        /// <summary>
        /// Ensure the activity objects are correctly set for proactive messages
        /// There is known issues about not being able to send these messages back
        /// correctly if the properties are not set in a certain way.
        /// </summary>
        /// <param name="activity">activity that's being sent out.</param>
        private void EnsureActivity(Activity activity)
        {
            if (activity != null)
            {
                if (activity.From != null)
                {
                    activity.From.Name = "User";
                    activity.From.Properties["role"] = "user";
                }

                if (activity.Recipient != null)
                {
                    activity.Recipient.Id = "1";
                    activity.Recipient.Name = "Bot";
                    activity.Recipient.Properties["role"] = "bot";
                }
            }
        }

        private class Events
        {
            public const string SkillBeginEventName = "skillBegin";
            public const string TokenRequestEventName = "tokens/request";
            public const string TokenResponseEventName = "tokens/response";
        }
    }
}