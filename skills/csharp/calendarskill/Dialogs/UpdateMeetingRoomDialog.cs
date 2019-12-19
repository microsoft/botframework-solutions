using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Options;
using CalendarSkill.Prompts;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.UpdateEvent;
using CalendarSkill.Responses.FindMeetingRoom;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
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
                var origin = state.ShowMeetingInfor.FocusedEvents[0];
                var updateEvent = new EventModel(origin.Source);

                state.MeetingInfor.StartDateTime = origin.StartTime;
                state.MeetingInfor.EndDateTime = origin.EndTime;
                var ts = state.MeetingInfor.StartDateTime.Value.Subtract(state.MeetingInfor.EndDateTime.Value).Duration();
                state.MeetingInfor.Duration = (int)ts.TotalSeconds;

                if (state.InitialIntent == CalendarLuis.Intent.CancelMeetingRoom)
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
                var origin = state.ShowMeetingInfor.FocusedEvents[0];
                var updateEvent = new EventModel(origin.Source);
                string meetingRoom = state.MeetingInfor.MeetingRoom?.DisplayName;
                var attendees = new List<EventModel.Attendee>();
                attendees.AddRange(origin.Attendees);

                if (state.InitialIntent == CalendarLuis.Intent.ChangeMeetingRoom || state.InitialIntent == CalendarLuis.Intent.CancelMeetingRoom)
                {
                    meetingRoom = attendees.Find(x => x.AttendeeType == AttendeeType.Resource)?.DisplayName;
                    attendees.RemoveAll(x => x.AttendeeType == AttendeeType.Resource);
                }

                if (state.InitialIntent == CalendarLuis.Intent.ChangeMeetingRoom || state.InitialIntent == CalendarLuis.Intent.AddMeetingRoom)
                {
                    if (state.MeetingInfor.MeetingRoom == null)
                    {
                        return await sc.EndDialogAsync();
                    }

                    attendees.Add(new EventModel.Attendee
                    {
                        DisplayName = state.MeetingInfor.MeetingRoom.DisplayName,
                        Address = state.MeetingInfor.MeetingRoom.EmailAddress,
                        AttendeeType = AttendeeType.Resource
                    });
                }

                updateEvent.Id = origin.Id;
                updateEvent.Attendees = attendees;
                updateEvent.Location = null;
                if (!string.IsNullOrEmpty(state.UpdateMeetingInfor.RecurrencePattern) && !string.IsNullOrEmpty(origin.RecurringId))
                {
                    updateEvent.Id = origin.RecurringId;
                }

                sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
                var calendarService = ServiceManager.InitCalendarService(token as string, state.EventSource);
                var newEvent = await calendarService.UpdateEventByIdAsync(updateEvent);

                var data = new
                {
                    MeetingRoom = meetingRoom ?? "",
                    DateTime = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime((DateTime)state.MeetingInfor.StartDateTime, state.GetUserTimeZone()), state.MeetingInfor.Allday == true, DateTime.UtcNow > state.MeetingInfor.StartDateTime),
                    Subject = string.IsNullOrEmpty(newEvent.Title) ? null : string.Format(CalendarCommonStrings.ShowEventTitleCondition, newEvent.Title),
                };
                if (state.InitialIntent == CalendarLuis.Intent.AddMeetingRoom)
                {
                    var replyMessage = await GetDetailMeetingResponseAsync(sc, newEvent, FindMeetingRoomResponses.MeetingRoomAdded, data);
                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else if (state.InitialIntent == CalendarLuis.Intent.ChangeMeetingRoom)
                {
                    var replyMessage = await GetDetailMeetingResponseAsync(sc, newEvent, FindMeetingRoomResponses.MeetingRoomAddChanged, data);
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

                if (state.ShowMeetingInfor.FocusedEvents.Any())
                {
                    return await sc.EndDialogAsync();
                }
                else if (state.ShowMeetingInfor.ShowingMeetings.Any())
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