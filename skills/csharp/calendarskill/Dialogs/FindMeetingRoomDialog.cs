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
using CalendarSkill.Prompts;
using CalendarSkill.Prompts.Options;
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
                GetAuthToken,
                AfterGetAuthToken,
                GetMeetingRoomInfo,
                CollectDuration,
                CollectMeetingRoomInfo,
                FindAnAvailableMeetingRoom,
                AfterConfirmMeetingRoom,
                CollectTitle,
                CollectInvitees,
                ConfirmBeforeCreatePrompt,
                AfterConfirmBeforeCreatePrompt,
                BookMeetingRoom
            };

            var updateDuration = new WaterfallStep[]
            {
                UpdateDurationForCreate,
                AfterUpdateDurationForCreate,
            };

            var collectMeetingRoomInfo = new WaterfallStep[]
            {
                CollectBuilding,
                CollectFloorNumber,
                //ConfirmMeetingRoom,
                //CheckMeetingRoomAvailable
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

            var collectTitle = new WaterfallStep[]
            {
                CollectTitlePrompt,
                AfterCollectTitlePrompt
            };

            var collectInvitees = new WaterfallStep[]
            {
                CollectInviteesPrompt,
                AfterCollectInviteesPrompt
            };

            /*
            var selectMeetingRoom = new WaterfallStep[]
            {
                SelectMeetingRoom,
                AfterSelectMeetingRoom,
            };
            */

            /*
            var updateLocation = new WaterfallStep[]
            {
                UpdateLocation,
                AfterUpdateLocation,
            };
            */

            AddDialog(new WaterfallDialog(Actions.UpdateDurationForCreate, updateDuration) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.BookMeetingRoom, bookMeetingRoom) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectMeetingRoomInfo, collectMeetingRoomInfo) { TelemetryClient = telemetryClient });
            //AddDialog(new WaterfallDialog(Actions.SelectMeetingRoom, selectMeetingRoom) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectBuilding, collectBuilding) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectFloorNumber, collectFloorNumber) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectTitle, collectTitle) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectInvitees, collectInvitees) { TelemetryClient = telemetryClient });
            AddDialog(new DatePrompt(Actions.DatePromptForCreate));
            AddDialog(new TimePrompt(Actions.TimePromptForCreate));
            AddDialog(new DurationPrompt(Actions.DurationPromptForCreate));
            AddDialog(new GetRecreateInfoPrompt(Actions.GetRecreateInfoPrompt));

            InitialDialogId = Actions.BookMeetingRoom;
        }

        private async Task<DialogTurnResult> CollectMeetingRoomInfo(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectMeetingRoomInfo);
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

        private async Task<DialogTurnResult> BookMeetingRoom(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                //List<EventModel.Attendee> attendees = new List<EventModel.Attendee>();
                List<EventModel.Attendee> attendees = state.MeetingInfor.ContactInfor.Contacts;
                attendees.Add(new EventModel.Attendee
                {
                    DisplayName = state.MeetingInfor.MeetingRoom.DisplayName,
                    Address = state.MeetingInfor.MeetingRoom.EmailAddress,
                    AttendeeType = AttendeeType.Resource
                });

                var newEvent = new EventModel(state.EventSource)
                {
                    Attendees = attendees,
                    StartTime = (DateTime)state.MeetingInfor.StartDateTime,
                    EndTime = (DateTime)state.MeetingInfor.EndDateTime,
                    TimeZone = TimeZoneInfo.Utc,
                    Location = state.MeetingInfor.MeetingRoom.DisplayName,
                    Title = state.MeetingInfor.Title
                };

                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);
                if (await calendarService.CreateEventAysnc(newEvent) != null)
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindMeetingRoomResponses.MeetingBooked));
                }
                else
                {
                    var prompt = ResponseManager.GetResponse(CreateEventResponses.EventCreationFailed);
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
                }

                state.Clear();

                return await sc.EndDialogAsync();
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

        private async Task<DialogTurnResult> GetMeetingRoomInfo(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                DateTime dateNow = DateTime.Now;


                DateTime startDate = state.MeetingInfor.StartDate.Count() > 0 ? state.MeetingInfor.StartDate[0] : dateNow;
                DateTime startTime = state.MeetingInfor.StartTime.Count() > 0 ? state.MeetingInfor.StartTime[0] : dateNow;
                state.MeetingInfor.StartDateTime = new DateTime(
                        startDate.Year,
                        startDate.Month,
                        startDate.Day,
                        startTime.Hour,
                        startTime.Minute,
                        startTime.Second);

                /*
                int duration = state.MeetingInfor.Duration > 0 ? state.MeetingInfor.Duration : 1800;
                DateTime endDate = state.MeetingInfor.EndDate.Count() > 0 ? state.MeetingInfor.EndDate[0] : ((DateTime)state.MeetingInfor.StartDateTime).AddSeconds(duration);
                DateTime endTime = state.MeetingInfor.EndTime.Count() > 0 ? state.MeetingInfor.EndTime[0] : ((DateTime)state.MeetingInfor.StartDateTime).AddSeconds(duration);
                state.MeetingInfor.EndDateTime = new DateTime(
                        endDate.Year,
                        endDate.Month,
                        endDate.Day,
                        endTime.Hour,
                        endTime.Minute,
                        endTime.Second);

                state.MeetingInfor.Duration = (int)state.MeetingInfor.StartDateTime.Value.Subtract(state.MeetingInfor.EndDateTime.Value).Duration().TotalSeconds;
                state.MeetingInfor.StartDateTime = TimeZoneInfo.ConvertTimeToUtc(state.MeetingInfor.StartDateTime.Value, state.GetUserTimeZone());
                state.MeetingInfor.EndDateTime = TimeZoneInfo.ConvertTimeToUtc(state.MeetingInfor.EndDateTime.Value, state.GetUserTimeZone());
                */

                // ask for Location
                //if (state.MeetingInfor.Location == null)
                //{
                //    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(FindMeetingRoomResponses.NoMeetingRoom) }, cancellationToken);
                //}
                //else
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

        private async Task<DialogTurnResult> ConfirmMeetingRoom(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var token = state.APIToken;
                //var service = ServiceManager.InitPlaceService(token, state.EventSource);

                // get name list from sc.result
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                    state.MeetingInfor.Location = userInput;
                }

                //bool updated = await CheckMeetingRoomDataUpdated(sc);
                List<PlaceModel> meetingRooms = await _azureSearchService.GetMeetingRoomByTitleAsync(state.MeetingInfor.Location);

                if (meetingRooms.Count == 0)
                {
                    state.MeetingInfor.Location = null;
                    return await sc.ReplaceDialogAsync(Actions.CollectMeetingRoomInfo, sc.Options, cancellationToken);
                }
                else if (meetingRooms.Count == 1)
                {
                    state.MeetingInfor.MeetingRoom = meetingRooms[0];
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    state.MeetingInfor.UnconfirmedMeetingRoom = meetingRooms;
                    return await sc.BeginDialogAsync(Actions.SelectMeetingRoom, sc.Options, cancellationToken);
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
                    return await sc.EndDialogAsync();
                }
                else
                {
                    state.MeetingInfor.Location = null;
                    return await sc.ReplaceDialogAsync(Actions.CollectMeetingRoomInfo, sc.Options, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<bool> CheckMeetingRoomDataUpdated(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            AzureSearchService azureSearchService = new AzureSearchService(Settings);
            AzureCosmosService azureCosmosService = new AzureCosmosService(Settings);

            if (azureCosmosService.DataExpiredOrNonExist())
            {
                var state = await Accessor.GetAsync(sc.Context);
                var token = state.APIToken;
                var service = ServiceManager.InitPlaceService(token, state.EventSource);
                List<PlaceModel> meetingrooms = await service.GetMeetingRoomAsync();
                if (meetingrooms.Count > 0)
                {
                    bool updated = await azureCosmosService.UpdateAsync(meetingrooms);
                    if (updated)
                    {
                        await azureSearchService.BuildIndexAsync();
                        return true;
                    }
                }

                return false;
            }

            return true;
        }

        private async Task<DialogTurnResult> SelectMeetingRoom(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var unionList = state.MeetingInfor.UnconfirmedMeetingRoom;
                if (unionList.Count <= ConfigData.GetInstance().MaxDisplaySize)
                {
                    return await sc.PromptAsync(Actions.Choice, await GenerateOptionsForMeetingRoom(sc, unionList, sc.Context, true));
                }
                else
                {
                    return await sc.PromptAsync(Actions.Choice, await GenerateOptionsForMeetingRoom(sc, unionList, sc.Context, false));
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<PromptOptions> GenerateOptionsForMeetingRoom(WaterfallStepContext sc, List<PlaceModel> unionList, ITurnContext context, bool isSinglePage = true)
        {
            var state = await Accessor.GetAsync(context);
            var pageIndex = state.MeetingInfor.ShowMeetingRoomIndex;
            var pageSize = 3;
            var skip = pageSize * pageIndex;
            var firstMeetingRoomName = state.MeetingInfor.UnconfirmedMeetingRoom[skip].DisplayName;

            // Go back to the last page when reaching the end.
            if (skip >= unionList.Count && pageIndex > 0)
            {
                state.MeetingInfor.ShowMeetingRoomIndex--;
                pageIndex = state.MeetingInfor.ShowMeetingRoomIndex;
                skip = pageSize * pageIndex;
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindMeetingRoomResponses.AlreadyLastPage));
            }

            var options = new PromptOptions
            {
                Choices = new List<Choice>(),
                Prompt = ResponseManager.GetResponse(FindMeetingRoomResponses.ConfirmMultipleMeetingRoomSinglePage, new StringDictionary() { { "RoomName", firstMeetingRoomName } })
            };

            if (!isSinglePage)
            {
                options.Prompt = ResponseManager.GetResponse(FindMeetingRoomResponses.ConfirmMultipleContactNameMultiPage, new StringDictionary() { { "RoomName", firstMeetingRoomName } });
            }

            for (var i = 0; i < unionList.Count; i++)
            {
                var room = unionList[i];

                var choice = new Choice()
                {
                    Value = $"**{room.DisplayName}**",
                    Synonyms = new List<string> { (options.Choices.Count + 1).ToString(), room.DisplayName, room.DisplayName.ToLower() },
                };

                if (skip <= 0)
                {
                    if (options.Choices.Count >= pageSize)
                    {
                        options.Prompt.Speak = SpeechUtility.ListToSpeechReadyString(options, ReadPreference.Chronological, ConfigData.GetInstance().MaxReadSize);
                        options.Prompt.Text = GetSelectPromptString(options, true);
                        options.RetryPrompt = ResponseManager.GetResponse(CalendarSharedResponses.DidntUnderstandMessage);
                        return options;
                    }

                    options.Choices.Add(choice);
                }
                else
                {
                    skip--;
                }
            }

            options.Prompt.Speak = SpeechUtility.ListToSpeechReadyString(options, ReadPreference.Chronological, ConfigData.GetInstance().MaxReadSize);
            options.Prompt.Text = GetSelectPromptString(options, true);
            options.RetryPrompt = ResponseManager.GetResponse(CalendarSharedResponses.DidntUnderstandMessage);
            return options;
        }

        private async Task<DialogTurnResult> AfterSelectMeetingRoom(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var luisResult = state.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;
                var generlLuisResult = state.GeneralLuisResult;
                var generalTopIntent = generlLuisResult?.TopIntent().intent;
                generalTopIntent = MergeShowIntent(generalTopIntent, topIntent, luisResult);

                if (sc.Result == null)
                {
                    if (generalTopIntent == General.Intent.ShowNext)
                    {
                        state.MeetingInfor.ShowMeetingRoomIndex++;
                    }
                    else if (generalTopIntent == General.Intent.ShowPrevious)
                    {
                        if (state.MeetingInfor.ShowMeetingRoomIndex > 0)
                        {
                            state.MeetingInfor.ShowMeetingRoomIndex--;
                        }
                        else
                        {
                            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindMeetingRoomResponses.AlreadyFirstPage));
                        }
                    }
                    else
                    {
                        // result is null when just update the recipient name. show recipients page should be reset.
                        state.MeetingInfor.ShowMeetingRoomIndex = 0;
                    }

                    return await sc.ReplaceDialogAsync(Actions.SelectMeetingRoom, options: sc.Options, cancellationToken: cancellationToken);
                }

                var choiceResult = (sc.Result as FoundChoice)?.Value.Trim('*');
                if (choiceResult != null)
                {
                    // Clean up data
                    state.MeetingInfor.ShowMeetingRoomIndex = 0;

                    // Start to confirm the email
                    var confirmedRoom = state.MeetingInfor.UnconfirmedMeetingRoom.Where(p => p.DisplayName.ToLower() == choiceResult.ToLower()).First();
                    state.MeetingInfor.MeetingRoom = confirmedRoom;
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AskforUserName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                state.MeetingInfor.ContactInfor.UnconfirmedContact.Clear();
                state.MeetingInfor.ContactInfor.ConfirmedContact = null;
                return await sc.PromptAsync(
                    Actions.Prompt,
                    new PromptOptions
                    {
                        Prompt = ResponseManager.GetResponse(CreateEventResponses.NoAttendees)
                    });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}
