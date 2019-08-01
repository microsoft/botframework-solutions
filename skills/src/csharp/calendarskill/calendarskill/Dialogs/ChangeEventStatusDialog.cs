using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.ChangeEventStatus;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace CalendarSkill.Dialogs
{
    public class ChangeEventStatusDialog : CalendarSkillDialogBase
    {
        private ResourceMultiLanguageGenerator _lgMultiLangEngine;

        public ChangeEventStatusDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(ChangeEventStatusDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            _lgMultiLangEngine = new ResourceMultiLanguageGenerator("ChangeEventStatusDialog.lg");

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
                    replyResponse = "ConfirmDelete";
                    retryResponse = "ConfirmDeleteFailed";
                }
                else
                {
                    replyResponse = "ConfirmAccept";
                    retryResponse = "ConfirmAcceptFailed";
                }

                var replyMessage = await GetDetailMeetingResponseAsync(sc, _lgMultiLangEngine, deleteEvent, replyResponse);
                var retryMessage = await GetDetailMeetingResponseAsync(sc, _lgMultiLangEngine, deleteEvent, retryResponse);

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

                        var eventDeletedLGResult = await _lgMultiLangEngine.Generate(sc.Context, "[EventDeleted]", null);
                        var eventDeletedPrompt = await new TextMessageActivityGenerator().CreateActivityFromText(sc.Context, eventDeletedLGResult, null);

                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ChangeEventStatusResponses.EventDeleted));
                    }
                    else
                    {
                        await calendarService.AcceptEventById(deleteEvent.Id);

                        var eventAcceptedLGResult = await _lgMultiLangEngine.Generate(sc.Context, "[EventAccepted]", null);
                        var eventAcceptedPrompt = await new TextMessageActivityGenerator().CreateActivityFromText(sc.Context, eventAcceptedLGResult, null);

                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ChangeEventStatusResponses.EventAccepted));
                    }
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
                if (state.LuisResult?.TopIntent().intent.ToString() == CalendarLuis.Intent.DeleteCalendarEntry.ToString())
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
                    var noDeleteStartTimeLGResult = await _lgMultiLangEngine.Generate(sc.Context, "[NoDeleteStartTime]", null);
                    var noDeleteStartTimePrompt = await new TextMessageActivityGenerator().CreateActivityFromText(sc.Context, noDeleteStartTimeLGResult, null);

                    var eventWithStartTimeNotFoundLGResult = await _lgMultiLangEngine.Generate(sc.Context, "[EventWithStartTimeNotFound]", null);
                    var eventWithStartTimeNotFoundPrompt = await new TextMessageActivityGenerator().CreateActivityFromText(sc.Context, eventWithStartTimeNotFoundLGResult, null);

                    return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, state.GetUserTimeZone())
                    {
                        Prompt = (Activity)noDeleteStartTimePrompt,
                        RetryPrompt = (Activity)eventWithStartTimeNotFoundPrompt
                    }, cancellationToken);
                }
                else
                {
                    var noAcceptStartTimeLGResult = await _lgMultiLangEngine.Generate(sc.Context, "[NoAcceptStartTime]", null);
                    var noAcceptStartTimePrompt = await new TextMessageActivityGenerator().CreateActivityFromText(sc.Context, noAcceptStartTimeLGResult, null);

                    var eventWithStartTimeNotFoundLGResult = await _lgMultiLangEngine.Generate(sc.Context, "[EventWithStartTimeNotFound]", null);
                    var eventWithStartTimeNotFoundPrompt = await new TextMessageActivityGenerator().CreateActivityFromText(sc.Context, eventWithStartTimeNotFoundLGResult, null);

                    return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, state.GetUserTimeZone())
                    {
                        Prompt = (Activity)noAcceptStartTimePrompt,
                        RetryPrompt = (Activity)eventWithStartTimeNotFoundPrompt
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

                    var prompt = await GetGeneralMeetingListResponseAsync(sc, _lgMultiLangEngine, CalendarCommonStrings.MeetingsToChoose, state.Events, "MultipleEventsStartAtSameTime", null);

                    options.Prompt = prompt;

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