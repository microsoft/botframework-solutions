using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.JoinEvent.Resources;
using CalendarSkill.Dialogs.Shared;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using HtmlAgilityPack;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Newtonsoft.Json;

namespace CalendarSkill.Dialogs.JoinEvent
{
    public class ConnectToMeetingDialog : CalendarSkillDialog
    {
        public ConnectToMeetingDialog(
            SkillConfigurationBase services,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ConnectToMeetingDialog), services, accessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var joinMeeting = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                JoinMeeting
            };

            AddDialog(new WaterfallDialog(Actions.ConnectToMeeting, joinMeeting) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.ConnectToMeeting;
        }

        private async Task<DialogTurnResult> JoinMeeting(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (string.IsNullOrEmpty(state.APIToken))
                {
                    return await sc.EndDialogAsync(true);
                }

                var tokens = new StringDictionary();
                var eventModels = await GetMeetingToJoin(sc);
                if (!eventModels.Any())
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(JoinEventResponses.MeetingNotFound));
                    return await sc.EndDialogAsync(null, cancellationToken);
                }

                var joinNumber = GetDialInNumberFromMeeting(eventModels[0]);
                if (string.IsNullOrEmpty(joinNumber))
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(JoinEventResponses.NoDialInNumber, tokens: tokens));
                    return await sc.EndDialogAsync(null, cancellationToken);
                }

                tokens.Add("CallNumber", joinNumber);
                var act = sc.Context.Activity.CreateReply(JoinEventResponses.CallingIn, ResponseBuilder, tokens: tokens);
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(JoinEventResponses.CallingIn, ResponseBuilder, tokens: tokens));

                // Reply the phone number as an event.
                var replyEvent = sc.Context.Activity.CreateReply();
                replyEvent.Type = ActivityTypes.Event;
                replyEvent.Name = "JoinEvent.DialInNumber";
                replyEvent.Value = joinNumber;
                var sample = JsonConvert.SerializeObject(replyEvent);
                await sc.Context.SendActivityAsync(replyEvent, cancellationToken);

                state.Clear();
                return await sc.EndDialogAsync(true, cancellationToken);
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private string GetDialInNumberFromMeeting(EventModel eventModel)
        {
            // Support teams and skype meeting.
            if (string.IsNullOrEmpty(eventModel.Content))
            {
                return null;
            }

            var body = eventModel.Content;
            var doc = new HtmlDocument();
            doc.LoadHtml(body);

            var number = doc.DocumentNode.SelectSingleNode("//a[contains(@href, 'tel')]");
            if (number == null || string.IsNullOrEmpty(number.InnerText))
            {
                return null;
            }

            const string telToken = "&#43;";
            return number.InnerText.Replace(telToken, string.Empty);
        }

        private async Task<List<EventModel>> GetMeetingToJoin(WaterfallStepContext sc)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);

            var eventList = await calendarService.GetUpcomingEvents();
            var nextEventList = new List<EventModel>();
            foreach (var item in eventList)
            {
                var itemUserTimeZoneTime = TimeZoneInfo.ConvertTime(item.StartTime, TimeZoneInfo.Utc, state.GetUserTimeZone());
                if (item.IsCancelled != true && nextEventList.Count == 0)
                {
                    if (state.OrderReference == "next")
                    {
                        nextEventList.Add(item);
                    }
                    else if (state.StartDate.Any() && itemUserTimeZoneTime.DayOfYear == state.StartDate[0].DayOfYear)
                    {
                        nextEventList.Add(item);
                    }
                    else if (state.StartTime.Any() && itemUserTimeZoneTime == state.StartTime[0])
                    {
                        nextEventList.Add(item);
                    }
                    else if (state.Title != null && item.Title.Equals(state.Title, StringComparison.CurrentCultureIgnoreCase))
                    {
                        nextEventList.Add(item);
                    }
                }
            }

            return nextEventList;
        }
    }
}