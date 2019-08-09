using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Prompts.Options;
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

        public async Task<DialogTurnResult> GetMeetingsToUpdate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                // If there is any meeting saved from other dialog, continue
                if (state.ShowMeetingInfor.FocusedEvents.Count > 0)
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
                    state.ShowMeetingInfor.FocusedEvents = await calendarService.GetEventsByTitle(state.MeetingInfor.Title);
                    state.MeetingInfor.ClearTitle();
                    if (state.ShowMeetingInfor.FocusedEvents.Count > 0)
                    {
                        return await sc.NextAsync();
                    }
                }

                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ChangeEventStatusResponses.EventDeleted));
                    }
                    else
                    {
                        await calendarService.AcceptEventById(deleteEvent.Id);
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ChangeEventStatusResponses.EventAccepted));
                    }
                }
                else
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CalendarSharedResponses.ActionEnded));
                }

                if (state.IsActionFromSummary)
                {
                    state.ClearChangeStautsInfo();
                }
                else
                {
                    return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, state.GetUserTimeZone())
                    {
                        Prompt = (Activity)await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[NoAcceptStartTime]", null),
                        RetryPrompt = (Activity)await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[EventWithStartTimeNotFound]", null)
                    }, cancellationToken);
                }
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

                // get result from GetEventPrompt
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

                    var prompt = await GetGeneralMeetingListResponseAsync(sc, _lgMultiLangEngine, CalendarCommonStrings.MeetingsToChoose, state.ShowMeetingInfor.FocusedEvents, "MultipleEventsStartAtSameTime", null);

                    options.Prompt = prompt;

                    return await sc.PromptAsync(Actions.EventChoice, options);
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

            if (sc.Result != null && state.ShowMeetingInfor.FocusedEvents.Count > 1)
            {
                var events = state.ShowMeetingInfor.FocusedEvents;
                state.ShowMeetingInfor.FocusedEvents = new List<EventModel>
                {
                    events[(sc.Result as FoundChoice).Index],
                };
            }

            return await sc.NextAsync();
        }

        public async Task<DialogTurnResult> ConfirmBeforeAction(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = (ChangeEventStatusDialogOptions)sc.Options;

                // if there is no meeting can be got from information in utterance, prompt user to get meetings to update
                if (state.NewEventStatus == EventStatus.Cancelled)
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

                var replyMessage = await GetDetailMeetingResponseAsync(sc, _lgMultiLangEngine, deleteEvent, replyResponse);
                var retryMessage = await GetDetailMeetingResponseAsync(sc, _lgMultiLangEngine, deleteEvent, retryResponse);

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

        public async Task<DialogTurnResult> AfterUpdateStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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
                            await calendarService.DeleteEventById(deleteEvent.Id);
                        }
                        else
                        {
                            await calendarService.DeclineEventById(deleteEvent.Id);
                        }

                        var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[EventDeleted]", null);
                        await sc.Context.SendActivityAsync(activity);
                    }
                    else
                    {
                        await calendarService.AcceptEventById(deleteEvent.Id);
                        var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[EventAccepted]", null);
                        await sc.Context.SendActivityAsync(activity);
                    }
                }

                if (options.SubFlowMode)
                {
                    state.ShowMeetingInfor.ClearFocusedEvents();
                }
                else
                {
                    state.Clear();
                }

                    var prompt = await GetGeneralMeetingListResponseAsync(sc, CalendarCommonStrings.MeetingsToChoose, state.Events, ChangeEventStatusResponses.MultipleEventsStartAtSameTime, null);

                    options.Prompt = prompt;

                    return await sc.PromptAsync(Actions.EventChoice, options);
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