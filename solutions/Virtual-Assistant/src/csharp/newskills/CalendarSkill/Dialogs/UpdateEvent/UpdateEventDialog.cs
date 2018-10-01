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
            AddDialog(new WaterfallDialog(Action.UpdateEventTime, updateEvent));
            AddDialog(new WaterfallDialog(Action.UpdateStartTime, updateStartTime));
            AddDialog(new WaterfallDialog(Action.UpdateNewStartTime, updateNewStartTime));

            // Set starting dialog for component
            InitialDialogId = Action.UpdateEventTime;
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
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.NotEventOrganizer));
                    state.Clear();
                    return await sc.EndDialogAsync(true);
                }
                else if (state.NewStartDateTime == null)
                {
                    return await sc.BeginDialogAsync(Action.UpdateNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                return await HandleDialogExceptions(sc);
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
                var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(CalendarBotResponses.ConfirmUpdate, origin.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", origin.ToAdaptiveCardData());

                return await sc.PromptAsync(Action.TakeFurtherAction, new PromptOptions
                {
                    Prompt = replyMessage,
                    RetryPrompt = sc.Context.Activity.CreateReply(CalendarBotResponses.ConfirmUpdateFailed, _responseBuilder),
                });
            }
            catch
            {
                return await HandleDialogExceptions(sc);
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
                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(CalendarBotResponses.EventUpdated, newEvent.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", newEvent.ToAdaptiveCardData());
                    await sc.Context.SendActivityAsync(replyMessage);
                    state.Clear();
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.ActionEnded));
                }

                return await sc.EndDialogAsync(true);
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        public async Task<DialogTurnResult> GetNewEventTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (((UpdateDateTimeDialogOptions)sc.Options).Reason == UpdateDateTimeDialogOptions.UpdateReason.NotFound)
                {
                    return await sc.PromptAsync(Action.DateTimePrompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoNewTime) });
                }
                else
                {
                    return await sc.PromptAsync(Action.DateTimePrompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.DidntUnderstandMessage) });
                }
            }
            catch
            {
                return await HandleDialogExceptions(sc);
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
                        bool isRelativeTime = IsRelativeTime(sc.Context.Activity.Text, dateTimeResolutions.First().Value, dateTimeResolutions.First().Timex);
                        state.NewStartDateTime = isRelativeTime ? TimeZoneInfo.ConvertTime(newStartTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : newStartTime;
                        return await sc.ContinueDialogAsync();
                    }
                    else
                    {
                        return await sc.BeginDialogAsync(Action.UpdateNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime));
                    }
                }
                else
                {
                    return await sc.BeginDialogAsync(Action.UpdateNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime));
                }
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }
    }
}
