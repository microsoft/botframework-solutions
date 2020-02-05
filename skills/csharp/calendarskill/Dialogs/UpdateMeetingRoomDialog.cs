using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.FindMeetingRoom;
using CalendarSkill.Responses.UpdateEvent;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Graph;

namespace CalendarSkill.Dialogs
{
    public class UpdateMeetingRoomDialog : CalendarSkillDialogBase
    {
        public UpdateMeetingRoomDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            IServiceManager serviceManager,
            FindMeetingRoomDialog findMeetingRoomDialog,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(UpdateMeetingRoomDialog), settings, services, conversationState, localeTemplateEngineManager, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            var updateMeetingRoom = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                CheckFocusedEvent,
                FindMeetingRoom,
                GetAuthToken,
                AfterGetAuthToken,
                UpdateMeetingRoom
            };

            var findEvent = new WaterfallStep[]
            {
                SearchEventsWithEntities,
                GetEventsPrompt,
                AfterGetEventsPrompt,
                AddConflictFlag,
                ChooseEvent
            };

            var chooseEvent = new WaterfallStep[]
            {
                ChooseEventPrompt,
                AfterChooseEvent
            };

            // Define the conversation flow using a waterfall model.UpdateMeetingRoom
            AddDialog(new WaterfallDialog(Actions.UpdateMeetingRoom, updateMeetingRoom) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindEvent, findEvent) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ChooseEvent, chooseEvent) { TelemetryClient = telemetryClient });
            AddDialog(findMeetingRoomDialog ?? throw new ArgumentNullException(nameof(findMeetingRoomDialog)));

            // Set starting dialog for component
            InitialDialogId = Actions.UpdateMeetingRoom;
        }

        private async Task<DialogTurnResult> FindMeetingRoom(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = (CalendarSkillDialogOptions)sc.Options;
                var origin = state.ShowMeetingInfo.FocusedEvents[0];
                var updateEvent = new EventModel(origin.Source);

                state.MeetingInfo.StartDateTime = origin.StartTime;
                state.MeetingInfo.EndDateTime = origin.EndTime;
                var ts = state.MeetingInfo.StartDateTime.Value.Subtract(state.MeetingInfo.EndDateTime.Value).Duration();
                state.MeetingInfo.Duration = (int)ts.TotalSeconds;

                if (state.InitialIntent == CalendarLuis.Intent.DeleteCalendarEntry)
                {
                    return await sc.NextAsync();
                }

                return await sc.BeginDialogAsync(nameof(FindMeetingRoomDialog), sc.Options, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> UpdateMeetingRoom(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var origin = state.ShowMeetingInfo.FocusedEvents[0];
                var updateEvent = new EventModel(origin.Source);
                string meetingRoom = state.MeetingInfo.MeetingRoom?.DisplayName;
                var attendees = new List<EventModel.Attendee>();
                attendees.AddRange(origin.Attendees);

                if (state.InitialIntent == CalendarLuis.Intent.ChangeCalendarEntry)
                {
                    attendees.RemoveAll(x => x.AttendeeType == AttendeeType.Resource);
                }

                if (state.InitialIntent == CalendarLuis.Intent.DeleteCalendarEntry)
                {
                    meetingRoom = attendees.Find(x => x.AttendeeType == AttendeeType.Resource)?.DisplayName;
                    if (meetingRoom == null)
                    {
                        throw new Exception("No meeting room found.");
                    }

                    attendees.RemoveAll(x => x.AttendeeType == AttendeeType.Resource);
                }

                if (state.InitialIntent == CalendarLuis.Intent.ChangeCalendarEntry || state.InitialIntent == CalendarLuis.Intent.AddCalendarEntryAttribute)
                {
                    if (state.MeetingInfo.MeetingRoom == null)
                    {
                        throw new NullReferenceException("UpdateMeetingRoom received a null MeetingRoom.");
                    }

                    attendees.Add(new EventModel.Attendee
                    {
                        DisplayName = state.MeetingInfo.MeetingRoom.DisplayName,
                        Address = state.MeetingInfo.MeetingRoom.EmailAddress,
                        AttendeeType = AttendeeType.Resource
                    });
                }

                updateEvent.Id = origin.Id;
                updateEvent.Attendees = attendees;
                updateEvent.Location = null;
                if (!string.IsNullOrEmpty(state.UpdateMeetingInfo.RecurrencePattern) && !string.IsNullOrEmpty(origin.RecurringId))
                {
                    updateEvent.Id = origin.RecurringId;
                }

                sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
                var calendarService = ServiceManager.InitCalendarService(token as string, state.EventSource);
                var newEvent = await calendarService.UpdateEventByIdAsync(updateEvent);

                var data = new
                {
                    MeetingRoom = meetingRoom,
                    DateTime = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime((DateTime)state.MeetingInfo.StartDateTime, state.GetUserTimeZone()), state.MeetingInfo.AllDay == true, DateTime.UtcNow > state.MeetingInfo.StartDateTime),
                    Subject = newEvent.Title,
                };
                if (state.InitialIntent == CalendarLuis.Intent.AddCalendarEntryAttribute)
                {
                    var replyMessage = await GetDetailMeetingResponseAsync(sc, newEvent, FindMeetingRoomResponses.MeetingRoomAdded, data);
                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else if (state.InitialIntent == CalendarLuis.Intent.ChangeCalendarEntry)
                {
                    var replyMessage = await GetDetailMeetingResponseAsync(sc, newEvent, FindMeetingRoomResponses.MeetingRoomChanged, data);
                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else
                {
                    var activity = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.MeetingRoomCanceled, data);
                    await sc.Context.SendActivityAsync(activity);
                }

                state.Clear();
                return await sc.EndDialogAsync(true);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> GetEventsPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                if (state.ShowMeetingInfo.FocusedEvents.Any())
                {
                    return await sc.EndDialogAsync();
                }
                else if (state.ShowMeetingInfo.ShowingMeetings.Any())
                {
                    return await sc.NextAsync();
                }
                else
                {
                    sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
                    var calendarService = ServiceManager.InitCalendarService(token as string, state.EventSource);
                    return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, state.GetUserTimeZone())
                    {
                        Prompt = TemplateEngine.GenerateActivityForLocale(UpdateEventResponses.NoUpdateStartTime),
                        RetryPrompt = TemplateEngine.GenerateActivityForLocale(UpdateEventResponses.EventWithStartTimeNotFound),
                    }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}