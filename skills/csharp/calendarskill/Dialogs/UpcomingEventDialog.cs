// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Proactive;
using CalendarSkill.Responses.UpcomingEvent;
using CalendarSkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Bot.Solutions.Util;
using static CalendarSkill.Proactive.CheckUpcomingEventHandler;

namespace CalendarSkill.Dialogs
{
    public class UpcomingEventDialog : CalendarSkillDialogBase
    {
        private IBackgroundTaskQueue _backgroundTaskQueue;
        private ProactiveState _proactiveState;
        private IStatePropertyAccessor<ProactiveModel> _proactiveStateAccessor;

        public UpcomingEventDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            ProactiveState proactiveState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            IBackgroundTaskQueue backgroundTaskQueue,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(UpcomingEventDialog), settings, services, conversationState, localeTemplateEngineManager, serviceManager, telemetryClient, appCredentials)
        {
            _backgroundTaskQueue = backgroundTaskQueue;
            _proactiveState = proactiveState;
            _proactiveStateAccessor = _proactiveState.CreateProperty<ProactiveModel>(nameof(ProactiveModel));

            var upcomingMeeting = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                QueueUpcomingEventWorker
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.ShowUpcomingMeeting, upcomingMeeting));

            // Set starting dialog for component
            InitialDialogId = Actions.ShowUpcomingMeeting;
        }

        private async Task<DialogTurnResult> QueueUpcomingEventWorker(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var calendarState = await Accessor.GetAsync(sc.Context, () => new CalendarSkillState());
                sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var apiToken);

                if (!string.IsNullOrWhiteSpace((string)apiToken))
                {
                    var activity = sc.Context.Activity;
                    var userId = activity.From.Id;

                    var proactiveState = await _proactiveStateAccessor.GetAsync(sc.Context, () => new ProactiveModel());
                    var calendarService = ServiceManager.InitCalendarService((string)apiToken, calendarState.EventSource);

                    _backgroundTaskQueue.QueueBackgroundWorkItem(async (token) =>
                    {
                        var handler = new CheckUpcomingEventHandler
                        {
                            CalendarService = calendarService
                        };
                        await handler.Handle(UpcomingEventCallback(userId, sc, proactiveState));
                    });
                }

                calendarState.Clear();
                return EndOfTurn;
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                throw;
            }
        }

        private UpcomingEventCallback UpcomingEventCallback(string userId, WaterfallStepContext sc, ProactiveModel proactiveModel)
        {
            return async (eventModel, cancellationToken) =>
            {
                await sc.Context.Adapter.ContinueConversationAsync(Settings.MicrosoftAppId, proactiveModel[MD5Util.ComputeHash(userId)].Conversation, UpcomingEventContinueConversationCallback(eventModel, sc), cancellationToken);
            };
        }

        // Creates the turn logic to use for the proactive message.
        private BotCallbackHandler UpcomingEventContinueConversationCallback(EventModel eventModel, WaterfallStepContext sc)
        {
            sc.EndDialogAsync();

            return async (turnContext, token) =>
            {
                var responseString = string.Empty;
                var responseParams = new
                {
                    Minutes = (eventModel.StartTime - DateTime.UtcNow).Minutes.ToString(),
                    Attendees = string.Join(",", eventModel.Attendees.ToSpeechString(CommonStrings.And, attendee => attendee.DisplayName ?? attendee.Address)),
                    Title = eventModel.Title,
                    Location = eventModel.Location ?? string.Empty
                };

                if (!string.IsNullOrWhiteSpace(eventModel.Location))
                {
                    responseString = UpcomingEventResponses.UpcomingEventMessageWithLocation;
                }
                else
                {
                    responseString = UpcomingEventResponses.UpcomingEventMessage;
                }

                var activity = TemplateEngine.GenerateActivityForLocale(responseString, responseParams);
                await turnContext.SendActivityAsync(activity);
            };
        }
    }
}