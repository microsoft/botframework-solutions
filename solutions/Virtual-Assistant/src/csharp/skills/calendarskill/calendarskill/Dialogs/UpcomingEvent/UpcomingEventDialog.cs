using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.Shared;
using CalendarSkill.Dialogs.UpcomingEvent.Resources;
using CalendarSkill.Models;
using CalendarSkill.Proactive;
using CalendarSkill.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Models.Proactive;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Bot.Solutions.Util;
using static CalendarSkill.Proactive.CheckUpcomingEventHandler;

namespace CalendarSkill.Dialogs.UpcomingEvent
{
    public class UpcomingEventDialog : CalendarSkillDialog
    {
        private IBackgroundTaskQueue _backgroundTaskQueue;
        private IStatePropertyAccessor<ProactiveModel> _proactiveStateAccessor;
        private EndpointService _endpointService;
        private ResponseManager _responseManager;

        public UpcomingEventDialog(
            SkillConfigurationBase services,
            EndpointService endpointService,
            ResponseManager responseManager,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IStatePropertyAccessor<ProactiveModel> proactiveStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            IBackgroundTaskQueue backgroundTaskQueue)
            : base(nameof(UpcomingEventDialog), services, responseManager, accessor, serviceManager, telemetryClient)
        {
            _backgroundTaskQueue = backgroundTaskQueue;
            _proactiveStateAccessor = proactiveStateAccessor;
            _endpointService = endpointService;
            _responseManager = responseManager;

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

        public async Task<DialogTurnResult> QueueUpcomingEventWorker(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var calendarState = await Accessor.GetAsync(sc.Context, () => new CalendarSkillState());

                if (!string.IsNullOrWhiteSpace(calendarState.APIToken))
                {
                    var activity = sc.Context.Activity;
                    var userId = activity.From.Id;

                    var proactiveState = await _proactiveStateAccessor.GetAsync(sc.Context, () => new ProactiveModel());
                    var calendarService = ServiceManager.InitCalendarService(calendarState.APIToken, calendarState.EventSource);

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
                await sc.Context.Adapter.ContinueConversationAsync(_endpointService.AppId, proactiveModel[MD5Util.ComputeHash(userId)].Conversation, UpcomingEventContinueConversationCallback(eventModel, sc), cancellationToken);
            };
        }

        // Creates the turn logic to use for the proactive message.
        private BotCallbackHandler UpcomingEventContinueConversationCallback(EventModel eventModel, WaterfallStepContext sc)
        {
            sc.EndDialogAsync();

            return async (turnContext, token) =>
            {
                var responseString = string.Empty;
                var responseParams = new StringDictionary()
                {
                    { "Minutes", (eventModel.StartTime - DateTime.UtcNow).Minutes.ToString() },
                    { "Attendees", string.Join(",", eventModel.Attendees.ToSpeechString(CommonStrings.And, attendee => attendee.DisplayName ?? attendee.Address)) },
                    { "Title", eventModel.Title },
                };

                if (!string.IsNullOrWhiteSpace(eventModel.Location))
                {
                    responseString = UpcomingEventResponses.UpcomingEventMessageWithLocation;
                    responseParams.Add("Location", eventModel.Location);
                }
                else
                {
                    responseString = UpcomingEventResponses.UpcomingEventMessage;
                }

                var activity = turnContext.Activity.CreateReply();
                var response = _responseManager.GetResponse(responseString, responseParams);
                activity.Text = response.Text;
                activity.Speak = response.Speak;
                activity.InputHint = response.InputHint;
                activity.SuggestedActions = response.SuggestedActions;
                activity.DeliveryMode = CommonUtil.DeliveryModeProactive;
                await turnContext.SendActivityAsync(activity);
            };
        }
    }
}