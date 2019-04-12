using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Proactive;
using CalendarSkill.Responses.UpcomingEvent;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Builder.Solutions.Util;
using static CalendarSkill.Proactive.CheckUpcomingEventHandler;

namespace CalendarSkill.Dialogs
{
    public class UpcomingEventDialog : CalendarSkillDialog
    {
        private IBackgroundTaskQueue _backgroundTaskQueue;
        private IStatePropertyAccessor<ProactiveModel> _proactiveStateAccessor;
        private ResponseManager _responseManager;

        public UpcomingEventDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IStatePropertyAccessor<ProactiveModel> proactiveStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            IBackgroundTaskQueue backgroundTaskQueue)
            : base(nameof(UpcomingEventDialog), settings, services, responseManager, accessor, serviceManager, telemetryClient)
        {
            _backgroundTaskQueue = backgroundTaskQueue;
            _proactiveStateAccessor = proactiveStateAccessor;
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