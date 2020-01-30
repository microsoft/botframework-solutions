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
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.UpdateEvent;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace CalendarSkill.Dialogs
{
    public class UpdateEventDialog : CalendarSkillDialogBase
    {
        public UpdateEventDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(UpdateEventDialog), settings, services, conversationState, localeTemplateEngineManager, serviceManager, telemetryClient, appCredentials)
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
                        Prompt = TemplateEngine.GenerateActivityForLocale(UpdateEventResponses.NoUpdateStartTime) as Activity,
                        RetryPrompt = TemplateEngine.GenerateActivityForLocale(UpdateEventResponses.EventWithStartTimeNotFound) as Activity,
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
                var newStartTime = (DateTime)state.UpdateMeetingInfo.NewStartDateTime;
                var origin = state.ShowMeetingInfo.FocusedEvents[0];
                var last = origin.EndTime - origin.StartTime;
                origin.StartTime = newStartTime;
                origin.EndTime = (newStartTime + last).AddSeconds(1);

                var replyMessage = await GetDetailMeetingResponseAsync(sc, origin, UpdateEventResponses.ConfirmUpdate);

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = replyMessage,
                    RetryPrompt = TemplateEngine.GenerateActivityForLocale(UpdateEventResponses.ConfirmUpdateFailed) as Activity,
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
                    var activity = TemplateEngine.GenerateActivityForLocale(CalendarSharedResponses.ActionEnded);
                    await sc.Context.SendActivityAsync(activity);
                    if (options.SubFlowMode)
                    {
                        state.UpdateMeetingInfo.Clear();
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

                var newStartTime = (DateTime)state.UpdateMeetingInfo.NewStartDateTime;
                var origin = state.ShowMeetingInfo.FocusedEvents[0];
                var updateEvent = new EventModel(origin.Source);
                var last = origin.EndTime - origin.StartTime;
                updateEvent.StartTime = newStartTime;
                updateEvent.EndTime = (newStartTime + last).AddSeconds(1);
                updateEvent.TimeZone = TimeZoneInfo.Utc;
                updateEvent.Id = origin.Id;

                if (!string.IsNullOrEmpty(state.UpdateMeetingInfo.RecurrencePattern) && !string.IsNullOrEmpty(origin.RecurringId))
                {
                    updateEvent.Id = origin.RecurringId;
                }

                sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
                var calendarService = ServiceManager.InitCalendarService(token as string, state.EventSource);
                var newEvent = await calendarService.UpdateEventByIdAsync(updateEvent);

                var replyMessage = await GetDetailMeetingResponseAsync(sc, newEvent, UpdateEventResponses.EventUpdated);

                await sc.Context.SendActivityAsync(replyMessage);

                if (options.SubFlowMode)
                {
                    state.UpdateMeetingInfo.Clear();
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
                if (state.UpdateMeetingInfo.NewStartDate.Any() || state.UpdateMeetingInfo.NewStartTime.Any() || state.UpdateMeetingInfo.MoveTimeSpan != 0)
                {
                    return await sc.NextAsync();
                }

                return await sc.PromptAsync(Actions.TimePrompt, new TimePromptOptions
                {
                    Prompt = TemplateEngine.GenerateActivityForLocale(UpdateEventResponses.NoNewTime) as Activity,
                    RetryPrompt = TemplateEngine.GenerateActivityForLocale(UpdateEventResponses.NoNewTimeRetry) as Activity,
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
                if (state.UpdateMeetingInfo.NewStartDate.Any() || state.UpdateMeetingInfo.NewStartTime.Any() || state.UpdateMeetingInfo.MoveTimeSpan != 0)
                {
                    var originalEvent = state.ShowMeetingInfo.FocusedEvents[0];
                    var originalStartDateTime = TimeConverter.ConvertUtcToUserTime(originalEvent.StartTime, state.GetUserTimeZone());
                    var userNow = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone());

                    if (state.UpdateMeetingInfo.NewStartDate.Any() || state.UpdateMeetingInfo.NewStartTime.Any())
                    {
                        var newStartDate = state.UpdateMeetingInfo.NewStartDate.Any() ?
                            state.UpdateMeetingInfo.NewStartDate.Last() :
                            originalStartDateTime;

                        var newStartTime = new List<DateTime>();
                        if (state.UpdateMeetingInfo.NewStartTime.Any())
                        {
                            newStartTime.AddRange(state.UpdateMeetingInfo.NewStartTime);
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

                            if (state.UpdateMeetingInfo.NewStartDateTime == null)
                            {
                                state.UpdateMeetingInfo.NewStartDateTime = newStartDateTime;
                            }

                            if (newStartDateTime >= userNow)
                            {
                                state.UpdateMeetingInfo.NewStartDateTime = newStartDateTime;
                                break;
                            }
                        }
                    }
                    else if (state.UpdateMeetingInfo.MoveTimeSpan != 0)
                    {
                        state.UpdateMeetingInfo.NewStartDateTime = originalStartDateTime.AddSeconds(state.UpdateMeetingInfo.MoveTimeSpan);
                    }
                    else
                    {
                        return await sc.BeginDialogAsync(Actions.GetNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
                    }

                    state.UpdateMeetingInfo.NewStartDateTime = TimeZoneInfo.ConvertTimeToUtc(state.UpdateMeetingInfo.NewStartDateTime.Value, state.GetUserTimeZone());

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

                        var originalStartDateTime = TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfo.FocusedEvents[0].StartTime, state.GetUserTimeZone());
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
                        state.UpdateMeetingInfo.NewStartDateTime = newStartTime;

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
                    var activity = TemplateEngine.GenerateActivityForLocale(CalendarSharedResponses.RetryTooManyResponse);
                    await sc.Context.SendActivityAsync(activity);
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