using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.Summary;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Newtonsoft.Json.Linq;
using static CalendarSkill.Models.ShowMeetingsDialogOptions;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Dialogs
{
    public class CalendarSummaryDialog : CalendarSkillDialogBase
    {
        public CalendarSummaryDialog(
           BotSettings settings,
           BotServices services,
           ResponseManager responseManager,
           ConversationState conversationState,
           UpdateEventDialog updateEventDialog,
           ChangeEventStatusDialog changeEventStatusDialog,
           IServiceManager serviceManager,
           IBotTelemetryClient telemetryClient,
           MicrosoftAppCredentials appCredentials)
            : base(nameof(CalendarSummaryDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            var summaryDialog = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                GetSummary
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog("summaryDialog", summaryDialog) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = "summaryDialog";

        }

        protected async Task<DialogTurnResult> GetSummary(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);

                var searchDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, state.GetUserTimeZone());
                if (state.StartDate.Any())
                {
                    searchDate = state.StartDate.Last();
                }

                var results = await GetEventsByTime(new List<DateTime>() { searchDate }, state.StartTime, state.EndDate, state.EndTime, state.GetUserTimeZone(), calendarService);
                var searchedEvents = new List<EventModel>();
                var searchTodayMeeting = SearchesTodayMeeting(state);

                foreach (var item in results)
                {
                    if (!searchTodayMeeting || item.StartTime >= DateTime.UtcNow)
                    {
                        searchedEvents.Add(item);
                    }
                }


                var response = sc.Context.Activity.CreateReply();
                var entities = new Dictionary<string, Entity>();

                response.Name = "calendarSkill.MeetingSummary";
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
                    name = "calendarSkill.MeetingSummary",
                    totalCount = totalCount,
                    items = items
                });
                entities.Add(response.Name, new Entity { Properties = obj });
                response.SemanticAction = new SemanticAction("entity", entities);
                response.Type = ActivityTypes.EndOfConversation;

                await sc.Context.SendActivityAsync(response);
                return await sc.EndDialogAsync(true);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private bool SearchesTodayMeeting(CalendarSkillState state)
        {
            var userNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, state.GetUserTimeZone());
            var searchDate = userNow;

            if (state.StartDate.Any())
            {
                searchDate = state.StartDate.Last();
            }

            return !state.StartTime.Any() &&
                !state.EndDate.Any() &&
                !state.EndTime.Any() &&
                EventModel.IsSameDate(searchDate, userNow);
        }
    }
}