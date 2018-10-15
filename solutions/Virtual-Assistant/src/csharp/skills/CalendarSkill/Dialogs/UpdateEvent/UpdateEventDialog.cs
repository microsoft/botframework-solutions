using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Dialogs.UpdateEvent.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarSkill
{
    public class UpdateEventDialog : CalendarSkillDialog
    {
        public UpdateEventDialog(
            SkillConfiguration services,
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
                var state = await _accessor.GetAsync(sc.Context);
                if (sc.Result != null && state.Events.Count > 1)
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
                var state = await _accessor.GetAsync(sc.Context);

                var newStartTime = (DateTime)state.NewStartDateTime;
                newStartTime = DateTime.SpecifyKind(newStartTime, DateTimeKind.Local);

                // DateTime newStartTime = DateTime.Parse((string)state.NewStartDateTime);
                var origin = state.Events[0];
                var last = origin.EndTime - origin.StartTime;
                origin.StartTime = newStartTime;
                origin.EndTime = (newStartTime + last).AddSeconds(1);
                var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(UpdateEventResponses.ConfirmUpdate, origin.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", origin.ToAdaptiveCardData());

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = replyMessage,
                    RetryPrompt = sc.Context.Activity.CreateReply(UpdateEventResponses.ConfirmUpdateFailed, _responseBuilder),
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
                    var state = await _accessor.GetAsync(sc.Context);

                    var newStartTime = (DateTime)state.NewStartDateTime;

                    var origin = state.Events[0];
                    var updateEvent = new EventModel(origin.Source);
                    var last = origin.EndTime - origin.StartTime;
                    updateEvent.StartTime = TimeZoneInfo.ConvertTimeToUtc(newStartTime, state.GetUserTimeZone());
                    updateEvent.EndTime = TimeZoneInfo.ConvertTimeToUtc((newStartTime + last).AddSeconds(1), state.GetUserTimeZone());
                    updateEvent.TimeZone = TimeZoneInfo.Utc;
                    updateEvent.Id = origin.Id;
                    var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource, state.GetUserTimeZone());
                    var newEvent = await calendarService.UpdateEventById(updateEvent);

                    newEvent.StartTime = TimeZoneInfo.ConvertTimeFromUtc(newEvent.StartTime, state.GetUserTimeZone());
                    newEvent.EndTime = TimeZoneInfo.ConvertTimeFromUtc(newEvent.EndTime, state.GetUserTimeZone());
                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(UpdateEventResponses.EventUpdated, newEvent.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", newEvent.ToAdaptiveCardData());
                    await sc.Context.SendActivityAsync(replyMessage);
                    state.Clear();
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarSharedResponses.ActionEnded));
                }

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
                var state = await _accessor.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                    var newStartTime = DateTime.Parse(dateTimeResolutions.First().Value);
                    var dateTimeConvertType = dateTimeResolutions.First().Timex;

                    if (newStartTime != null)
                    {
                        var isRelativeTime = IsRelativeTime(sc.Context.Activity.Text, dateTimeResolutions.First().Value, dateTimeResolutions.First().Timex);
                        state.NewStartDateTime = isRelativeTime ? TimeZoneInfo.ConvertTime(newStartTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : newStartTime;
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
                var state = await _accessor.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.APIToken))
                {
                    return await sc.EndDialogAsync(true);
                }

                var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource, state.GetUserTimeZone());

                if (state.StartDateTime == null)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
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

        public async Task<DialogTurnResult> UpdateStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);

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
                var state = await _accessor.GetAsync(sc.Context);
                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                DateTime? startTime = null;
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

                var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource, state.GetUserTimeZone());

                var events = new List<EventModel>();
                if (startTime != null)
                {
                    state.StartDateTime = startTime;
                    startTime = DateTime.SpecifyKind(startTime.Value, DateTimeKind.Local);
                    events = await calendarService.GetEventsByStartTime(startTime.Value);
                }
                else
                {
                    state.Title = userInput;
                    events = await calendarService.GetEventsByTitle(userInput);
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
                        var meetingCard = item.ToAdaptiveCardData();
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
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarSharedResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessor.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }
    }
}
