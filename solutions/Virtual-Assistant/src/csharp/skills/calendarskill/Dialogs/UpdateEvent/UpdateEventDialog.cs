using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Common;
using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Dialogs.Shared.Resources.Strings;
using CalendarSkill.Dialogs.UpdateEvent.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace CalendarSkill
{
    public class UpdateEventDialog : CalendarSkillDialog
    {
        public UpdateEventDialog(
            ISkillConfiguration services,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager)
            : base(nameof(UpdateEventDialog), services, accessor, serviceManager)
        {
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
            AddDialog(new WaterfallDialog(Actions.UpdateEventTime, updateEvent));
            AddDialog(new WaterfallDialog(Actions.UpdateStartTime, updateStartTime));
            AddDialog(new WaterfallDialog(Actions.UpdateNewStartTime, updateNewStartTime));

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
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(UpdateEventResponses.NotEventOrganizer));
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
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
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
                var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(UpdateEventResponses.ConfirmUpdate, origin.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", origin.ToAdaptiveCardData(state.GetUserTimeZone()));

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = replyMessage,
                    RetryPrompt = sc.Context.Activity.CreateReply(UpdateEventResponses.ConfirmUpdateFailed, ResponseBuilder),
                });
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
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

                    if (sc.Context.Activity.Text.Contains(CalendarCommonStrings.DailyToken)
                        || sc.Context.Activity.Text.Contains(CalendarCommonStrings.WeeklyToken)
                        || sc.Context.Activity.Text.Contains(CalendarCommonStrings.MonthlyToken))
                    {
                        updateEvent.Id = ((Microsoft.Graph.Event)origin.Value).SeriesMasterId;
                    }

                    var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);
                    var newEvent = await calendarService.UpdateEventById(updateEvent);

                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(UpdateEventResponses.EventUpdated, newEvent.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", newEvent.ToAdaptiveCardData(state.GetUserTimeZone()));
                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarSharedResponses.ActionEnded));
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

        public async Task<DialogTurnResult> GetNewEventTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.StartDate.Any() || state.StartTime.Any() || state.MoveTimeSpan != 0)
                {
                    return await sc.ContinueDialogAsync();
                }

                if (((UpdateDateTimeDialogOptions)sc.Options).Reason == UpdateDateTimeDialogOptions.UpdateReason.NotFound)
                {
                    return await sc.PromptAsync(Actions.DateTimePrompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(UpdateEventResponses.NoNewTime) });
                }
                else
                {
                    return await sc.PromptAsync(Actions.DateTimePrompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CalendarSharedResponses.DidntUnderstandMessage) });
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> AfterGetNewEventTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.StartDate.Any() || state.StartTime.Any() || state.MoveTimeSpan != 0)
                {
                    var originalEvent = state.Events[0];
                    var originalStartDateTime = TimeConverter.ConvertUtcToUserTime(originalEvent.StartTime, state.GetUserTimeZone());
                    var userNow = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone());

                    if (state.StartDate.Any() || state.StartTime.Any())
                    {
                        var newStartDate = state.StartDate.Any() ?
                            state.StartDate.Last() :
                            originalStartDateTime;

                        var newStartTime = new List<DateTime>();
                        if (state.StartTime.Any())
                        {
                            foreach (var time in state.StartTime)
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

                        var isRelativeTime = IsRelativeTime(sc.Context.Activity.Text, dateTimeResolutions.First().Value, dateTimeResolutions.First().Timex);
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
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
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
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> UpdateStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                if (state.OriginalStartDate.Any() || state.OriginalStartTime.Any() || state.Title != null)
                {
                    return await sc.NextAsync();
                }

                if (((UpdateDateTimeDialogOptions)sc.Options).Reason == UpdateDateTimeDialogOptions.UpdateReason.NoEvent)
                {
                    return await sc.PromptAsync(Actions.DateTimePromptForUpdateDelete, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(UpdateEventResponses.EventWithStartTimeNotFound),
                    });
                }
                else
                {
                    return await sc.PromptAsync(Actions.DateTimePromptForUpdateDelete, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(UpdateEventResponses.NoUpdateStartTime),
                    });
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> AfterUpdateStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var events = new List<EventModel>();

                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);
                var searchByEntities = state.OriginalStartDate.Any() || state.OriginalStartTime.Any() || state.Title != null;

                if (state.OriginalStartDate.Any() || state.OriginalStartTime.Any())
                {
                    events = await GetEventsByTime(state.OriginalStartDate, state.OriginalStartTime, state.OriginalEndDate, state.OriginalEndTime, state.GetUserTimeZone(), calendarService);
                    state.OriginalStartDate = new List<DateTime>();
                    state.OriginalStartTime = new List<DateTime>();
                    state.OriginalEndDate = new List<DateTime>();
                    state.OriginalStartTime = new List<DateTime>();
                }
                else if (state.Title != null)
                {
                    events = await calendarService.GetEventsByTitle(state.Title);
                    state.Title = null;
                }
                else
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                    try
                    {
                        IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                        if (dateTimeResolutions.Count > 0)
                        {
                            foreach (var resolution in dateTimeResolutions)
                            {
                                var startTimeValue = DateTime.Parse(resolution.Value);
                                if (startTimeValue == null)
                                {
                                    continue;
                                }

                                var dateTimeConvertType = resolution.Timex;
                                bool isRelativeTime = IsRelativeTime(sc.Context.Activity.Text, dateTimeResolutions.First().Value, dateTimeResolutions.First().Timex);
                                startTimeValue = isRelativeTime ? TimeZoneInfo.ConvertTime(startTimeValue, TimeZoneInfo.Local, state.GetUserTimeZone()) : startTimeValue;

                                startTimeValue = TimeConverter.ConvertLuisLocalToUtc(startTimeValue, state.GetUserTimeZone());
                                events = await calendarService.GetEventsByStartTime(startTimeValue);
                                if (events != null && events.Count > 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                    }

                    if (events == null || events.Count <= 0)
                    {
                        state.Title = userInput;
                        events = await calendarService.GetEventsByTitle(userInput);
                    }
                }

                state.Events = events;
                if (events.Count <= 0)
                {
                    if (searchByEntities)
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(UpdateEventResponses.EventWithStartTimeNotFound));
                        state.Clear();
                        return await sc.CancelAllDialogsAsync();
                    }
                    else
                    {
                        return await sc.BeginDialogAsync(Actions.UpdateStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NoEvent));
                    }
                }
                else if (events.Count > 1)
                {
                    var options = new PromptOptions()
                    {
                        Choices = new List<Choice>(),
                    };

                    for (var i = 0; i < events.Count; i++)
                    {
                        var item = events[i];
                        var choice = new Choice()
                        {
                            Value = string.Empty,
                            Synonyms = new List<string> { (i + 1).ToString(), item.Title },
                        };
                        options.Choices.Add(choice);
                    }

                    var replyToConversation = sc.Context.Activity.CreateReply(UpdateEventResponses.MultipleEventsStartAtSameTime);
                    replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyToConversation.Attachments = new List<Microsoft.Bot.Schema.Attachment>();

                    var cardsData = new List<CalendarCardData>();
                    foreach (var item in events)
                    {
                        var meetingCard = item.ToAdaptiveCardData(state.GetUserTimeZone());
                        var replyTemp = sc.Context.Activity.CreateAdaptiveCardReply(CalendarMainResponses.GreetingMessage, item.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", meetingCard);
                        replyToConversation.Attachments.Add(replyTemp.Attachments[0]);
                    }

                    options.Prompt = replyToConversation;

                    return await sc.PromptAsync(Actions.EventChoice, options);
                }
                else
                {
                    return await sc.EndDialogAsync(true);
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }
    }
}
