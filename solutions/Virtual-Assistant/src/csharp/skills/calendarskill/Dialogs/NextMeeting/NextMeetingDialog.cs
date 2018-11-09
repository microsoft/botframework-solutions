using CalendarSkill.Dialogs.NextMeeting.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarSkill
{
    public class NextMeetingDialog : CalendarSkillDialog
    {
        public NextMeetingDialog(
            ISkillConfiguration services,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager)
            : base(nameof(NextMeetingDialog), services, accessor, serviceManager)
        {
            var nextMeeting = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ShowNextEvent,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.ShowEventsSummary, nextMeeting));

            // Set starting dialog for component
            InitialDialogId = Actions.ShowEventsSummary;
        }

        public async Task<DialogTurnResult> ShowNextEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.APIToken))
                {
                    return await sc.EndDialogAsync(true);
                }

                var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource, state.GetUserTimeZone());

                var eventList = await calendarService.GetUpcomingEvents();
                var nextEventList = new List<EventModel>();
                foreach (var item in eventList)
                {
                    if (item.IsCancelled != true && (nextEventList.Count == 0 || nextEventList[0].StartTime == item.StartTime))
                    {
                        nextEventList.Add(item);
                    }
                }

                if (nextEventList.Count == 0)
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(NextMeetingResponses.ShowNoMeetingMessage));
                }
                else
                {
                    if (nextEventList.Count == 1)
                    {
                        var speakParams = new StringDictionary()
                        {
                            { "EventName", nextEventList[0].Title },
                            { "PeopleCount", nextEventList[0].Attendees.Count.ToString() },
                        };
                        if (nextEventList[0].IsAllDay == true)
                        {
                            speakParams.Add("EventTime", nextEventList[0].StartTime.ToString("MMMM dd all day"));
                        }
                        else
                        {
                            speakParams.Add("EventTime", nextEventList[0].StartTime.ToString("h:mm tt"));
                        }

                        if (string.IsNullOrEmpty(nextEventList[0].Location))
                        {
                            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(NextMeetingResponses.ShowNextMeetingNoLocationMessage, _responseBuilder, speakParams));
                        }
                        else
                        {
                            speakParams.Add("Location", nextEventList[0].Location);
                            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(NextMeetingResponses.ShowNextMeetingMessage, _responseBuilder, speakParams));
                        }
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(NextMeetingResponses.ShowMultipleNextMeetingMessage));
                    }

                    await ShowMeetingList(sc, nextEventList, true);
                }

                state.Clear();
                return await sc.EndDialogAsync(true);
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }
    }
}
