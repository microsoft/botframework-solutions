using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Common;
using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Dialogs.UpdateEvent.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;

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
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var state = await Accessor.GetAsync(sc.Context);

                    var newStartTime = (DateTime)state.NewStartDateTime;
                    var origin = state.Events[0];
                    var updateEvent = new EventModel(origin.Source);
                    var last = origin.EndTime - origin.StartTime;
                    updateEvent.StartTime = newStartTime;
                    updateEvent.EndTime = (newStartTime + last).AddSeconds(1);
                    updateEvent.TimeZone = TimeZoneInfo.Utc;
                    updateEvent.Id = origin.Id;
                    var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);
                    var newEvent = await calendarService.UpdateEventById(updateEvent);

                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(UpdateEventResponses.EventUpdated, newEvent.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", newEvent.ToAdaptiveCardData(state.GetUserTimeZone()));
                    await sc.Context.SendActivityAsync(replyMessage);
                    state.Clear();
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarSharedResponses.ActionEnded));
                }

                return await sc.EndDialogAsync(true);
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> GetNewEventTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.StartDate != null || state.StartTime != null)
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
                if (state.StartDate != null || state.StartTime != null)
                {
                    var originalEvent = state.Events[0];
                    var originalStartDateTime = TimeConverter.ConvertUtcToUserTime(originalEvent.StartTime, state.GetUserTimeZone());

                    if (state.StartDate != null && state.StartTime != null)
                    {
                        state.NewStartDateTime = new DateTime(
                            state.StartDate.Value.Year,
                            state.StartDate.Value.Month,
                            state.StartDate.Value.Day,
                            state.StartTime.Value.Hour,
                            state.StartTime.Value.Minute,
                            state.StartTime.Value.Second);
                    }
                    else if (state.StartDate != null)
                    {
                        state.NewStartDateTime = new DateTime(
                            state.StartDate.Value.Year,
                            state.StartDate.Value.Month,
                            state.StartDate.Value.Day,
                            originalStartDateTime.Hour,
                            originalStartDateTime.Minute,
                            originalStartDateTime.Second);
                    }
                    else
                    {
                        state.NewStartDateTime = new DateTime(
                            originalStartDateTime.Year,
                            originalStartDateTime.Month,
                            originalStartDateTime.Day,
                            state.StartTime.Value.Hour,
                            state.StartTime.Value.Minute,
                            state.StartTime.Value.Second);
                    }

                    state.NewStartDateTime = TimeZoneInfo.ConvertTimeToUtc(state.NewStartDateTime.Value, state.GetUserTimeZone());

                    return await sc.ContinueDialogAsync();
                }
                else if (sc.Result != null)
                {
                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                    var newStartTime = DateTime.Parse(dateTimeResolutions.First().Value);

                    var dateTimeConvertType = dateTimeResolutions.First().Timex;

                    if (newStartTime != null)
                    {
                        var isRelativeTime = IsRelativeTime(sc.Context.Activity.Text, dateTimeResolutions.First().Value, dateTimeResolutions.First().Timex);
                        if (isRelativeTime)
                        {
                            newStartTime = DateTime.SpecifyKind(newStartTime, DateTimeKind.Local);
                        }

                        state.NewStartDateTime = isRelativeTime ? TimeConverter.ConvertLuisLocalToUtc(newStartTime, state.GetUserTimeZone()) : TimeZoneInfo.ConvertTimeToUtc(newStartTime, state.GetUserTimeZone());

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

                if (state.OriginalStartDate != null || state.OriginalStartTime != null || state.Title != null)
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

                if (state.OriginalStartDate != null || state.OriginalStartTime != null)
                {
                    events = await GetEventsByTime(state.OriginalStartDate, state.OriginalStartTime, state.OriginalEndDate, state.OriginalEndTime, state.GetUserTimeZone(), calendarService);
                    state.OriginalStartDate = null;
                    state.OriginalStartTime = null;
                    state.OriginalEndDate = null;
                    state.OriginalStartTime = null;
                }
                else if (state.Title != null)
                {
                    events = await calendarService.GetEventsByTitle(state.Title);
                    state.Title = null;
                }
                else
                {
                    DateTime? startTime = null;
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                    try
                    {
                        IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                        if (dateTimeResolutions.Count > 0)
                        {
                            startTime = DateTime.Parse(dateTimeResolutions.First().Value);
                            var dateTimeConvertType = dateTimeResolutions.First().Timex;
                            bool isRelativeTime = IsRelativeTime(sc.Context.Activity.Text, dateTimeResolutions.First().Value, dateTimeResolutions.First().Timex);
                            startTime = isRelativeTime ? TimeZoneInfo.ConvertTime(startTime.Value, TimeZoneInfo.Local, state.GetUserTimeZone()) : startTime;
                        }
                    }
                    catch
                    {
                    }

                    if (startTime != null)
                    {
                        startTime = TimeConverter.ConvertLuisLocalToUtc(startTime.Value, state.GetUserTimeZone());
                        events = await calendarService.GetEventsByStartTime(startTime.Value);
                    }
                    else
                    {
                        state.Title = userInput;
                        events = await calendarService.GetEventsByTitle(userInput);
                    }
                }

                state.Events = events;
                if (events.Count <= 0)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NoEvent));
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
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarSharedResponses.CalendarErrorMessage, ResponseBuilder));
                var state = await Accessor.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }
    }
}
