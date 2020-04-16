// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using ITSMSkill.Extensions.Teams.TaskModule;
using ITSMSkill.Models.ServiceNow;
using ITSMSkill.Proactive;
using ITSMSkill.Services;
using ITSMSkill.TeamsChannels.Invoke;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace ITSMSkill.Bots
{
    public class DefaultActivityHandler<T> : TeamsActivityHandler
        where T : Dialog
    {
        private readonly Dialog _dialog;
        private readonly BotState _conversationState;
        private readonly BotState _userState;
        private readonly ServiceNowProactiveState _proactiveState;
        private IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private MicrosoftAppCredentials _appCredentials;
        private IStatePropertyAccessor<ProactiveModel> _proactiveStateAccessor;
        private BotSettings _botSettings;
        private BotServices _botServices;
        private IServiceManager _serviceManager;
        private BotTelemetryClient _botTelemetryClient;
        private IConnectorClient _connectorClient;

        public DefaultActivityHandler(
            IServiceProvider serviceProvider,
            T dialog)
        {
            _dialog = dialog;
            _conversationState = serviceProvider.GetService<ConversationState>();
            _userState = serviceProvider.GetService<UserState>();
            _proactiveState = serviceProvider.GetService<ServiceNowProactiveState>();
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
            _proactiveStateAccessor = _proactiveState.CreateProperty<ProactiveModel>(nameof(ProactiveModel));
            _appCredentials = serviceProvider.GetService<MicrosoftAppCredentials>();
            _botSettings = serviceProvider.GetService<BotSettings>();
            _botServices = serviceProvider.GetService<BotServices>();
            _serviceManager = serviceProvider.GetService<ServiceManager>();
            _botTelemetryClient = serviceProvider.GetService<BotTelemetryClient>();
            _connectorClient = serviceProvider.GetService<IConnectorClient>();
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            return _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        protected override Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            return _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        protected override Task OnTeamsSigninVerifyStateAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            return _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        protected override async Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            var ev = turnContext.Activity.AsEventActivity();
            var value = ev.Value?.ToString();

            switch (ev.Name)
            {
                case TokenEvents.TokenResponseEventName:
                    {
                        // Forward the token response activity to the dialog waiting on the stack.
                        await _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
                        break;
                    }

                case ServiceNowEvents.Proactive:
                    {
                        var eventData = JsonConvert.DeserializeObject<ServiceNowNotification>(turnContext.Activity.Value.ToString());

                        var proactiveModel = await _proactiveStateAccessor.GetAsync(turnContext, () => new ProactiveModel());

                        // TODO: Implement a proactive subscription manager for mapping Notification to ConversationReference
                        var conversationReference = proactiveModel["29:1L2z9sqte3pWsVlRFyFpw5RiB8N0eoUM9MBkywGgU6rGNKPd95Jx15AvIetaNLO5L8ZJ3C76pmnuy-mx5_oIDDQ"].Conversation;

                        await turnContext.Adapter.ContinueConversationAsync(_appCredentials.MicrosoftAppId, conversationReference, ContinueConversationCallback(turnContext, eventData), cancellationToken);
                        break;
                    }

                default:
                    {
                        await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{ev.Name ?? "undefined"}' was received but not processed."));
                        break;
                    }
            }
        }

        protected override async Task<InvokeResponse> OnInvokeActivityAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            var itsmTeamsActivityHandler = new ITSMTeamsInvokeActivityHandlerFactory(_botSettings, _botServices, (ConversationState)_conversationState, _serviceManager, _botTelemetryClient, _connectorClient);
            var teamsInvokeEnvelope = await itsmTeamsActivityHandler.HandleTaskModuleActivity(turnContext, cancellationToken);

            return new InvokeResponse()
            {
                Status = (int)HttpStatusCode.OK,
                Body = teamsInvokeEnvelope
            };
        }

        private IMessageActivity CreateAdaptiveCard(ITurnContext context, ServiceNowNotification serviceNowNotification)
        {
            Activity reply = context.Activity.CreateReply();
            var card = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveContainer
                    {
                        Items = new List<AdaptiveElement>
                        {
                            new AdaptiveColumnSet
                            {
                                Columns = new List<AdaptiveColumn>
                                {
                                    new AdaptiveColumn
                                    {
                                        Width = AdaptiveColumnWidth.Stretch,
                                        Items = new List<AdaptiveElement>
                                        {
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"Title: {serviceNowNotification.Title}",
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                // Incase of IcmForwarder, Triggers do not have incidentUrl hence being explicit here
                                                Text = $"Urgency: {serviceNowNotification.Urgency}",
                                                Color = AdaptiveTextColor.Good,
                                                MaxLines = 1,
                                                Weight = AdaptiveTextWeight.Bolder,
                                                Size = AdaptiveTextSize.Large
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"Description: {serviceNowNotification.Description}",
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"Impact: {serviceNowNotification.Impact}",
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
                                            },
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            reply.Attachments = new List<Attachment>
            {
                new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = card
                }
            };

            return reply;
        }

        /// <summary>
        /// Continue the conversation callback.
        /// </summary>
        /// <param name="context">Turn context.</param>
        /// <param name="message">Activity text.</param>
        /// <returns>Bot Callback Handler.</returns>
        private BotCallbackHandler ContinueConversationCallback(ITurnContext context, ServiceNowNotification notification)
        {
            return async (turnContext, cancellationToken) =>
            {
                var activity = CreateAdaptiveCard(context, notification);
                EnsureActivity(activity);
                await turnContext.SendActivityAsync(activity);
            };
        }

        /// <summary>
        /// This method is required for proactive notifications to work in Web Chat.
        /// </summary>
        /// <param name="activity">Proactive Activity.</param>
        private void EnsureActivity(IMessageActivity activity)
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
    }
}
