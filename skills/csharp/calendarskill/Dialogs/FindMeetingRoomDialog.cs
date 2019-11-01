using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Responses.CreateEvent;
using CalendarSkill.Responses.FindMeetingRoom;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Options;
using CalendarSkill.Prompts;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.CreateEvent;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Google.Apis.Calendar.v3.Data;
using Luis;
using Microsoft.Azure.Search;
using Microsoft.Azure.Cosmos;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Graph;
using static CalendarSkill.Models.CalendarSkillState;
using static CalendarSkill.Models.CreateEventStateModel;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Number;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Dialogs
{
    public class FindMeetingRoomDialog : CalendarSkillDialogBase
    {
        private AzureSearchService _azureSearchService { get; set; }

        public FindMeetingRoomDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IServiceManager serviceManager,
            FindContactDialog findContactDialog,
            IBotTelemetryClient telemetryClient,
            AzureSearchService azureSearchService,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(FindMeetingRoomDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;
            _azureSearchService = azureSearchService;

            // entry, get the name list
            var bookMeetingRoom = new WaterfallStep[]
            {
                //GetAuthToken,
                //AfterGetAuthToken,
                GetStartDateTime,
                CollectStartDate,
                CollectStartTime,
                CollectDuration,
                RouteToSearch,
            };

            var findMeetingRoom = new WaterfallStep[]
            {
                CollectBuilding,
                CollectFloorNumber,
                FindAnAvailableMeetingRoom,
                AfterConfirmMeetingRoom
            };

            var checkAvailability = new WaterfallStep[]
            {
                CollectMeetingRoom,
                CheckMeetingRoomAvailable,
                AfterConfirmMeetingRoom
            };

            var bookConfirmedMeetingRoom = new WaterfallStep[]
            {
                AfterFindMeetingRoom,
                //CollectTitle,
                //CollectAttendees,
                //ConfirmBeforeCreatePrompt,
                //AfterConfirmBeforeCreatePrompt,
                //BookMeetingRoom
            };

            var recreatMeetingRoom = new WaterfallStep[]
            {
                RecreateMeetingRoomPrompt,
                AfterRecreateMeetingRoomPrompt
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

            var collectMeetingRoom = new WaterfallStep[]
            {
                CollectMeetingRoomPrompt,
                AfterCollectMeetingRoomPrompt
            };

            AddDialog(new WaterfallDialog(Actions.BookMeetingRoom, bookMeetingRoom) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindMeetingRoom, findMeetingRoom) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CheckAvailability, checkAvailability) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.BookConfirmedMeetingRoom, bookConfirmedMeetingRoom) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateStartDateForCreate, updateStartDate) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateStartTimeForCreate, updateStartTime) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateDurationForCreate, updateDuration) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectBuilding, collectBuilding) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectFloorNumber, collectFloorNumber) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectMeetingRoom, collectMeetingRoom) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.RecreateMeetingRoom, recreatMeetingRoom) { TelemetryClient = telemetryClient });
            AddDialog(new DatePrompt(Actions.DatePromptForCreate));
            AddDialog(new TimePrompt(Actions.TimePromptForCreate));
            AddDialog(new DurationPrompt(Actions.DurationPromptForCreate));
            AddDialog(new GetRecreateInfoPrompt(Actions.GetRecreateInfoPrompt));
            AddDialog(findContactDialog ?? throw new ArgumentNullException(nameof(findContactDialog)));

            InitialDialogId = Actions.BookMeetingRoom;
        }

        private async Task<DialogTurnResult> RouteToSearch(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.MeetingInfor.MeetingRoomName == null)
                {
                    return await sc.ReplaceDialogAsync(Actions.FindMeetingRoom, sc.Options, cancellationToken);
                }
                else
                {
                    return await sc.ReplaceDialogAsync(Actions.CheckAvailability, sc.Options, cancellationToken);
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

        private async Task<DialogTurnResult> GetStartDateTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                DateTime dateNow = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone());

                DateTime endOfToday = new DateTime(dateNow.Year, dateNow.Month, dateNow.Day, 23, 59, 59);

                if (state.MeetingInfor.StartDateTime != null)
                {
                    state.MeetingInfor.StartDateTime = null;

                }
                else
                {
                    if (state.MeetingInfor.StartDate.Any() && state.MeetingInfor.EndDate.Any() && state.MeetingInfor.StartDate.First() == state.MeetingInfor.EndDate.First())
                    {
                        state.MeetingInfor.EndDate.Clear();
                    }

                    if (state.MeetingInfor.StartDate.Count() == 0)
                    {
                        state.MeetingInfor.StartDate.Add(dateNow);

                        if (state.MeetingInfor.StartTime.Count() == 0)
                        {
                            if (endOfToday < state.MeetingInfor.StartDate.First())
                            {
                                state.MeetingInfor.StartTime.Add(endOfToday.AddMinutes(1));
                            }
                            else
                            {
                                state.MeetingInfor.StartTime.Add(dateNow);
                            }
                        }
                    }
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectMeetingRoom(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                //ask for user state
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                return await sc.BeginDialogAsync(Actions.CollectMeetingRoom, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectMeetingRoomPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (string.IsNullOrEmpty(state.MeetingInfor.MeetingRoomName))
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(FindMeetingRoomResponses.NoMeetingRoom) }, cancellationToken);
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

        private async Task<DialogTurnResult> AfterCollectMeetingRoomPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var token = state.APIToken;

                if (sc.Result != null)
                {
                    state.MeetingInfor.MeetingRoomName = sc.Result.ToString().Replace("_", " ");
                }

                List<PlaceModel> meetingRooms = await _azureSearchService.GetMeetingRoomAsync(state.MeetingInfor.MeetingRoomName);

                if (meetingRooms.Count == 0)
                {
                    var data = new StringDictionary() { { "MeetingRoom", state.MeetingInfor.MeetingRoomName } };
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindMeetingRoomResponses.MeetingRoomNotFound, data));
                    state.MeetingInfor.MeetingRoomName = null;
                    return await sc.ReplaceDialogAsync(Actions.CollectMeetingRoom, sc.Options, cancellationToken);
                }
                else if (meetingRooms.Count == 1)
                {
                    state.MeetingInfor.MeetingRoom = meetingRooms[0];
                    return await sc.EndDialogAsync();
                }
                else
                {
                    state.MeetingInfor.MeetingRoom = meetingRooms[0];
                    return await sc.EndDialogAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CheckMeetingRoomAvailable(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var token = state.APIToken;
                var service = ServiceManager.InitCalendarService(token, state.EventSource);

                List<string> users = new List<string>();
                users.Add(state.MeetingInfor.MeetingRoom.EmailAddress);

                List<bool> availablity = await service.CheckAvailable(users, (DateTime)state.MeetingInfor.StartDateTime, state.MeetingInfor.Duration / 60);
                if (availablity[0])
                {
                    StringDictionary tokens = new StringDictionary
                        {
                            { "MeetingRoom", state.MeetingInfor.MeetingRoom.DisplayName },
                            { "DateTime", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime((DateTime)state.MeetingInfor.StartDateTime, state.GetUserTimeZone()), state.MeetingInfor.Allday == true, DateTime.UtcNow > state.MeetingInfor.StartDateTime) },
                        };
                    return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                    {
                        Prompt = ResponseManager.GetResponse(FindMeetingRoomResponses.ConfirmMeetingRoomPrompt, tokens),
                    }, cancellationToken);
                }
                else
                {
                    var data = new StringDictionary()
                    {
                        { "MeetingRoom", state.MeetingInfor.MeetingRoom.DisplayName },
                        { "DateTime", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime((DateTime)state.MeetingInfor.StartDateTime, state.GetUserTimeZone()), state.MeetingInfor.Allday == true, DateTime.UtcNow > state.MeetingInfor.StartDateTime) },
                    };
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindMeetingRoomResponses.MeetingRoomUnavailable, data));
                    return await sc.ReplaceDialogAsync(Actions.RecreateMeetingRoom, cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectBuilding(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                //ask for user state
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                return await sc.BeginDialogAsync(Actions.CollectBuilding, cancellationToken);

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

                if (string.IsNullOrEmpty(state.MeetingInfor.Building))
                {
                    var options = sc.Options as CollectInfoDialogOptions;
                    if (options != null && options.Reason == CollectInfoDialogOptions.UpdateReason.ReCollect)
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(FindMeetingRoomResponses.BuildingNonexistent) }, cancellationToken);
                    }
                    else
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(FindMeetingRoomResponses.NoBuilding) }, cancellationToken);
                    }
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

        private async Task<DialogTurnResult> AfterCollectBuildingPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if ((state.LuisResult.TopIntent().intent == CalendarLuis.Intent.Reject && state.LuisResult.TopIntent().score > 0.8) 
                    || (sc.Result != null && sc.Result.ToString().ToLower().Contains(CalendarCommonStrings.Any)))
                {
                    state.MeetingInfor.Building = "*";
                }
                else if (state.MeetingInfor.FloorNumber == null)
                {
                    AzureSearchService azureSearchService = new AzureSearchService(Settings);
                    List<PlaceModel> meetingRooms = null;
                    if (sc.Result != null)
                    {
                        state.MeetingInfor.Building = sc.Result.ToString();
                        state.MeetingInfor.FloorNumber = null;
                    }

                    if (state.MeetingInfor.FloorNumber.GetValueOrDefault() == 0)
                    {
                        meetingRooms = await azureSearchService.GetMeetingRoomAsync(state.MeetingInfor.Building);
                    }
                    else
                    {
                        meetingRooms = await azureSearchService.GetMeetingRoomByBuildingAndFloorNumberAsync(state.MeetingInfor.Building, state.MeetingInfor.FloorNumber.Value);
                    }

                    if (meetingRooms.Count() == 0)
                    {
                        state.MeetingInfor.Building = null;
                        return await sc.ReplaceDialogAsync(Actions.CollectBuilding, options: new CollectInfoDialogOptions(CollectInfoDialogOptions.UpdateReason.ReCollect), cancellationToken: cancellationToken);
                    }
                    else if (meetingRooms.Count() == 1)
                    {
                        state.MeetingInfor.MeetingRoomName = state.MeetingInfor.Building;
                        state.MeetingInfor.Building = null;
                        state.MeetingInfor.FloorNumber = null;
                        return await sc.ReplaceDialogAsync(Actions.CheckAvailability, sc.Options, cancellationToken);
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
                //ask for user state
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                return await sc.BeginDialogAsync(Actions.CollectFloorNumber, cancellationToken);
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

                if (state.MeetingInfor.Building != null && state.MeetingInfor.Building != "*" && state.MeetingInfor.FloorNumber == null)
                {
                    var options = sc.Options as CollectInfoDialogOptions;
                    if (options != null && options.Reason == CollectInfoDialogOptions.UpdateReason.ReCollect)
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(FindMeetingRoomResponses.FloorNumberRetry) }, cancellationToken);
                    }
                    else
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(FindMeetingRoomResponses.NoFloorNumber) }, cancellationToken);
                    }
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

        private async Task<DialogTurnResult> AfterCollectFloorNumberPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (sc.Result == null)
                {
                    return await sc.EndDialogAsync();
                }

                if (state.LuisResult.TopIntent().intent == CalendarLuis.Intent.Reject)
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

        private async Task<DialogTurnResult> FindAnAvailableMeetingRoom(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var token = state.APIToken;
                var service = ServiceManager.InitCalendarService(token, state.EventSource);

                AzureSearchService azureSearchService = new AzureSearchService(Settings);

                List<PlaceModel> meetingRooms = null;
                if (state.MeetingInfor.FloorNumber.GetValueOrDefault() != 0)
                {
                    meetingRooms = await azureSearchService.GetMeetingRoomByBuildingAndFloorNumberAsync(state.MeetingInfor.Building, (int)state.MeetingInfor.FloorNumber);
                }
                else
                {
                    meetingRooms = await azureSearchService.GetMeetingRoomAsync(state.MeetingInfor.Building);
                }

                bool hasRoom = false;
                if (meetingRooms.Count() > 0)
                {
                    List<string> users = new List<string>();
                    foreach (var room in meetingRooms)
                    {
                        users.Add(room.EmailAddress);
                    }

                    List<bool> availablity = await service.CheckAvailable(users, (DateTime)state.MeetingInfor.StartDateTime, state.MeetingInfor.Duration / 60);
                    for (int i = 0; i < availablity.Count(); i++)
                    {
                        var status = availablity[i];
                        if (status == true && !state.MeetingInfor.IgnoredMeetingRoom.Contains(meetingRooms[i].DisplayName + state.MeetingInfor.StartDateTime.ToString()))
                        {
                            state.MeetingInfor.MeetingRoom = meetingRooms[i];
                            StringDictionary tokens = new StringDictionary
                            {
                                { "MeetingRoom", state.MeetingInfor.MeetingRoom.DisplayName },
                                { "DateTime", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime((DateTime)state.MeetingInfor.StartDateTime, state.GetUserTimeZone()), state.MeetingInfor.Allday == true, DateTime.UtcNow > state.MeetingInfor.StartDateTime) },
                                //{ "DateTime", TimeConverter.ConvertUtcToUserTime((DateTime)state.MeetingInfor.StartDateTime, state.GetUserTimeZone()).ToString("h:mm tt") }
                            };
                            return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                            {
                                Prompt = ResponseManager.GetResponse(FindMeetingRoomResponses.ConfirmMeetingRoomPrompt, tokens),
                            }, cancellationToken);
                        }

                        if (status == true)
                        {
                            hasRoom = true;
                        }
                    }
                }

                StringDictionary data = new StringDictionary
                {
                    { "InBulding", state.MeetingInfor.Building != null && state.MeetingInfor.Building != "*" ? " in " + state.MeetingInfor.Building : null },
                    { "OnFloorNumber", state.MeetingInfor.FloorNumber.GetValueOrDefault() != 0 ? " on floor " + state.MeetingInfor.FloorNumber.ToString() : null },
                    { "DateTime", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime((DateTime)state.MeetingInfor.StartDateTime, state.GetUserTimeZone()), state.MeetingInfor.Allday == true, DateTime.UtcNow > state.MeetingInfor.StartDateTime) },
                };
                if (hasRoom)
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindMeetingRoomResponses.CannotFindOtherMeetingRoom, data));
                }
                else
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindMeetingRoomResponses.CannotFindMeetingRoom, data));
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
                    return await sc.ReplaceDialogAsync(Actions.BookConfirmedMeetingRoom, options: sc.Options, cancellationToken: cancellationToken);
                }
                else
                {
                    state.MeetingInfor.IgnoredMeetingRoom.Add(state.MeetingInfor.MeetingRoom.DisplayName + state.MeetingInfor.StartDateTime.ToString());
                    state.MeetingInfor.MeetingRoom = null;
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindMeetingRoomResponses.IgnoreMeetingRoom));
                    return await sc.ReplaceDialogAsync(Actions.RecreateMeetingRoom, cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> RecreateMeetingRoomPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = sc.Options as CollectInfoDialogOptions;
                if (options != null && options.Reason == CollectInfoDialogOptions.UpdateReason.ReCollect)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(FindMeetingRoomResponses.RecreateMeetingRoomAgain) }, cancellationToken);
                }
                else
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(FindMeetingRoomResponses.RecreateMeetingRoom) }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterRecreateMeetingRoomPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.LuisResult.Entities.datetime != null)
                {
                    var dateString = GetDateTimeStringFromInstanceData(state.LuisResult.Text, state.LuisResult.Entities._instance.datetime[0]);
                    var date = GetDateFromDateTimeString(dateString, sc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                    if (date != null)
                    {
                        state.MeetingInfor.StartDate = date;
                        state.MeetingInfor.StartDateString = dateString;
                    }

                    var timeString = GetDateTimeStringFromInstanceData(state.LuisResult.Text, state.LuisResult.Entities._instance.datetime[0]);
                    var time = GetTimeFromDateTimeString(timeString, sc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                    if (time != null)
                    {
                        state.MeetingInfor.StartTime = time;
                    }

                    state.MeetingInfor.EndDate.Clear();
                    state.MeetingInfor.EndTime.Clear();
                    state.MeetingInfor.StartDateTime = null;
                    state.MeetingInfor.EndDateTime = null;
                    return await sc.ReplaceDialogAsync(Actions.BookMeetingRoom, options: sc.Options, cancellationToken: cancellationToken);
                }
                else if (state.LuisResult.TopIntent().intent == CalendarLuis.Intent.Reject)
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CalendarSharedResponses.CancelRequest));
                    state.Clear();
                    return await sc.EndDialogAsync();
                }
                else if (state.LuisResult.Entities.FloorNumber != null)
                {
                    string utterance = state.LuisResult.Entities.FloorNumber[0];
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
                else if (state.LuisResult.Entities.MeetingRoom != null || state.LuisResult.Entities.MeetingRoomName != null)
                {
                    state.MeetingInfor.MeetingRoomName = GetMeetingRoomFromEntity(state.LuisResult.Entities);
                    return await sc.ReplaceDialogAsync(Actions.CheckAvailability, options: sc.Options, cancellationToken: cancellationToken);
                }
                else if (state.LuisResult.Entities.Building != null || state.LuisResult.Entities.BuildingName != null)
                {
                    state.MeetingInfor.Building = GetBuildingFromEntity(state.LuisResult.Entities);
                    state.MeetingInfor.FloorNumber = null;
                    state.MeetingInfor.MeetingRoomName = null;
                    return await sc.ReplaceDialogAsync(Actions.FindMeetingRoom, options: sc.Options, cancellationToken: cancellationToken);
                }
                else if (state.LuisResult.TopIntent().intent == CalendarLuis.Intent.FindMeetingRoom || state.LuisResult.TopIntent().intent == CalendarLuis.Intent.CheckAvailability)
                {
                    state.MeetingInfor.MeetingRoomName = null;
                    return await sc.ReplaceDialogAsync(Actions.FindMeetingRoom, options: sc.Options, cancellationToken: cancellationToken);
                }
                else if (state.LuisResult.Text.ToLower().Contains(CalendarCommonStrings.Time))
                {
                    state.MeetingInfor.StartDate.Clear();
                    state.MeetingInfor.StartTime.Clear();
                    state.MeetingInfor.EndDate.Clear();
                    state.MeetingInfor.EndTime.Clear();
                    state.MeetingInfor.Duration = 0;
                    state.MeetingInfor.EndDateTime = null;
                    return await sc.ReplaceDialogAsync(Actions.BookMeetingRoom, options: sc.Options, cancellationToken: cancellationToken);
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
