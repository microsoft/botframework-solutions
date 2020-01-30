using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Prompts;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.FindMeetingRoom;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Azure.Search;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Recognizers.Text.Number;
using static CalendarSkill.Models.CreateEventStateModel;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Dialogs
{
    public class FindMeetingRoomDialog : CalendarSkillDialogBase
    {
        private ISearchService SearchService { get; set; }

        public FindMeetingRoomDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            IServiceManager serviceManager,
            FindContactDialog findContactDialog,
            IBotTelemetryClient telemetryClient,
            ISearchService searchService,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(FindMeetingRoomDialog), settings, services, conversationState, localeTemplateEngineManager, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;
            SearchService = searchService;

            // entry, get the name list
            var findMeetingRoom = new WaterfallStep[]
            {
                CollectStartDate,
                CollectStartTime,
                CollectDuration,
                CollectBuilding,
                CollectFloorNumber,
                GetMeetingRooms,
                GetAuthToken,
                AfterGetAuthToken,
                CheckRoomAvailable,
                CheckRoomRejected,
                AfterConfirmMeetingRoom
            };

            var updateStartDate = new WaterfallStep[]
            {
                UpdateStartDateForCreate,
                AfterUpdateStartDateForCreate,
            };

            var updateStartTime = new WaterfallStep[]
            {
                UpdateStartTimeForCreate,
                AfterUpdateStartTimeForCreate,
            };

            var updateDuration = new WaterfallStep[]
            {
                UpdateDurationForCreate,
                AfterUpdateDurationForCreate,
            };

            var collectBuilding = new WaterfallStep[]
            {
                CollectBuildingPrompt,
                AfterCollectBuildingPrompt
            };

            var collectFloorNumber = new WaterfallStep[]
            {
                CollectFloorNumberPrompt,
                AfterCollectFloorNumberPrompt
            };

            var recreatMeetingRoom = new WaterfallStep[]
            {
                RecreateMeetingRoomPrompt,
                AfterRecreateMeetingRoomPrompt
            };

            AddDialog(new WaterfallDialog(Actions.FindMeetingRoom, findMeetingRoom) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateStartDateForCreate, updateStartDate) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateStartTimeForCreate, updateStartTime) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateDurationForCreate, updateDuration) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectBuilding, collectBuilding) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectFloorNumber, collectFloorNumber) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.RecreateMeetingRoom, recreatMeetingRoom) { TelemetryClient = telemetryClient });
            AddDialog(new DatePrompt(Actions.DatePromptForCreate));
            AddDialog(new TimePrompt(Actions.TimePromptForCreate));
            AddDialog(new DurationPrompt(Actions.DurationPromptForCreate));
            AddDialog(new GetBuildingPrompt(Actions.BuildingPromptForCreate, services, searchService));
            AddDialog(new GetFloorNumberPrompt(Actions.FloorNumberPromptForCreate, services));
            AddDialog(new GetRecreateMeetingRoomInfoPrompt(Actions.RecreateMeetingRoomPrompt, services));
            AddDialog(findContactDialog ?? throw new ArgumentNullException(nameof(findContactDialog)));

            InitialDialogId = Actions.FindMeetingRoom;
        }

        private async Task<DialogTurnResult> CollectBuilding(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (string.IsNullOrEmpty(state.MeetingInfo.MeetingRoomName))
                {
                    return await sc.BeginDialogAsync(Actions.CollectBuilding, cancellationToken: cancellationToken);
                }
                else
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectBuildingPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (!string.IsNullOrEmpty(state.MeetingInfo.Building))
                {
                    List<RoomModel> meetingRooms = await SearchService.GetMeetingRoomAsync(state.MeetingInfo.Building);
                    if (meetingRooms.Any())
                    {
                        return await sc.NextAsync(result: meetingRooms, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        return await sc.PromptAsync(Actions.BuildingPromptForCreate, new CalendarPromptOptions
                        {
                            Prompt = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.NoBuilding),
                            MaxReprompt = CalendarCommonUtil.MaxRepromptCount
                        }, cancellationToken);
                    }
                }

                return await sc.PromptAsync(Actions.BuildingPromptForCreate, new CalendarPromptOptions
                {
                    Prompt = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.NoBuilding),
                    RetryPrompt = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.BuildingNonexistent),
                    MaxReprompt = CalendarCommonUtil.MaxRepromptCount
                }, cancellationToken);

            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterCollectBuildingPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var luisResult = sc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
                if (luisResult.TopIntent().intent == CalendarLuis.Intent.RejectCalendar && luisResult.TopIntent().score > 0.8)
                {
                    // '*' matches any buildings
                    state.MeetingInfo.Building = "*";
                    state.MeetingInfo.FloorNumber = null;
                }
                else if (sc.Result == null)
                {
                    var activity = TemplateEngine.GenerateActivityForLocale(CalendarSharedResponses.RetryTooManyResponse);
                    await sc.Context.SendActivityAsync(activity);
                    state.Clear();
                    await sc.CancelAllDialogsAsync();
                    return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                }
                else
                {
                    List<RoomModel> meetingRooms = sc.Result as List<RoomModel>;
                    if (state.MeetingInfo.Building == null)
                    {
                        state.MeetingInfo.Building = sc.Context.Activity.Text;
                    }

                    if (state.MeetingInfo.FloorNumber == null)
                    {
                        state.MeetingInfo.FloorNumber = meetingRooms[0].FloorNumber;
                        foreach (var room in meetingRooms)
                        {
                            if (room.FloorNumber != state.MeetingInfo.FloorNumber)
                            {
                                state.MeetingInfo.FloorNumber = null;
                                break;
                            }
                        }
                    }
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectFloorNumber(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfo.MeetingRoomName != null || state.MeetingInfo.Building == null || state.MeetingInfo.Building == "*" || state.MeetingInfo.FloorNumber != null)
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    return await sc.BeginDialogAsync(Actions.CollectFloorNumber, cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectFloorNumberPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                return await sc.PromptAsync(Actions.FloorNumberPromptForCreate, new CalendarPromptOptions
                {
                    Prompt = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.NoFloorNumber),
                    RetryPrompt = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.FloorNumberRetry),
                    MaxReprompt = CalendarCommonUtil.MaxRepromptCount
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterCollectFloorNumberPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var luisResult = sc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);

                if (luisResult.TopIntent().intent == CalendarLuis.Intent.RejectCalendar)
                {
                    state.MeetingInfo.FloorNumber = 0;
                }
                else if (sc.Result == null)
                {
                    var activity = TemplateEngine.GenerateActivityForLocale(CalendarSharedResponses.RetryTooManyResponse);
                    await sc.Context.SendActivityAsync(activity);
                    state.Clear();
                    await sc.CancelAllDialogsAsync();
                    return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                }
                else
                {
                    state.MeetingInfo.FloorNumber = sc.Result as int?;
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // Get the rooms with given conditions.
        private async Task<DialogTurnResult> GetMeetingRooms(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                state.MeetingInfo.UnconfirmedMeetingRoom = !string.IsNullOrEmpty(state.MeetingInfo.MeetingRoomName) ?
                        await SearchService.GetMeetingRoomAsync(state.MeetingInfo.MeetingRoomName) :
                        await SearchService.GetMeetingRoomAsync(state.MeetingInfo.Building, state.MeetingInfo.FloorNumber.GetValueOrDefault());

                if (state.MeetingInfo.UnconfirmedMeetingRoom.Count == 0)
                {
                    if (!string.IsNullOrEmpty(state.MeetingInfo.MeetingRoomName))
                    {
                        var tokens = new { MeetingRoom = state.MeetingInfo.MeetingRoomName };
                        var activity = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.MeetingRoomNotFoundByName, tokens);
                        await sc.Context.SendActivityAsync(activity);
                        state.MeetingInfo.MeetingRoomName = null;
                    }
                    else
                    {
                        var tokens = new
                        {
                            state.MeetingInfo.Building,
                            state.MeetingInfo.FloorNumber,
                            DateTime = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime((DateTime)state.MeetingInfo.StartDateTime, state.GetUserTimeZone()), state.MeetingInfo.AllDay == true, DateTime.UtcNow > state.MeetingInfo.StartDateTime),
                        };
                        var activity = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.MeetingRoomNotFoundByBuildingAndFloor, tokens);
                        await sc.Context.SendActivityAsync(activity);
                        if (state.MeetingInfo.FloorNumber.GetValueOrDefault() == 0)
                        {
                            state.MeetingInfo.Building = null;
                        }

                        state.MeetingInfo.FloorNumber = null;
                    }

                    return await sc.ReplaceDialogAsync(Actions.RecreateMeetingRoom, cancellationToken: cancellationToken);
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // Check whether the candidate rooms are free.
        private async Task<DialogTurnResult> CheckRoomAvailable(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
                var service = ServiceManager.InitCalendarService(token as string, state.EventSource);

                List<string> users = new List<string>();
                foreach (var room in state.MeetingInfo.UnconfirmedMeetingRoom)
                {
                    users.Add(room.EmailAddress);
                }

                // roomAvailablility indicates whether the room is free.
                List<bool> roomAvailablity = await service.CheckAvailable(users, (DateTime)state.MeetingInfo.StartDateTime, state.MeetingInfo.Duration / 60);
                List<RoomModel> meetingRooms = new List<RoomModel>();
                for (int i = 0; i < state.MeetingInfo.UnconfirmedMeetingRoom.Count(); i++)
                {
                    var status = roomAvailablity[i];
                    if (status == true)
                    {
                        meetingRooms.Add(state.MeetingInfo.UnconfirmedMeetingRoom[i]);
                    }
                }

                state.MeetingInfo.UnconfirmedMeetingRoom = meetingRooms;

                if (meetingRooms.Count > 0)
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else if (string.IsNullOrEmpty(state.MeetingInfo.MeetingRoomName))
                {
                    var tokens = new
                    {
                        state.MeetingInfo.Building,
                        state.MeetingInfo.FloorNumber,
                        DateTime = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime((DateTime)state.MeetingInfo.StartDateTime, state.GetUserTimeZone()), 
                        state.MeetingInfo.AllDay == true, DateTime.UtcNow > state.MeetingInfo.StartDateTime),
                    };
                    var activity = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.MeetingRoomNotFoundByBuildingAndFloor, tokens);
                    await sc.Context.SendActivityAsync(activity);
                }
                else
                {
                    var tokens = new { MeetingRoom = state.MeetingInfo.MeetingRoomName };
                    var activity = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.MeetingRoomUnavailable, tokens);
                    await sc.Context.SendActivityAsync(activity);
                }

                return await sc.ReplaceDialogAsync(Actions.RecreateMeetingRoom, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // If the room has been rejected, it needs to be filterd.
        private async Task<DialogTurnResult> CheckRoomRejected(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.MeetingInfo.MeetingRoomName))
                {
                    state.MeetingInfo.UnconfirmedMeetingRoom = state.MeetingInfo.UnconfirmedMeetingRoom.FindAll(x => !state.MeetingInfo.IgnoredMeetingRoom.Contains(x.DisplayName + state.MeetingInfo.StartDateTime.ToString()));
                }

                if (state.MeetingInfo.UnconfirmedMeetingRoom.Count == 0)
                {
                    var tokens = new
                    {
                        state.MeetingInfo.Building,
                        state.MeetingInfo.FloorNumber,
                        DateTime = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime((DateTime)state.MeetingInfo.StartDateTime, state.GetUserTimeZone()), state.MeetingInfo.AllDay == true, DateTime.UtcNow > state.MeetingInfo.StartDateTime),
                    };
                    var activity = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.CannotFindOtherMeetingRoom, tokens);
                    await sc.Context.SendActivityAsync(activity);
                    return await sc.ReplaceDialogAsync(Actions.RecreateMeetingRoom, cancellationToken: cancellationToken);
                }
                else
                {
                    var tokens = new
                    {
                        MeetingRoom = state.MeetingInfo.UnconfirmedMeetingRoom.First().DisplayName,
                        DateTime = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime((DateTime)state.MeetingInfo.StartDateTime, state.GetUserTimeZone()), state.MeetingInfo.AllDay == true, DateTime.UtcNow > state.MeetingInfo.StartDateTime)
                    };

                    // find an available room, continue
                    var activity = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.ConfirmMeetingRoomPrompt, tokens);
                    return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = activity }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterConfirmMeetingRoom(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    state.MeetingInfo.MeetingRoom = state.MeetingInfo.UnconfirmedMeetingRoom.First();
                    return await sc.EndDialogAsync();
                }
                else
                {
                    state.MeetingInfo.IgnoredMeetingRoom.Add(state.MeetingInfo.UnconfirmedMeetingRoom.First().DisplayName + state.MeetingInfo.StartDateTime.ToString());
                    var activity = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.IgnoreMeetingRoom);
                    await sc.Context.SendActivityAsync(activity);
                    return await sc.ReplaceDialogAsync(Actions.RecreateMeetingRoom, cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> RecreateMeetingRoomPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.RecreateMeetingRoomPrompt, new CalendarPromptOptions
                {
                    Prompt = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.RecreateMeetingRoom),
                    RetryPrompt = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.RecreateMeetingRoomAgain),
                    MaxReprompt = CalendarCommonUtil.MaxRepromptCount
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterRecreateMeetingRoomPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var luisResult = sc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
                if (sc.Result != null)
                {
                    var recreateState = sc.Result as RecreateMeetingRoomState?;
                    switch (recreateState.Value)
                    {
                        case RecreateMeetingRoomState.Cancel:
                            {
                                var activity = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.CancelRequest);
                                await sc.Context.SendActivityAsync(activity);
                                state.Clear();
                                await sc.CancelAllDialogsAsync();
                                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                            }

                        case RecreateMeetingRoomState.ChangeMeetingRoom:
                            {
                                state.MeetingInfo.MeetingRoomName = null;

                                // No rooms in current condition.
                                if (state.MeetingInfo.UnconfirmedMeetingRoom.Count == 0)
                                {
                                    state.MeetingInfo.FloorNumber = null;

                                    // Keep building if use just change floorNumber, or clear current conditions.
                                    if (luisResult.Entities.FloorNumber == null)
                                    {
                                        state.MeetingInfo.Building = null;
                                    }
                                }

                                if (luisResult.Entities.Building != null)
                                {
                                    state.MeetingInfo.Building = GetBuildingFromEntity(luisResult.Entities);
                                    state.MeetingInfo.FloorNumber = null;
                                }

                                if (luisResult.Entities.FloorNumber != null)
                                {
                                    string utterance = luisResult.Entities.FloorNumber[0];
                                    string culture = sc.Context.Activity.Locale ?? English;
                                    state.MeetingInfo.FloorNumber = ParseFloorNumber(utterance, culture);
                                }

                                if (luisResult.Entities.MeetingRoom != null)
                                {
                                    state.MeetingInfo.MeetingRoomName = GetMeetingRoomFromEntity(luisResult.Entities);
                                }

                                return await sc.ReplaceDialogAsync(Actions.FindMeetingRoom, options: sc.Options, cancellationToken: cancellationToken);
                            }

                        case RecreateMeetingRoomState.ChangeTime:
                            {
                                state.MeetingInfo.StartTime.Clear();
                                state.MeetingInfo.EndDate.Clear();
                                state.MeetingInfo.EndTime.Clear();
                                state.MeetingInfo.StartDateTime = null;
                                state.MeetingInfo.EndDateTime = null;

                                if (luisResult.Entities.ToDate != null)
                                {
                                    var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, luisResult.Entities._instance.ToDate[0]);
                                    var date = GetDateFromDateTimeString(dateString, sc.Context.Activity.Locale, state.GetUserTimeZone(), false, false);
                                    if (date != null)
                                    {
                                        state.MeetingInfo.StartDate = date;
                                        state.MeetingInfo.StartDateString = dateString;
                                    }
                                }

                                if (luisResult.Entities.ToTime != null)
                                {
                                    var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, luisResult.Entities._instance.ToTime[0]);
                                    var time = GetTimeFromDateTimeString(timeString, sc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                    if (time != null)
                                    {
                                        state.MeetingInfo.StartTime = time;
                                    }
                                }

                                // If not given any specific time, all the time slot will be cleared and recollected from user.
                                if (luisResult.Entities.ToDate == null && luisResult.Entities.ToTime == null)
                                {
                                    state.MeetingInfo.StartDate.Clear();
                                    state.MeetingInfo.Duration = 0;
                                }

                                return await sc.ReplaceDialogAsync(Actions.FindMeetingRoom, options: sc.Options, cancellationToken: cancellationToken);
                            }

                        default:
                            {
                                // should not go to this part. place an error handling for save.
                                await HandleDialogExceptions(sc, new Exception("Get unexpect state in recreate."));
                                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                            }
                    }
                }
                else
                {
                    // user has tried too many times but can't get result
                    var activity = TemplateEngine.GenerateActivityForLocale(CalendarSharedResponses.RetryTooManyResponse);
                    await sc.Context.SendActivityAsync(activity);
                    state.Clear();
                    await sc.CancelAllDialogsAsync();
                    return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private int? ParseFloorNumber(string utterance, string culture)
        {
            var model_ordinal = new NumberRecognizer(culture).GetOrdinalModel(culture);
            var result = model_ordinal.Parse(utterance);
            if (result.Any())
            {
                return int.Parse(result.First().Resolution["value"].ToString());
            }
            else
            {
                var model_number = new NumberRecognizer(culture).GetNumberModel(culture);
                result = model_number.Parse(utterance);
                if (result.Any())
                {
                    return int.Parse(result.First().Resolution["value"].ToString());
                }
            }

            return null;
        }
    }
}