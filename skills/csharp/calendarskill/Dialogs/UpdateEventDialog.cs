// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace CalendarSkill.Dialogs
{
    public class UpdateEventDialog : CalendarSkillDialogBase
    {
        public UpdateEventDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(UpdateEventDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            var updateEvent = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                CheckFocusedEvent,
                GetNewEventTime,
                GetAuthToken,
                AfterGetAuthToken,
                ConfirmBeforeUpdate,
                AfterConfirmBeforeUpdate,
                GetAuthToken,
                AfterGetAuthToken,
                UpdateEventTime
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

            var getNewStartTime = new WaterfallStep[]
            {
                GetNewEventTimePrompt,
                AfterGetNewEventTimePrompt,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.UpdateEventTime, updateEvent) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindEvent, findEvent) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ChooseEvent, chooseEvent) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.GetNewStartTime, getNewStartTime) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.UpdateEventTime;
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
                    sc.Context.TurnState.TryGetValue(APITokenKey, out var token);
                    var calendarService = ServiceManager.InitCalendarService((string)token, state.EventSource, sc.Context);
                    return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, state.GetUserTimeZone())
                    {
                        Prompt = ResponseManager.GetResponse(UpdateEventResponses.NoUpdateStartTime),
                        RetryPrompt = ResponseManager.GetResponse(UpdateEventResponses.EventWithStartTimeNotFound),
                        MaxReprompt = CalendarCommonUtil.MaxRepromptCount
                    }, cancellationToken);
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

        private async Task<DialogTurnResult> GetNewEventTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.GetNewStartTime, sc.Options);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> ConfirmBeforeUpdate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var newStartTime = (DateTime)state.UpdateMeetingInfor.NewStartDateTime;
                var origin = state.ShowMeetingInfor.FocusedEvents[0];
                var last = origin.EndTime - origin.StartTime;
                origin.StartTime = newStartTime;
                origin.EndTime = (newStartTime + last).AddSeconds(1);

                var replyMessage = await GetDetailMeetingResponseAsync(sc, origin, UpdateEventResponses.ConfirmUpdate);

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = replyMessage,
                    RetryPrompt = ResponseManager.GetResponse(UpdateEventResponses.ConfirmUpdateFailed),
                });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterConfirmBeforeUpdate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = (CalendarSkillDialogOptions)sc.Options;
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    return await sc.NextAsync();
                }
                else
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CalendarSharedResponses.ActionEnded));
                    if (options.SubFlowMode)
                    {
                        state.UpdateMeetingInfor.Clear();
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

        private async Task<DialogTurnResult> UpdateEventTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = (CalendarSkillDialogOptions)sc.Options;

                var newStartTime = (DateTime)state.UpdateMeetingInfor.NewStartDateTime;
                var origin = state.ShowMeetingInfor.FocusedEvents[0];
                var updateEvent = new EventModel(origin.Source);
                var last = origin.EndTime - origin.StartTime;
                updateEvent.StartTime = newStartTime;
                updateEvent.EndTime = (newStartTime + last).AddSeconds(1);
                updateEvent.TimeZone = TimeZoneInfo.Utc;
                updateEvent.Id = origin.Id;

                if (!string.IsNullOrEmpty(state.UpdateMeetingInfor.RecurrencePattern) && !string.IsNullOrEmpty(origin.RecurringId))
                {
                    updateEvent.Id = origin.RecurringId;
                }

                sc.Context.TurnState.TryGetValue(APITokenKey, out var token);
                var calendarService = ServiceManager.InitCalendarService((string)token, state.EventSource, sc.Context);
                var newEvent = await calendarService.UpdateEventByIdAsync(updateEvent);

                var replyMessage = await GetDetailMeetingResponseAsync(sc, newEvent, UpdateEventResponses.EventUpdated);

                await sc.Context.SendActivityAsync(replyMessage);

                if (options.SubFlowMode)
                {
                    state.UpdateMeetingInfor.Clear();
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

        private async Task<DialogTurnResult> GetNewEventTimePrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.UpdateMeetingInfor.NewStartDate.Any() || state.UpdateMeetingInfor.NewStartTime.Any() || state.UpdateMeetingInfor.MoveTimeSpan != 0)
                {
                    return await sc.NextAsync();
                }

                return await sc.PromptAsync(Actions.TimePrompt, new TimePromptOptions
                {
                    Prompt = ResponseManager.GetResponse(UpdateEventResponses.NoNewTime),
                    RetryPrompt = ResponseManager.GetResponse(UpdateEventResponses.NoNewTimeRetry),
                    TimeZone = state.GetUserTimeZone(),
                    MaxReprompt = CalendarCommonUtil.MaxRepromptCount
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterGetNewEventTimePrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.UpdateMeetingInfor.NewStartDate.Any() || state.UpdateMeetingInfor.NewStartTime.Any() || state.UpdateMeetingInfor.MoveTimeSpan != 0)
                {
                    var originalEvent = state.ShowMeetingInfor.FocusedEvents[0];
                    var originalStartDateTime = TimeConverter.ConvertUtcToUserTime(originalEvent.StartTime, state.GetUserTimeZone());
                    var userNow = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone());

                    if (state.UpdateMeetingInfor.NewStartDate.Any() || state.UpdateMeetingInfor.NewStartTime.Any())
                    {
                        var newStartDate = state.UpdateMeetingInfor.NewStartDate.Any() ?
                            state.UpdateMeetingInfor.NewStartDate.Last() :
                            originalStartDateTime;

                        var newStartTime = new List<DateTime>();
                        if (state.UpdateMeetingInfor.NewStartTime.Any())
                        {
                            newStartTime.AddRange(state.UpdateMeetingInfor.NewStartTime);
                        }
                        else
                        {
                            newStartTime.Add(originalStartDateTime);
                        }

                        foreach (var time in newStartTime)
                        {
                            var newStartDateTime = new DateTime(
                                newStartDate.Year,
                                newStartDate.Month,
                                newStartDate.Day,
                                time.Hour,
                                time.Minute,
                                time.Second);

                            if (state.UpdateMeetingInfor.NewStartDateTime == null)
                            {
                                state.UpdateMeetingInfor.NewStartDateTime = newStartDateTime;
                            }

                            if (newStartDateTime >= userNow)
                            {
                                state.UpdateMeetingInfor.NewStartDateTime = newStartDateTime;
                                break;
                            }
                        }
                    }
                    else if (state.UpdateMeetingInfor.MoveTimeSpan != 0)
                    {
                        state.UpdateMeetingInfor.NewStartDateTime = originalStartDateTime.AddSeconds(state.UpdateMeetingInfor.MoveTimeSpan);
                    }
                    else
                    {
                        return await sc.BeginDialogAsync(Actions.GetNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
                    }

                    state.UpdateMeetingInfor.NewStartDateTime = TimeZoneInfo.ConvertTimeToUtc(state.UpdateMeetingInfor.NewStartDateTime.Value, state.GetUserTimeZone());

                    return await sc.EndDialogAsync();
                }
                else if (sc.Result != null)
                {
                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;

                    DateTime? newStartTime = null;

                    foreach (var resolution in dateTimeResolutions)
                    {
                        var utcNow = DateTime.UtcNow;
                        var dateTimeConvertTypeString = resolution.Timex;
                        var dateTimeConvertType = new TimexProperty(dateTimeConvertTypeString);
                        var dateTimeValue = DateTime.Parse(resolution.Value);
                        if (dateTimeValue == null)
                        {
                            continue;
                        }

                        var originalStartDateTime = TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfor.FocusedEvents[0].StartTime, state.GetUserTimeZone());
                        if (dateTimeConvertType.Types.Contains(Constants.TimexTypes.Date) && !dateTimeConvertType.Types.Contains(Constants.TimexTypes.DateTime))
                        {
                            dateTimeValue = new DateTime(
                                dateTimeValue.Year,
                                dateTimeValue.Month,
                                dateTimeValue.Day,
                                originalStartDateTime.Hour,
                                originalStartDateTime.Minute,
                                originalStartDateTime.Second);
                        }
                        else if (dateTimeConvertType.Types.Contains(Constants.TimexTypes.Time) && !dateTimeConvertType.Types.Contains(Constants.TimexTypes.DateTime))
                        {
                            dateTimeValue = new DateTime(
                                originalStartDateTime.Year,
                                originalStartDateTime.Month,
                                originalStartDateTime.Day,
                                dateTimeValue.Hour,
                                dateTimeValue.Minute,
                                dateTimeValue.Second);
                        }

                        dateTimeValue = TimeZoneInfo.ConvertTimeToUtc(dateTimeValue, state.GetUserTimeZone());

                        if (newStartTime == null)
                        {
                            newStartTime = dateTimeValue;
                        }

                        if (dateTimeValue >= utcNow)
                        {
                            newStartTime = dateTimeValue;
                            break;
                        }
                    }

                    if (newStartTime != null)
                    {
                        state.UpdateMeetingInfor.NewStartDateTime = newStartTime;

                        return await sc.EndDialogAsync();
                    }
                    else
                    {
                        return await sc.BeginDialogAsync(Actions.GetNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime));
                    }
                }
                else
                {
                    // user has tried 5 times but can't get result
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CalendarSharedResponses.RetryTooManyResponse));
                    return await sc.CancelAllDialogsAsync();
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