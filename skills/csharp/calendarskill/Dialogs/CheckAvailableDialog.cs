using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Options;
using CalendarSkill.Responses.CheckAvailable;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace CalendarSkill.Dialogs
{
    public class CheckAvailableDialog : CalendarSkillDialogBase
    {
        public CheckAvailableDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            FindContactDialog findContactDialog,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(CheckAvailableDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            var checkAvailable = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                CollectContacts,
                CollectTime,
                CheckAvailable,
            };

            var collectTime = new WaterfallStep[]
            {
                AskForTimePrompt,
                AfterAskForTimePrompt
            };

            var findNextAvailableTime = new WaterfallStep[]
            {
                FindNextAvailableTimePrompt,
                AfterFindNextAvailableTimePrompt,
            };

            var createMeetingWithAvailableTime = new WaterfallStep[]
            {
                CreateMeetingPrompt,
                AfterCreateMeetingPrompt
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CheckAvailable, checkAvailable) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindNextAvailableTime, findNextAvailableTime) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectTime, collectTime) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CreateMeetingWithAvailableTime, createMeetingWithAvailableTime) { TelemetryClient = telemetryClient });
            AddDialog(findContactDialog ?? throw new ArgumentNullException(nameof(findContactDialog)));

            // Set starting dialog for component
            InitialDialogId = Actions.CheckAvailable;
        }

        private async Task<DialogTurnResult> CollectContacts(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(nameof(FindContactDialog), options: new FindContactDialogOptions(sc.Options), cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfor.StartDate.Any() || state.MeetingInfor.StartTime.Any())
                {
                    var userNow = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone());
                    var startDate = state.MeetingInfor.StartDate.Any() ? state.MeetingInfor.StartDate.Last() : userNow;
                    foreach (var startTime in state.MeetingInfor.StartTime)
                    {
                        var startDateTime = new DateTime(
                            startDate.Year,
                            startDate.Month,
                            startDate.Day,
                            startTime.Hour,
                            startTime.Minute,
                            startTime.Second);
                        if (state.MeetingInfor.StartDateTime == null)
                        {
                            state.MeetingInfor.StartDateTime = startDateTime;
                        }

                        if (startDateTime >= userNow)
                        {
                            state.MeetingInfor.StartDateTime = startDateTime;
                            break;
                        }
                    }

                    state.MeetingInfor.StartDateTime = TimeZoneInfo.ConvertTimeToUtc(state.MeetingInfor.StartDateTime.Value, state.GetUserTimeZone());
                    return await sc.NextAsync();
                }
                else
                {
                    return await sc.BeginDialogAsync(Actions.CollectTime, options: sc.Options, cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AskForTimePrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.TimePrompt, new TimePromptOptions()
                {
                    Prompt = ResponseManager.GetResponse(CheckAvailableResponses.AskForCheckAvailableTime),
                    RetryPrompt = ResponseManager.GetResponse(CheckAvailableResponses.AskForCheckAvailableTime),
                });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterAskForTimePrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;

                DateTime? startTime = null;

                foreach (var resolution in dateTimeResolutions)
                {
                    var utcNow = DateTime.UtcNow;
                    var dateTimeValue = DateTime.Parse(resolution.Value);
                    if (dateTimeValue == null)
                    {
                        continue;
                    }

                    dateTimeValue = TimeZoneInfo.ConvertTimeToUtc(dateTimeValue, state.GetUserTimeZone());

                    if (startTime == null)
                    {
                        startTime = dateTimeValue;
                    }

                    if (dateTimeValue >= utcNow)
                    {
                        startTime = dateTimeValue;
                        break;
                    }
                }

                state.MeetingInfor.StartDateTime = startTime;

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CheckAvailable(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                sc.Context.TurnState.TryGetValue(APITokenKey, out var token);
                var calendarService = ServiceManager.InitCalendarService((string)token, state.EventSource);

                var dateTime = state.MeetingInfor.StartDateTime;

                var me = await GetMe(sc.Context);

                // todo: change 5 to const
                // the last one in result is the current user
                var availabilityResult = await calendarService.GetUserAvailableTimeSlotAsync(me.Emails[0], new List<string>() { state.MeetingInfor.ContactInfor.Contacts[0].Address }, dateTime.Value, 5);

                if (!availabilityResult.AvailabilityViewList.First().StartsWith("0"))
                {
                    // the attendee is not available
                    var timeString = TimeConverter.ConvertUtcToUserTime(state.MeetingInfor.StartDateTime.Value, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime);
                    var dateString = string.Empty;
                    if (string.IsNullOrEmpty(state.MeetingInfor.StartDateString) ||
                        state.MeetingInfor.StartDateString.Equals(CalendarCommonStrings.TodayLower, StringComparison.InvariantCultureIgnoreCase) ||
                        state.MeetingInfor.StartDateString.Equals(CalendarCommonStrings.TomorrowLower, StringComparison.InvariantCultureIgnoreCase))
                    {
                        dateString = (state.MeetingInfor.StartDateString ?? CalendarCommonStrings.TodayLower).ToLower();
                    }
                    else
                    {
                        dateString = string.Format(CalendarCommonStrings.ShowEventDateCondition, state.MeetingInfor.StartDateString);
                    }

                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CheckAvailableResponses.NotAvailable, new StringDictionary()
                    {
                        { "UserName", state.MeetingInfor.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfor.ContactInfor.Contacts[0].Address },
                        { "Time", timeString },
                        { "Date", dateString }
                    }));

                    state.MeetingInfor.AvailabilityResult = availabilityResult;

                    return await sc.BeginDialogAsync(Actions.FindNextAvailableTime, sc.Options);
                }
                else
                {
                    // find the attendee's available time
                    var availableTime = 1;
                    var availabilityView = availabilityResult.AvailabilityViewList.First();
                    for (int i = 1; i < availabilityView.Length; i++)
                    {
                        if (availabilityView[i] == availabilityView[i - 1])
                        {
                            availableTime = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    availableTime *= 5;
                    var startAvailableTime = TimeConverter.ConvertUtcToUserTime(state.MeetingInfor.StartDateTime.Value, state.GetUserTimeZone());
                    var endAvailableTime = startAvailableTime.AddMinutes(availableTime);

                    if (availabilityResult.AvailabilityViewList.Last().StartsWith("0"))
                    {
                        // both attendee and current user is available
                        var responseParams = new StringDictionary()
                        {
                            { "StartTime", startAvailableTime.ToString(CommonStrings.DisplayTime) },
                            { "EndTime", endAvailableTime.ToString(CommonStrings.DisplayTime) },
                            { "User", state.MeetingInfor.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfor.ContactInfor.Contacts[0].Address }

                        };

                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CheckAvailableResponses.AttendeeIsAvailable, responseParams));
                    }
                    else
                    {
                        // attendee is available but current user is not available
                        var responseParams = new StringDictionary()
                        {
                            { "StartTime", startAvailableTime.ToString(CommonStrings.DisplayTime) },
                            { "EndTime", endAvailableTime.ToString(CommonStrings.DisplayTime) },
                            { "User", state.MeetingInfor.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfor.ContactInfor.Contacts[0].Address }
                        };

                        // todo: check conflict meetings
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CheckAvailableResponses.AttendeeIsAvailableOrgnizerIsUnavailableWithOneConflict, responseParams));
                    }

                    return await sc.BeginDialogAsync(Actions.CreateMeetingWithAvailableTime, sc.Options);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> FindNextAvailableTimePrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = ResponseManager.GetResponse(CheckAvailableResponses.AskForNextAvailableTime),
                    RetryPrompt = ResponseManager.GetResponse(CheckAvailableResponses.AskForNextAvailableTime)
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterFindNextAvailableTimePrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    // todo: show next available time
                    var availabilityResult = state.MeetingInfor.AvailabilityResult;

                    var startAvailableTimeIndex = -1;
                    var endAvailableTimeIndex = -1;

                    for (int i = 0; i < availabilityResult.AvailabilityViewList.First().Length; i++)
                    {
                        if (availabilityResult.AvailabilityViewList[0][i] == '0' && availabilityResult.AvailabilityViewList[1][i] == '0')
                        {
                            if (startAvailableTimeIndex < 0)
                            {
                                startAvailableTimeIndex = i;
                            }
                        }
                        else
                        {
                            if (startAvailableTimeIndex >= 0)
                            {
                                endAvailableTimeIndex = i - 1;
                                break;
                            }
                        }
                    }

                    if (startAvailableTimeIndex > 0)
                    {
                        endAvailableTimeIndex = endAvailableTimeIndex == -1 ? availabilityResult.AvailabilityViewList.First().Length - 1 : endAvailableTimeIndex;
                        var queryAvailableTime = TimeConverter.ConvertUtcToUserTime(state.MeetingInfor.StartDateTime.Value, state.GetUserTimeZone());

                        var startAvailableTime = queryAvailableTime.AddMinutes(startAvailableTimeIndex * 5);
                        var endAvailableTime = queryAvailableTime.AddMinutes(endAvailableTimeIndex * 5);

                        state.MeetingInfor.StartDateTime = startAvailableTime;
                        state.MeetingInfor.EndDateTime = endAvailableTime;

                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CheckAvailableResponses.NextBothAvailableTime, new StringDictionary()
                        {
                            { "UserName", state.MeetingInfor.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfor.ContactInfor.Contacts[0].Address },
                            { "StartTime", TimeConverter.ConvertUtcToUserTime(state.MeetingInfor.StartDateTime.Value, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime) },
                            { "EndTime", TimeConverter.ConvertUtcToUserTime(state.MeetingInfor.EndDateTime.Value, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime) },
                            { "EndDate", DisplayHelper.ToDisplayDate(state.MeetingInfor.EndDateTime.Value, state.GetUserTimeZone()) }
                        }));

                        return await sc.BeginDialogAsync(Actions.CreateMeetingWithAvailableTime, sc.Options);
                    }

                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CheckAvailableResponses.NoNextBothAvailableTime, new StringDictionary
                    {
                        { "UserName", state.MeetingInfor.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfor.ContactInfor.Contacts[0].Address }
                    }));

                    state.Clear();
                    return await sc.EndDialogAsync();
                }
                else
                {
                    state.Clear();
                    return await sc.EndDialogAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CreateMeetingPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = ResponseManager.GetResponse(CheckAvailableResponses.AskForCreateNewMeeting),
                    RetryPrompt = ResponseManager.GetResponse(CheckAvailableResponses.AskForCreateNewMeeting)
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterCreateMeetingPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    return await sc.BeginDialogAsync(nameof(CreateEventDialog), sc.Options);
                }
                else
                {
                    state.Clear();
                    return await sc.EndDialogAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}
