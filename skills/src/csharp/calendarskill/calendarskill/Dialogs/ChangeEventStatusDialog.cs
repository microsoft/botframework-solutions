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
                CheckFocusedEvent,
                ConfirmBeforeAction,
                ChangeEventStatus
            };

            var findEvent = new WaterfallStep[]
            {
                SearchEventsWithEntities,
                GetEvents,
                AfterGetEventsPrompt,
                AddConflictFlag,
                ChooseEvent
            };

            var chooseEvent = new WaterfallStep[]
            {
                ChooseEventPrompt,
                AfterChooseEvent
            };

            AddDialog(new WaterfallDialog(Actions.ChangeEventStatus, changeEventStatus) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindEvent, findEvent) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ChooseEvent, chooseEvent) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.ChangeEventStatus;
        }

        public async Task<DialogTurnResult> ConfirmBeforeAction(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = (ChangeEventStatusDialogOptions)sc.Options;

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

        private async Task<DialogTurnResult> GetEvents(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = (ChangeEventStatusDialogOptions)sc.Options;

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
                    var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);
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

        public async Task<DialogTurnResult> AfterGetEventsPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                if (sc.Result != null)
                {
                    state.ShowMeetingInfor.ShowingMeetings = sc.Result as List<EventModel>;
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> ChooseEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Actions.ChooseEvent, sc.Options);
        }

        public async Task<DialogTurnResult> ChooseEventPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                if (sc.Result != null)
                {
                    state.ShowMeetingInfor.ShowingMeetings = sc.Result as List<EventModel>;
                }

                if (state.ShowMeetingInfor.ShowingMeetings.Count == 0)
                {
                    // should not doto this part. add log here for safe
                    await HandleDialogExceptions(sc, new Exception("Unexpect zero events count"));
                    return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                }
                else if (state.ShowMeetingInfor.ShowingMeetings.Count > 1)
                {
                    var options = new PromptOptions()
                    {
                        Choices = new List<Choice>(),
                    };

                    for (var i = 0; i < state.ShowMeetingInfor.ShowingMeetings.Count; i++)
                    {
                        var item = state.ShowMeetingInfor.ShowingMeetings[i];
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
                    state.ShowMeetingInfor.FocusedEvents.Add(state.ShowMeetingInfor.ShowingMeetings.First());
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

        public async Task<DialogTurnResult> AfterChooseEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = (ChangeEventStatusDialogOptions)sc.Options;

                if (sc.Result != null)
                {
                    var events = state.ShowMeetingInfor.ShowingMeetings;
                    state.ShowMeetingInfor.FocusedEvents.Add(events[(sc.Result as FoundChoice).Index]);
                }

                return await sc.NextAsync();
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