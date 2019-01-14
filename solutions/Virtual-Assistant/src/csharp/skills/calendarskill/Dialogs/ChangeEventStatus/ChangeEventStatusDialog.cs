using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Common;
using CalendarSkill.Dialogs.ChangeEventStatus.Resources;
using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;

namespace CalendarSkill.Dialogs.ChangeEventStatus
{
    public class ChangeEventStatusDialog : CalendarSkillDialog
    {
        public ChangeEventStatusDialog(
            SkillConfigurationBase services,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ChangeEventStatusDialog), services, accessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

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

            AddDialog(new WaterfallDialog(Actions.DeleteEvent, deleteEvent) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateStartTime, updateStartTime) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.DeleteEvent;
        }

        public async Task<DialogTurnResult> ConfirmBeforeDelete(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (sc.Result != null && state.Events.Count > 1)
                {
                    var events = state.Events;
                    state.Events = new List<EventModel>
                {
                    events[(sc.Result as FoundChoice).Index],
                };
                }

                var deleteEvent = state.Events[0];
                var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(ChangeEventStatusResponses.ConfirmDelete, deleteEvent.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", deleteEvent.ToAdaptiveCardData(state.GetUserTimeZone()));

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = replyMessage,
                    RetryPrompt = sc.Context.Activity.CreateReply(ChangeEventStatusResponses.ConfirmDeleteFailed, ResponseBuilder),
                });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> DeleteEventByStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var deleteEvent = state.Events[0];
                    await calendarService.DeleteEventById(deleteEvent.Id);

                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ChangeEventStatusResponses.EventDeleted));
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarSharedResponses.ActionEnded));
                }

                state.Clear();
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
                if (state.StartDateTime == null)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
                }
                else
                {
                    return await sc.NextAsync();
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

        public async Task<DialogTurnResult> UpdateStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                if (state.Events.Count > 0 || state.StartDate.Any() || state.StartTime.Any() || state.Title != null)
                {
                    return await sc.NextAsync();
                }

                if (((UpdateDateTimeDialogOptions)sc.Options).Reason == UpdateDateTimeDialogOptions.UpdateReason.NoEvent)
                {
                    return await sc.PromptAsync(Actions.DateTimePromptForUpdateDelete, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(ChangeEventStatusResponses.EventWithStartTimeNotFound),
                    });
                }
                else
                {
                    return await sc.PromptAsync(Actions.DateTimePromptForUpdateDelete, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(ChangeEventStatusResponses.NoDeleteStartTime),
                    });
                }
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
                var events = new List<EventModel>();

                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);
                var searchByEntities = state.StartDate.Any() || state.StartTime.Any() || state.Title != null;

                if (state.Events.Count < 1)
                {
                    if (state.StartDate.Any() || state.StartTime.Any())
                    {
                        events = await GetEventsByTime(state.StartDate, state.StartTime, state.EndDate, state.EndTime, state.GetUserTimeZone(), calendarService);
                        state.StartDate = new List<DateTime>();
                        state.StartTime = new List<DateTime>();
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
                                    if (resolution.Value == null)
                                    {
                                        continue;
                                    }

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
                }

                if (state.Events.Count <= 0)
                {
                    if (searchByEntities)
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ChangeEventStatusResponses.EventWithStartTimeNotFound));
                        state.Clear();
                        return await sc.CancelAllDialogsAsync();
                    }
                    else
                    {
                        return await sc.BeginDialogAsync(Actions.UpdateStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NoEvent));
                    }
                }
                else if (state.Events.Count > 1)
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

                    var replyToConversation = sc.Context.Activity.CreateReply(ChangeEventStatusResponses.MultipleEventsStartAtSameTime);
                    replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyToConversation.Attachments = new List<Microsoft.Bot.Schema.Attachment>();

                    var cardsData = new List<CalendarCardData>();
                    foreach (var item in state.Events)
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