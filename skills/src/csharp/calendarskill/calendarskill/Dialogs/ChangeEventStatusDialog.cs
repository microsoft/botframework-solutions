using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.ChangeEventStatus;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;

namespace CalendarSkill.Dialogs
{
    public class ChangeEventStatusDialog : CalendarSkillDialogBase
    {
        public ChangeEventStatusDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(ChangeEventStatusDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            var changeEventStatus = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                FromTokenToStartTime,
                ConfirmBeforeAction,
                ChangeEventStatus,
            };

            var updateStartTime = new WaterfallStep[]
            {
                UpdateStartTime,
                AfterUpdateStartTime,
            };

            AddDialog(new WaterfallDialog(Actions.ChangeEventStatus, changeEventStatus) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateStartTime, updateStartTime) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.ChangeEventStatus;
        }

        public async Task<DialogTurnResult> ConfirmBeforeAction(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = (ChangeEventStatusDialogOptions)sc.Options;

                if (sc.Result != null && state.ShowMeetingInfor.FocusedEvents.Count > 1)
                {
                    var events = state.ShowMeetingInfor.FocusedEvents;
                    state.ShowMeetingInfor.FocusedEvents = new List<EventModel>
                    {
                        events[(sc.Result as FoundChoice).Index],
                    };
                }

                var deleteEvent = state.ShowMeetingInfor.FocusedEvents[0];
                string replyResponse;
                string retryResponse;
                if (options.NewEventStatus == EventStatus.Cancelled)
                {
                    replyResponse = ChangeEventStatusResponses.ConfirmDelete;
                    retryResponse = ChangeEventStatusResponses.ConfirmDeleteFailed;
                }
                else
                {
                    replyResponse = ChangeEventStatusResponses.ConfirmAccept;
                    retryResponse = ChangeEventStatusResponses.ConfirmAcceptFailed;
                }

                var replyMessage = await GetDetailMeetingResponseAsync(sc, deleteEvent, replyResponse);

                var retryMessage = ResponseManager.GetResponse(retryResponse);

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = replyMessage,
                    RetryPrompt = retryMessage,
                });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> ChangeEventStatus(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = (ChangeEventStatusDialogOptions)sc.Options;

                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var deleteEvent = state.ShowMeetingInfor.FocusedEvents[0];
                    if (options.NewEventStatus == EventStatus.Cancelled)
                    {
                        if (deleteEvent.IsOrganizer)
                        {
                            await calendarService.DeleteEventByIdAsync(deleteEvent.Id);
                        }
                        else
                        {
                            await calendarService.DeclineEventByIdAsync(deleteEvent.Id);
                        }

                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ChangeEventStatusResponses.EventDeleted));
                    }
                    else
                    {
                        await calendarService.AcceptEventByIdAsync(deleteEvent.Id);
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ChangeEventStatusResponses.EventAccepted));
                    }
                }
                else
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CalendarSharedResponses.ActionEnded));
                }

                if (options.SubFlowMode)
                {
                    state.MeetingInfor.ClearTimes();
                    state.MeetingInfor.ClearTitle();
                }
                else
                {
                    state.Clear();
                }

                return await sc.EndDialogAsync(true);
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

        public async Task<DialogTurnResult> FromTokenToStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);
                if (state.MeetingInfor.StartDateTime == null)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateStartTime, sc.Options);
                }
                else
                {
                    return await sc.NextAsync();
                }
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

        public async Task<DialogTurnResult> UpdateStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = (ChangeEventStatusDialogOptions)sc.Options;

                if (state.ShowMeetingInfor.FocusedEvents.Count > 0)
                {
                    return await sc.NextAsync();
                }

                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);

                if (state.MeetingInfor.StartDate.Any() || state.MeetingInfor.StartTime.Any())
                {
                    state.ShowMeetingInfor.FocusedEvents = await GetEventsByTime(state.MeetingInfor.StartDate, state.MeetingInfor.StartTime, state.MeetingInfor.EndDate, state.MeetingInfor.EndTime, state.GetUserTimeZone(), calendarService);
                    state.MeetingInfor.ClearTimes();
                    if (state.ShowMeetingInfor.FocusedEvents.Count > 0)
                    {
                        return await sc.NextAsync();
                    }
                }

                if (state.MeetingInfor.Title != null)
                {
                    state.ShowMeetingInfor.FocusedEvents = await calendarService.GetEventsByTitleAsync(state.MeetingInfor.Title);
                    state.MeetingInfor.Title = null;
                    if (state.ShowMeetingInfor.FocusedEvents.Count > 0)
                    {
                        return await sc.NextAsync();
                    }
                }

                if (options.NewEventStatus == EventStatus.Cancelled)
                {
                    return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, state.GetUserTimeZone())
                    {
                        Prompt = ResponseManager.GetResponse(ChangeEventStatusResponses.NoDeleteStartTime),
                        RetryPrompt = ResponseManager.GetResponse(ChangeEventStatusResponses.EventWithStartTimeNotFound)
                    }, cancellationToken);
                }
                else
                {
                    return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, state.GetUserTimeZone())
                    {
                        Prompt = ResponseManager.GetResponse(ChangeEventStatusResponses.NoAcceptStartTime),
                        RetryPrompt = ResponseManager.GetResponse(ChangeEventStatusResponses.EventWithStartTimeNotFound)
                    }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterUpdateStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                if (sc.Result != null)
                {
                    state.ShowMeetingInfor.FocusedEvents = sc.Result as List<EventModel>;
                }

                if (state.ShowMeetingInfor.FocusedEvents.Count == 0)
                {
                    // should not doto this part. add log here for safe
                    await HandleDialogExceptions(sc, new Exception("Unexpect zero events count"));
                    return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                }
                else
                if (state.ShowMeetingInfor.FocusedEvents.Count > 1)
                {
                    var options = new PromptOptions()
                    {
                        Choices = new List<Choice>(),
                    };

                    for (var i = 0; i < state.ShowMeetingInfor.FocusedEvents.Count; i++)
                    {
                        var item = state.ShowMeetingInfor.FocusedEvents[i];
                        var choice = new Choice()
                        {
                            Value = string.Empty,
                            Synonyms = new List<string> { (i + 1).ToString(), item.Title },
                        };
                        options.Choices.Add(choice);
                    }

                    state.ShowMeetingInfor.ShowingCardTitle = CalendarCommonStrings.MeetingsToChoose;
                    var prompt = await GetGeneralMeetingListResponseAsync(sc.Context, state, true, ChangeEventStatusResponses.MultipleEventsStartAtSameTime, null);

                    options.Prompt = prompt;

                    return await sc.PromptAsync(Actions.EventChoice, options);
                }
                else
                {
                    return await sc.EndDialogAsync(true);
                }
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
    }
}