using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Responses.CalendarSummary;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace CalendarSkill.Dialogs
{
    public class CalendarSummaryDialog : CalendarSkillDialogBase
    {
        public CalendarSummaryDialog(
           BotSettings settings,
           BotServices services,
           ResponseManager responseManager,
           ConversationState conversationState,
           IServiceManager serviceManager,
           IBotTelemetryClient telemetryClient,
           MicrosoftAppCredentials appCredentials)
            : base(nameof(CalendarSummaryDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            var getMeetings = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                GetMeetings
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.GetMeetings, getMeetings) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.GetMeetings;

        }

        protected async Task<DialogTurnResult> GetMeetings(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.APIToken))
                {
                    state.Clear();
                    return await sc.EndDialogAsync(true);
                }

                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);

                var searchDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, state.GetUserTimeZone());
                if (state.MeetingInfor.StartDate.Any())
                {
                    searchDate = state.MeetingInfor.StartDate.Last();
                }

                var results = await CalendarCommonUtil.GetEventsByTime(new List<DateTime>() { searchDate }, state.MeetingInfor.StartTime, state.MeetingInfor.EndDate, state.MeetingInfor.EndTime, state.GetUserTimeZone(), calendarService);
                var searchedEvents = new List<EventModel>();

                foreach (var item in results)
                {
                    if (item.StartTime >= DateTime.UtcNow)
                    {
                        searchedEvents.Add(item);
                    }
                }

                SemanticAction semanticAction = new SemanticAction("calendar_summary", new Dictionary<string, Entity>());

                var items = new JArray();
                var totalCount = searchedEvents.Count();
                foreach (var result in searchedEvents)
                {
                    items.Add(JObject.FromObject(new
                    {
                        title = result.Title,
                        date = result.StartTime.Date,
                        time = result.StartTime.TimeOfDay,
                        duration = result.ToSpeechDurationString(),
                        Participants = result.Attendees
                    }));
                }

                var obj = JObject.FromObject(new
                {
                    name = CalendarSummaryStrings.MEETING_SUMMARY_SHOW_NAME,
                    totalCount = totalCount,
                    items = items
                });

                semanticAction.Entities.Add("CalendarSkill.MeetingSummary", new Entity { Properties = obj });
                semanticAction.State = SemanticActionStates.Done;

                state.Clear();
                return await sc.EndDialogAsync(semanticAction);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}