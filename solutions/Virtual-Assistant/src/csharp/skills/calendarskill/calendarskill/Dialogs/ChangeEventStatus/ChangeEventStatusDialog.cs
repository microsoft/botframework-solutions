using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.ChangeEventStatus.Resources;
using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared;
using CalendarSkill.Dialogs.Shared.Prompts.Options;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;

namespace CalendarSkill.Dialogs.ChangeEventStatus
{
    public class ChangeEventStatusDialog : CalendarSkillDialog
    {
        public ChangeEventStatusDialog(
            SkillConfigurationBase services,
            ResponseManager responseManager,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ChangeEventStatusDialog), services, responseManager, accessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var changeEventStatus = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                FromTokenToStartTime,
                ConfirmBeforeAction,
                ChangeEventStatus,
            };

            var updateStartTime = new WaterfallStep[]
            {
                UpdateStartTime,
                AfterUpdateStartTime,
            };

            AddDialog(new WaterfallDialog(Actions.ChangeEventStatus, changeEventStatus) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateStartTime, updateStartTime) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.ChangeEventStatus;
        }

        public async Task<DialogTurnResult> ConfirmBeforeAction(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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
                string replyResponse;
                string retryResponse;
                if (state.NewEventStatus == EventStatus.Cancelled)
                {
                    replyResponse = ChangeEventStatusResponses.ConfirmDelete;
                    retryResponse = ChangeEventStatusResponses.ConfirmDeleteFailed;
                }
                else
                {
                    replyResponse = ChangeEventStatusResponses.ConfirmAccept;
                    retryResponse = ChangeEventStatusResponses.ConfirmAcceptFailed;
                }

                var card = new Card(deleteEvent.OnlineMeetingUrl == null ? "CalendarCardNoJoinButton" : "CalendarCard", deleteEvent.ToAdaptiveCardData(state.GetUserTimeZone()));
                var replyMessage = ResponseManager.GetCardResponse(replyResponse, card);
                var retryMessage = ResponseManager.GetResponse(retryResponse);

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

        public async Task<DialogTurnResult> ChangeEventStatus(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var deleteEvent = state.Events[0];
                    if (state.NewEventStatus == EventStatus.Cancelled)
                    {
                        if (deleteEvent.IsOrganizer)
                        {
                            await calendarService.DeleteEventById(deleteEvent.Id);
                        }
                        else
                        {
                            await calendarService.DeclineEventById(deleteEvent.Id);
                        }

                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ChangeEventStatusResponses.EventDeleted));
                    }
                    else
                    {
                        await calendarService.AcceptEventById(deleteEvent.Id);
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ChangeEventStatusResponses.EventAccepted));
                    }
                }
                else
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CalendarSharedResponses.ActionEnded));
                }

                if (state.IsActionFromSummary)
                {
                    state.ClearChangeStautsInfo();
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

        public async Task<DialogTurnResult> FromTokenToStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.LuisResult?.TopIntent().intent.ToString() == CalendarLU.Intent.DeleteCalendarEntry.ToString())
                {
                    state.NewEventStatus = EventStatus.Cancelled;
                }
                else
                {
                    state.NewEventStatus = EventStatus.Accepted;
                }

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

                if (state.Events.Count > 0)
                {
                    return await sc.NextAsync();
                }

                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);

                if (state.StartDate.Any() || state.StartTime.Any())
                {
                    state.Events = await GetEventsByTime(state.StartDate, state.StartTime, state.EndDate, state.EndTime, state.GetUserTimeZone(), calendarService);
                    state.StartDate = new List<DateTime>();
                    state.StartTime = new List<DateTime>();
                    state.EndDate = new List<DateTime>();
                    state.EndTime = new List<DateTime>();
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

                if (state.NewEventStatus == EventStatus.Cancelled)
                {
                    return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, state.GetUserTimeZone())
                    {
                        Prompt = ResponseManager.GetResponse(ChangeEventStatusResponses.NoDeleteStartTime),
                        RetryPrompt = ResponseManager.GetResponse(ChangeEventStatusResponses.EventWithStartTimeNotFound)
                    }, cancellationToken);
                }
                else
                {
                    return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, state.GetUserTimeZone())
                    {
                        Prompt = ResponseManager.GetResponse(ChangeEventStatusResponses.NoAcceptStartTime),
                        RetryPrompt = ResponseManager.GetResponse(ChangeEventStatusResponses.EventWithStartTimeNotFound)
                    }, cancellationToken);
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
                        var card = new Card()
                        {
                            Name = item.OnlineMeetingUrl == null ? "CalendarCardNoJoinButton" : "CalendarCard",
                            Data = item.ToAdaptiveCardData(state.GetUserTimeZone())
                        };
                        cards.Add(card);
                    }

                    options.Prompt = ResponseManager.GetCardResponse(
                        templateId: ChangeEventStatusResponses.MultipleEventsStartAtSameTime,
                        cards: cards);

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