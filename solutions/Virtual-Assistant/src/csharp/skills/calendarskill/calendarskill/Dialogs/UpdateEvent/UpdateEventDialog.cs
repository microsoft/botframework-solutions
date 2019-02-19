using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Common;
using CalendarSkill.Dialogs.Shared;
using CalendarSkill.Dialogs.Shared.Prompts.Options;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Dialogs.UpdateEvent.Resources;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace CalendarSkill.Dialogs.UpdateEvent
{
    public class UpdateEventDialog : CalendarSkillDialog
    {
        public UpdateEventDialog(
            SkillConfigurationBase services,
            ResponseManager responseManager,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(UpdateEventDialog), services, responseManager, accessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;
            var updateEvent = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                FromTokenToStartTime,
                FromEventsToNewDate,
                ConfirmBeforeUpdate,
                UpdateEventTime,
            };

            var updateStartTime = new WaterfallStep[]
            {
                UpdateStartTime,
                AfterUpdateStartTime,
            };

            var updateNewStartTime = new WaterfallStep[]
            {
                GetNewEventTime,
                AfterGetNewEventTime,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.UpdateEventTime, updateEvent) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateStartTime, updateStartTime) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateNewStartTime, updateNewStartTime) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.UpdateEventTime;
        }

        public async Task<DialogTurnResult> FromEventsToNewDate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (sc.Result != null && sc.Result is FoundChoice && state.Events.Count > 1)
                {
                    var events = state.Events;
                    state.Events = new List<EventModel>
                {
                    events[(sc.Result as FoundChoice).Index],
                };
                }

                var origin = state.Events[0];
                if (!origin.IsOrganizer)
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(UpdateEventResponses.NotEventOrganizer));
                    state.Clear();
                    return await sc.EndDialogAsync(true);
                }
                else if (state.NewStartDateTime == null)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> ConfirmBeforeUpdate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var newStartTime = (DateTime)state.NewStartDateTime;
                var origin = state.Events[0];
                var last = origin.EndTime - origin.StartTime;
                origin.StartTime = newStartTime;
                origin.EndTime = (newStartTime + last).AddSeconds(1);

                var replyMessage = ResponseManager.GetCardResponse(
                    UpdateEventResponses.ConfirmUpdate,
                    new Card()
                    {
                        Name = origin.OnlineMeetingUrl == null ? "CalendarCardNoJoinButton" : "CalendarCard",
                        Data = origin.ToAdaptiveCardData(state.GetUserTimeZone())
                    });

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

        public async Task<DialogTurnResult> UpdateEventTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var newStartTime = (DateTime)state.NewStartDateTime;
                    var origin = state.Events[0];
                    var updateEvent = new EventModel(origin.Source);
                    var last = origin.EndTime - origin.StartTime;
                    updateEvent.StartTime = newStartTime;
                    updateEvent.EndTime = (newStartTime + last).AddSeconds(1);
                    updateEvent.TimeZone = TimeZoneInfo.Utc;
                    updateEvent.Id = origin.Id;

                    if (!string.IsNullOrEmpty(state.RecurrencePattern) && !string.IsNullOrEmpty(origin.RecurringId))
                    {
                        updateEvent.Id = origin.RecurringId;
                    }

                    var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);
                    var newEvent = await calendarService.UpdateEventById(updateEvent);

                    var replyMessage = ResponseManager.GetCardResponse(
                        UpdateEventResponses.EventUpdated,
                        new Card()
                        {
                            Name = newEvent.OnlineMeetingUrl == null ? "CalendarCardNoJoinButton" : "CalendarCard",
                            Data = newEvent.ToAdaptiveCardData(state.GetUserTimeZone())
                        });

                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CalendarSharedResponses.ActionEnded));
                }

                if (state.IsActionFromSummary)
                {
                    state.ClearUpdateEventInfo();
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

        public async Task<DialogTurnResult> GetNewEventTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.NewStartDate.Any() || state.NewStartTime.Any() || state.MoveTimeSpan != 0)
                {
                    return await sc.ContinueDialogAsync();
                }

                return await sc.PromptAsync(Actions.TimePrompt, new PromptOptions
                {
                    Prompt = ResponseManager.GetResponse(UpdateEventResponses.NoNewTime),
                    RetryPrompt = ResponseManager.GetResponse(UpdateEventResponses.NoNewTime_Retry)
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterGetNewEventTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.NewStartDate.Any() || state.NewStartTime.Any() || state.MoveTimeSpan != 0)
                {
                    var originalEvent = state.Events[0];
                    var originalStartDateTime = TimeConverter.ConvertUtcToUserTime(originalEvent.StartTime, state.GetUserTimeZone());
                    var userNow = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone());

                    if (state.NewStartDate.Any() || state.NewStartTime.Any())
                    {
                        var newStartDate = state.NewStartDate.Any() ?
                            state.NewStartDate.Last() :
                            originalStartDateTime;

                        var newStartTime = new List<DateTime>();
                        if (state.NewStartTime.Any())
                        {
                            foreach (var time in state.NewStartTime)
                            {
                                var newStartDateTime = new DateTime(
                                    newStartDate.Year,
                                    newStartDate.Month,
                                    newStartDate.Day,
                                    time.Hour,
                                    time.Minute,
                                    time.Second);

                                if (state.NewStartDateTime == null)
                                {
                                    state.NewStartDateTime = newStartDateTime;
                                }

                                if (newStartDateTime >= userNow)
                                {
                                    state.NewStartDateTime = newStartDateTime;
                                    break;
                                }
                            }
                        }
                    }
                    else if (state.MoveTimeSpan != 0)
                    {
                        state.NewStartDateTime = originalStartDateTime.AddSeconds(state.MoveTimeSpan);
                    }
                    else
                    {
                        return await sc.BeginDialogAsync(Actions.UpdateNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
                    }

                    state.NewStartDateTime = TimeZoneInfo.ConvertTimeToUtc(state.NewStartDateTime.Value, state.GetUserTimeZone());

                    return await sc.ContinueDialogAsync();
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

                        var isRelativeTime = IsRelativeTime(sc.Context.Activity.Text, resolution.Value, dateTimeConvertTypeString);
                        if (isRelativeTime)
                        {
                            dateTimeValue = DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Local);
                        }

                        dateTimeValue = isRelativeTime ? TimeZoneInfo.ConvertTime(dateTimeValue, TimeZoneInfo.Local, state.GetUserTimeZone()) : dateTimeValue;
                        var originalStartDateTime = TimeConverter.ConvertUtcToUserTime(state.Events[0].StartTime, state.GetUserTimeZone());
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
                        state.NewStartDateTime = newStartTime;

                        return await sc.ContinueDialogAsync();
                    }
                    else
                    {
                        return await sc.BeginDialogAsync(Actions.UpdateNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime));
                    }
                }
                else
                {
                    return await sc.BeginDialogAsync(Actions.UpdateNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime));
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
                if (string.IsNullOrEmpty(state.APIToken))
                {
                    return await sc.EndDialogAsync(true);
                }

                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);
                return await sc.BeginDialogAsync(Actions.UpdateStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
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

                if (state.Events.Count > 0)
                {
                    return await sc.NextAsync();
                }

                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);

                if (state.OriginalStartDate.Any() || state.OriginalStartTime.Any())
                {
                    state.Events = await GetEventsByTime(state.OriginalStartDate, state.OriginalStartTime, state.OriginalEndDate, state.OriginalEndTime, state.GetUserTimeZone(), calendarService);
                    state.OriginalStartDate = new List<DateTime>();
                    state.OriginalStartTime = new List<DateTime>();
                    state.OriginalEndDate = new List<DateTime>();
                    state.OriginalEndTime = new List<DateTime>();
                    if (state.Events.Count > 0)
                    {
                        return await sc.NextAsync();
                    }
                }

                if (state.Title != null)
                {
                    state.Events = await calendarService.GetEventsByTitle(state.Title);
                    state.Title = null;
                    if (state.Events.Count > 0)
                    {
                        return await sc.NextAsync();
                    }
                }

                return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, state.GetUserTimeZone())
                {
                    Prompt = ResponseManager.GetResponse(UpdateEventResponses.NoUpdateStartTime),
                    RetryPrompt = ResponseManager.GetResponse(UpdateEventResponses.EventWithStartTimeNotFound)
                }, cancellationToken);
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
                    state.Events = sc.Result as List<EventModel>;
                }

                if (state.Events.Count == 0)
                {
                    // should not doto this part. add log here for safe
                    await HandleDialogExceptions(sc, new Exception("Unexpect zero events count"));
                    return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                }
                else
                if (state.Events.Count > 1)
                {
                    var options = new PromptOptions()
                    {
                        Choices = new List<Choice>(),
                    };

                    for (var i = 0; i < state.Events.Count; i++)
                    {
                        var item = state.Events[i];
                        var choice = new Choice()
                        {
                            Value = string.Empty,
                            Synonyms = new List<string> { (i + 1).ToString(), item.Title },
                        };
                        options.Choices.Add(choice);
                    }

                    var cards = new List<Card>();
                    foreach (var item in state.Events)
                    {
                        cards.Add(new Card()
                        {
                            Name = item.OnlineMeetingUrl == null ? "CalendarCardNoJoinButton" : "CalendarCard",
                            Data = item.ToAdaptiveCardData(state.GetUserTimeZone())
                        });
                    }

                    options.Prompt = ResponseManager.GetCardResponse(
                        UpdateEventResponses.MultipleEventsStartAtSameTime,
                        cards);

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