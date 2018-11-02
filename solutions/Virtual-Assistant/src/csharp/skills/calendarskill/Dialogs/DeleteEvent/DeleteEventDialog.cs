using CalendarSkill.Common;
using CalendarSkill.Dialogs.DeleteEvent.Resources;
using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
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
    public class DeleteEventDialog : CalendarSkillDialog
    {
        public DeleteEventDialog(
            SkillConfiguration services,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager)
            : base(nameof(DeleteEventDialog), services, accessor, serviceManager)
        {
            var deleteEvent = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                FromTokenToStartTime,
                ConfirmBeforeDelete,
                DeleteEventByStartTime,
            };

            var updateStartTime = new WaterfallStep[]
            {
                UpdateStartTime,
                AfterUpdateStartTime,
            };

            AddDialog(new WaterfallDialog(Actions.DeleteEvent, deleteEvent));
            AddDialog(new WaterfallDialog(Actions.UpdateStartTime, updateStartTime));

            // Set starting dialog for component
            InitialDialogId = Actions.DeleteEvent;
        }

        public async Task<DialogTurnResult> ConfirmBeforeDelete(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

                var deleteEvent = state.Events[0];
                var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(DeleteEventResponses.ConfirmDelete, deleteEvent.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", deleteEvent.ToAdaptiveCardData(state.GetUserTimeZone()));

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = replyMessage,
                    RetryPrompt = sc.Context.Activity.CreateReply(DeleteEventResponses.ConfirmDeleteFailed, _responseBuilder),
                });
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> DeleteEventByStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var deleteEvent = state.Events[0];
                    await calendarService.DeleteEventById(deleteEvent.Id);

                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(DeleteEventResponses.EventDeleted));
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

        public async Task<DialogTurnResult> FromTokenToStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.APIToken))
                {
                    return await sc.EndDialogAsync(true);
                }

                var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource);
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

                if (state.StartDate != null || state.StartTime != null || state.Title != null)
                {
                    return await sc.NextAsync();
                }

                if (((UpdateDateTimeDialogOptions)sc.Options).Reason == UpdateDateTimeDialogOptions.UpdateReason.NoEvent)
                {
                    return await sc.PromptAsync(Actions.DateTimePromptForUpdateDelete, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(DeleteEventResponses.EventWithStartTimeNotFound),
                    });
                }
                else
                {
                    return await sc.PromptAsync(Actions.DateTimePromptForUpdateDelete, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(DeleteEventResponses.NoDeleteStartTime),
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
                var events = new List<EventModel>();

                var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource);

                if (state.StartDate != null || state.StartTime != null)
                {
                    events = await GetEventsByTime(state.StartDate, state.StartTime, state.EndDate, state.EndTime, state.GetUserTimeZone(), calendarService);
                    state.StartDate = null;
                    state.StartTime = null;
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

                    var replyToConversation = sc.Context.Activity.CreateReply(DeleteEventResponses.MultipleEventsStartAtSameTime);
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
