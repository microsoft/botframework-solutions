// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.ChangeEventStatus;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace CalendarSkill.Dialogs
{
    public class ChangeEventStatusDialog : CalendarSkillDialogBase
    {
        public ChangeEventStatusDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(ChangeEventStatusDialog), settings, services, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            var changeEventStatus = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                CheckFocusedEvent,
                GetAuthToken,
                AfterGetAuthToken,
                ConfirmBeforeAction,
                AfterConfirmBeforeAction,
                GetAuthToken,
                AfterGetAuthToken,
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

        private async Task<DialogTurnResult> ConfirmBeforeAction(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

                var startTime = TimeConverter.ConvertUtcToUserTime(deleteEvent.StartTime, state.GetUserTimeZone());

                var responseParams = new
                {
                    Time = startTime.ToString(CommonStrings.DisplayTime),
                    Title = deleteEvent.Title
                };

                var replyMessage = await GetDetailMeetingResponseAsync(sc, deleteEvent, replyResponse, responseParams);
                var retryMessage = await LGHelper.GenerateMessageAsync(sc.Context, retryResponse, responseParams) as Activity;
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

        private async Task<DialogTurnResult> AfterConfirmBeforeAction(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = (ChangeEventStatusDialogOptions)sc.Options;

                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    return await sc.NextAsync();
                }
                else
                {
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
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> ChangeEventStatus(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = (ChangeEventStatusDialogOptions)sc.Options;
                sc.Context.TurnState.TryGetValue(APITokenKey, out var token);

                var calendarService = ServiceManager.InitCalendarService((string)token, state.EventSource);
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

                    var activity = await LGHelper.GenerateMessageAsync(sc.Context, ChangeEventStatusResponses.EventDeleted, null);
                    await sc.Context.SendActivityAsync(activity);
                }
                else
                {
                    await calendarService.AcceptEventByIdAsync(deleteEvent.Id);

                    var activity = await LGHelper.GenerateMessageAsync(sc.Context, ChangeEventStatusResponses.EventAccepted, null);
                    await sc.Context.SendActivityAsync(activity);
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
                    sc.Context.TurnState.TryGetValue(APITokenKey, out var token);
                    var calendarService = ServiceManager.InitCalendarService((string)token, state.EventSource);
                    if (options.NewEventStatus == EventStatus.Cancelled)
                    {
                        return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, state.GetUserTimeZone())
                        {
                            Prompt = await LGHelper.GenerateMessageAsync(sc.Context, ChangeEventStatusResponses.NoDeleteStartTime, null) as Activity,
                            RetryPrompt = await LGHelper.GenerateMessageAsync(sc.Context, ChangeEventStatusResponses.EventWithStartTimeNotFound, null) as Activity,
                            MaxReprompt = CalendarCommonUtil.MaxRepromptCount
                        }, cancellationToken);
                    }
                    else
                    {
                        return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, state.GetUserTimeZone())
                        {
                            Prompt = await LGHelper.GenerateMessageAsync(sc.Context, ChangeEventStatusResponses.NoAcceptStartTime, null) as Activity,
                            RetryPrompt = await LGHelper.GenerateMessageAsync(sc.Context, ChangeEventStatusResponses.EventWithStartTimeNotFound, null) as Activity,
                            MaxReprompt = CalendarCommonUtil.MaxRepromptCount
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
    }
}