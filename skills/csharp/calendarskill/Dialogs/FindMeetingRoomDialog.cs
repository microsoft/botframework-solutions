using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Prompts;
using CalendarSkill.Responses.FindMeetingRoom;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkill.Services.AzureSearchAPI;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Azure.Search;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Graph;
using Microsoft.Recognizers.Text.Number;
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
                GetAuthToken,
                AfterGetAuthToken,
                FindAvailableMeetingRoom,
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
            AddDialog(new GetRecreateInfoPrompt(Actions.GetRecreateInfoPrompt));
            AddDialog(findContactDialog ?? throw new ArgumentNullException(nameof(findContactDialog)));

            InitialDialogId = Actions.FindMeetingRoom;
        }

        private async Task<DialogTurnResult> CollectBuilding(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (string.IsNullOrEmpty(state.MeetingInfor.MeetingRoomName) && string.IsNullOrEmpty(state.MeetingInfor.Building))
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

                var options = sc.Options as CollectInfoDialogOptions;
                var activity = (options != null && options.Reason == CollectInfoDialogOptions.UpdateReason.ReCollect) ?
                    TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.BuildingNonexistent) :
                    TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.NoBuilding);

                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = activity }, cancellationToken);
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
                    state.MeetingInfor.Building = "*";
                }
                else if (state.MeetingInfor.FloorNumber == null)
                {
                    List<RoomModel> meetingRooms = null;
                    if (sc.Result != null)
                    {
                        state.MeetingInfor.Building = sc.Result.ToString();
                        state.MeetingInfor.FloorNumber = null;
                    }

                    meetingRooms = await SearchService.GetMeetingRoomAsync(state.MeetingInfor.Building, state.MeetingInfor.FloorNumber.GetValueOrDefault());
                    if (meetingRooms.Count() == 0)
                    {
                        state.MeetingInfor.Building = null;
                        return await sc.ReplaceDialogAsync(Actions.CollectBuilding, options: new CollectInfoDialogOptions(CollectInfoDialogOptions.UpdateReason.ReCollect), cancellationToken: cancellationToken);
                    }
                    else
                    {
                        if (state.MeetingInfor.FloorNumber == null)
                        {
                            state.MeetingInfor.FloorNumber = meetingRooms[0].FloorNumber;
                            foreach (var room in meetingRooms)
                            {
                                if (room.FloorNumber != state.MeetingInfor.FloorNumber)
                                {
                                    state.MeetingInfor.FloorNumber = null;
                                    break;
                                }
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
                if (state.MeetingInfor.MeetingRoomName == null && state.MeetingInfor.Building != null && state.MeetingInfor.Building != "*" && state.MeetingInfor.FloorNumber == null)
                {
                    return await sc.BeginDialogAsync(Actions.CollectFloorNumber, cancellationToken: cancellationToken);
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

        private async Task<DialogTurnResult> CollectFloorNumberPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = sc.Options as CollectInfoDialogOptions;
                var activity = (options != null && options.Reason == CollectInfoDialogOptions.UpdateReason.ReCollect) ?
                    TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.FloorNumberRetry) :
                    TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.NoFloorNumber);

                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = activity }, cancellationToken);
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
                if (sc.Result == null)
                {
                    return await sc.EndDialogAsync();
                }

                if (luisResult.TopIntent().intent == CalendarLuis.Intent.RejectCalendar)
                {
                    state.MeetingInfor.FloorNumber = 0;
                }
                else
                {
                    string utterance = sc.Result.ToString();
                    string culture = sc.Context.Activity.Locale ?? English;
                    var model_ordinal = new NumberRecognizer(culture).GetOrdinalModel(culture);
                    var result = model_ordinal.Parse(utterance);
                    if (result.Any())
                    {
                        state.MeetingInfor.FloorNumber = int.Parse(result.First().Resolution["value"].ToString());
                    }
                    else
                    {
                        var model_number = new NumberRecognizer(culture).GetNumberModel(culture);
                        result = model_number.Parse(utterance);
                        if (result.Any())
                        {
                            state.MeetingInfor.FloorNumber = int.Parse(result.First().Resolution["value"].ToString());
                        }
                    }

                    if (state.MeetingInfor.FloorNumber == null)
                    {
                        return await sc.ReplaceDialogAsync(Actions.CollectFloorNumber, options: new CollectInfoDialogOptions(CollectInfoDialogOptions.UpdateReason.ReCollect), cancellationToken: cancellationToken);
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

        private async Task<DialogTurnResult> FindAvailableMeetingRoom(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
                var service = ServiceManager.InitCalendarService(token as string, state.EventSource);

                List<RoomModel> meetingRooms = !string.IsNullOrEmpty(state.MeetingInfor.MeetingRoomName) ?
                        await SearchService.GetMeetingRoomAsync(state.MeetingInfor.MeetingRoomName) :
                        await SearchService.GetMeetingRoomAsync(state.MeetingInfor.Building, state.MeetingInfor.FloorNumber.GetValueOrDefault());
                List<bool> availablity = null;
                if (meetingRooms.Any())
                {
                    List<string> users = new List<string>();
                    foreach (var room in meetingRooms)
                    {
                        users.Add(room.EmailAddress);
                    }

                    availablity = await service.CheckAvailable(users, (DateTime)state.MeetingInfor.StartDateTime, state.MeetingInfor.Duration / 60);

                    for (int i = 0; i < availablity.Count(); i++)
                    {
                        var status = availablity[i];
                        if (status == true && (!state.MeetingInfor.IgnoredMeetingRoom.Contains(meetingRooms[i].DisplayName + state.MeetingInfor.StartDateTime.ToString()) || !string.IsNullOrEmpty(state.MeetingInfor.MeetingRoomName)))
                        {
                            state.MeetingInfor.MeetingRoom = meetingRooms[i];
                            var tokens = new
                            {
                                MeetingRoom = state.MeetingInfor.MeetingRoom.DisplayName,
                                DateTime = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime((DateTime)state.MeetingInfor.StartDateTime, state.GetUserTimeZone()), state.MeetingInfor.Allday == true, DateTime.UtcNow > state.MeetingInfor.StartDateTime)
                            };

                            // find an available room, continue
                            var activity = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.ConfirmMeetingRoomPrompt, tokens);
                            return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = activity }, cancellationToken);
                        }
                    }
                }

                if (meetingRooms.Count() == 0 && !string.IsNullOrEmpty(state.MeetingInfor.MeetingRoomName))
                {
                    // can't fint room for given room name
                    var tokens = new { MeetingRoom = state.MeetingInfor.MeetingRoomName };
                    var activity = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.MeetingRoomNotFound, tokens);
                    await sc.Context.SendActivityAsync(activity);
                }
                else
                {
                    var tokens = new
                    {
                        InBulding = state.MeetingInfor.Building != null && state.MeetingInfor.Building != "*" ? " in " + state.MeetingInfor.Building : null,
                        OnFloorNumber = state.MeetingInfor.FloorNumber.GetValueOrDefault() != 0 ? " on floor " + state.MeetingInfor.FloorNumber.ToString() : null,
                        DateTime = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime((DateTime)state.MeetingInfor.StartDateTime, state.GetUserTimeZone()), state.MeetingInfor.Allday == true, DateTime.UtcNow > state.MeetingInfor.StartDateTime),
                    };

                    // room in ignored list or can't be found/not available
                    var activity = meetingRooms.Any() && availablity.Contains(true) ?
                        TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.CannotFindOtherMeetingRoom, tokens) :
                        TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.CannotFindMeetingRoom, tokens);
                    await sc.Context.SendActivityAsync(activity);
                }

                state.MeetingInfor.MeetingRoom = null;
                return await sc.ReplaceDialogAsync(Actions.RecreateMeetingRoom, cancellationToken: cancellationToken);
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
                    return await sc.EndDialogAsync();
                }
                else
                {
                    state.MeetingInfor.IgnoredMeetingRoom.Add(state.MeetingInfor.MeetingRoom.DisplayName + state.MeetingInfor.StartDateTime.ToString());
                    state.MeetingInfor.MeetingRoom = null;
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
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = sc.Options as CollectInfoDialogOptions;
                if (options != null && options.Reason == CollectInfoDialogOptions.UpdateReason.ReCollect)
                {
                    var activity = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.RecreateMeetingRoomAgain);
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = activity }, cancellationToken);
                }
                else
                {
                    var activity = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.RecreateMeetingRoom);
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = activity }, cancellationToken);
                }
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
                if (luisResult.Entities.datetime != null)
                {
                    var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, luisResult.Entities._instance.datetime[0]);
                    var date = GetDateFromDateTimeString(dateString, sc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                    if (date != null)
                    {
                        state.MeetingInfor.StartDate = date;
                        state.MeetingInfor.StartDateString = dateString;
                    }

                    var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, luisResult.Entities._instance.datetime[0]);
                    var time = GetTimeFromDateTimeString(timeString, sc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                    if (time != null)
                    {
                        state.MeetingInfor.StartTime = time;
                    }

                    state.MeetingInfor.EndDate.Clear();
                    state.MeetingInfor.EndTime.Clear();
                    state.MeetingInfor.StartDateTime = null;
                    state.MeetingInfor.EndDateTime = null;
                    return await sc.ReplaceDialogAsync(Actions.FindMeetingRoom, options: sc.Options, cancellationToken: cancellationToken);
                }
                else if (luisResult.TopIntent().intent == CalendarLuis.Intent.RejectCalendar)
                {
                    var activity = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.ConfirmedMeetingRoom);
                    await sc.Context.SendActivityAsync(activity);
                    state.Clear();
                    return await sc.EndDialogAsync();
                }
                else if (luisResult.Entities.FloorNumber != null)
                {
                    string utterance = luisResult.Entities.FloorNumber[0];
                    string culture = sc.Context.Activity.Locale ?? English;
                    var model_ordinal = new NumberRecognizer(culture).GetOrdinalModel(culture);
                    var result = model_ordinal.Parse(utterance);
                    if (result.Any())
                    {
                        state.MeetingInfor.FloorNumber = int.Parse(result.First().Resolution["value"].ToString());
                    }
                    else
                    {
                        var model_number = new NumberRecognizer(culture).GetNumberModel(culture);
                        result = model_number.Parse(utterance);
                        if (result.Any())
                        {
                            state.MeetingInfor.FloorNumber = int.Parse(result.First().Resolution["value"].ToString());
                        }
                    }

                    state.MeetingInfor.MeetingRoomName = null;
                    return await sc.ReplaceDialogAsync(Actions.FindMeetingRoom, options: sc.Options, cancellationToken: cancellationToken);
                }
                else if (luisResult.Entities.MeetingRoom != null || luisResult.Entities.MeetingRoomName != null)
                {
                    state.MeetingInfor.MeetingRoomName = GetMeetingRoomFromEntity(luisResult.Entities);
                    return await sc.ReplaceDialogAsync(Actions.FindMeetingRoom, options: sc.Options, cancellationToken: cancellationToken);
                }
                else if (luisResult.Entities.Building != null || luisResult.Entities.BuildingName != null)
                {
                    state.MeetingInfor.Building = GetBuildingFromEntity(luisResult.Entities);
                    state.MeetingInfor.FloorNumber = null;
                    state.MeetingInfor.MeetingRoomName = null;
                    return await sc.ReplaceDialogAsync(Actions.FindMeetingRoom, options: sc.Options, cancellationToken: cancellationToken);
                }
                else
                {
                    var regex = new Regex(CalendarCommonStrings.AdjustTime);
                    if (regex.IsMatch(luisResult.Text.ToLower()))
                    {
                        state.MeetingInfor.StartDate.Clear();
                        state.MeetingInfor.StartTime.Clear();
                        state.MeetingInfor.EndDate.Clear();
                        state.MeetingInfor.EndTime.Clear();
                        state.MeetingInfor.Duration = 0;
                        state.MeetingInfor.EndDateTime = null;
                        return await sc.ReplaceDialogAsync(Actions.FindMeetingRoom, options: sc.Options, cancellationToken: cancellationToken);
                    }

                    regex = new Regex(CalendarCommonStrings.AdjustMeetingRoom);
                    if (regex.IsMatch(luisResult.Text.ToLower()))
                    {
                        state.MeetingInfor.MeetingRoomName = null;
                        return await sc.ReplaceDialogAsync(Actions.FindMeetingRoom, options: sc.Options, cancellationToken: cancellationToken);
                    }
                }

                var options = new CollectInfoDialogOptions(CollectInfoDialogOptions.UpdateReason.ReCollect);
                return await sc.ReplaceDialogAsync(Actions.RecreateMeetingRoom, options: options, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}