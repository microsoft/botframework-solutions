using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
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
using Microsoft.Graph;

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
                GetAuthToken,
                AfterGetAuthToken,
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
                if (!state.MeetingInfor.StartDate.Any() && !state.MeetingInfor.StartTime.Any())
                {
                    // when user say "Is Alex available", we think he mean right now.
                    // set time as the last five minutes time, for example now is 6:32, set start time as 6:35
                    var utcNow = DateTime.UtcNow;
                    var timeInterval = new TimeSpan(0, CalendarCommonUtil.AvailabilityViewInterval, 0);
                    var startTime = utcNow.AddTicks(timeInterval.Ticks - (utcNow.Ticks % timeInterval.Ticks));
                    state.MeetingInfor.StartTime.Add(TimeConverter.ConvertUtcToUserTime(startTime, state.GetUserTimeZone()));
                }

                if (state.MeetingInfor.StartTime.Any())
                {
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
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                return await sc.PromptAsync(Actions.TimePrompt, new TimePromptOptions()
                {
                    Prompt = ResponseManager.GetResponse(CheckAvailableResponses.AskForCheckAvailableTime),
                    RetryPrompt = ResponseManager.GetResponse(CheckAvailableResponses.AskForCheckAvailableTime),
                    TimeZone = state.GetUserTimeZone(),
                    MaxReprompt = CalendarCommonUtil.MaxRepromptCount
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
                foreach (var resolution in dateTimeResolutions)
                {
                    var dateTimeConvertType = resolution?.Timex;
                    var dateTimeValue = resolution?.Value;
                    if (dateTimeValue != null)
                    {
                        try
                        {
                            var dateTime = DateTime.Parse(dateTimeValue);

                            if (dateTime != null)
                            {
                                state.MeetingInfor.StartTime.Add(dateTime);
                            }
                        }
                        catch (FormatException ex)
                        {
                            await HandleExpectedDialogExceptions(sc, ex);
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

        private async Task<DialogTurnResult> CheckAvailable(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var userNow = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone());
                var startDate = state.MeetingInfor.StartDate.Any() ? state.MeetingInfor.StartDate.Last() : userNow.Date;

                List<DateTime> startTimes = new List<DateTime>();
                List<DateTime> endTimes = new List<DateTime>();
                foreach (var time in state.MeetingInfor.StartTime)
                {
                    startTimes.Add(startDate.AddSeconds(time.TimeOfDay.TotalSeconds));
                }

                var isStartTimeRestricted = Settings.RestrictedValue?.MeetingTime?.First(item => item.Name == "WorkTimeStart")?.IsRestricted;
                var isEndTimeRestricted = Settings.RestrictedValue?.MeetingTime?.First(item => item.Name == "WorkTimeEnd")?.IsRestricted;
                DateTime baseTime = new DateTime(startDate.Year, startDate.Month, startDate.Day);
                DateTime startTimeRestricted = isStartTimeRestricted.GetValueOrDefault() ? baseTime.AddSeconds(DateTime.Parse(Settings.RestrictedValue?.MeetingTime?.First(item => item.Name == "WorkTimeStart")?.Value).TimeOfDay.TotalSeconds) : baseTime;
                DateTime endTimeRestricted = isEndTimeRestricted.GetValueOrDefault() ? baseTime.AddSeconds(DateTime.Parse(Settings.RestrictedValue?.MeetingTime?.First(item => item.Name == "WorkTimeEnd")?.Value).TimeOfDay.TotalSeconds) : baseTime.AddDays(1);

                state.MeetingInfor.StartDateTime = DateTimeHelper.ChooseStartTime(startTimes, endTimes, startTimeRestricted, endTimeRestricted, userNow);
                state.MeetingInfor.StartDateTime = TimeZoneInfo.ConvertTimeToUtc(state.MeetingInfor.StartDateTime.Value, state.GetUserTimeZone());

                sc.Context.TurnState.TryGetValue(APITokenKey, out var token);
                var calendarService = ServiceManager.InitCalendarService(token as string, state.EventSource);

                var dateTime = state.MeetingInfor.StartDateTime;

                var me = await GetMe(sc.Context);

                // the last one in result is the current user
                var availabilityResult = await calendarService.GetUserAvailabilityAsync(me.Emails[0], new List<string>() { state.MeetingInfor.ContactInfor.Contacts[0].Address }, dateTime.Value, CalendarCommonUtil.AvailabilityViewInterval);

                var timeString = TimeConverter.ConvertUtcToUserTime(state.MeetingInfor.StartDateTime.Value, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime);
                var startDateString = string.Empty;
                if (string.IsNullOrEmpty(state.MeetingInfor.StartDateString) ||
                    state.MeetingInfor.StartDateString.Equals(CalendarCommonStrings.TodayLower, StringComparison.InvariantCultureIgnoreCase) ||
                    state.MeetingInfor.StartDateString.Equals(CalendarCommonStrings.TomorrowLower, StringComparison.InvariantCultureIgnoreCase))
                {
                    startDateString = (state.MeetingInfor.StartDateString ?? CalendarCommonStrings.TodayLower).ToLower();
                }
                else
                {
                    startDateString = string.Format(CalendarCommonStrings.ShowEventDateCondition, state.MeetingInfor.StartDateString);
                }

                if (!availabilityResult.AvailabilityViewList.First().StartsWith("0"))
                {
                    // the attendee is not available
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CheckAvailableResponses.NotAvailable, new StringDictionary()
                    {
                        { "UserName", state.MeetingInfor.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfor.ContactInfor.Contacts[0].Address },
                        { "Time", timeString },
                        { "Date", startDateString }
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
                        if (availabilityView[i] == '0')
                        {
                            availableTime = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    availableTime *= CalendarCommonUtil.AvailabilityViewInterval;
                    var startAvailableTime = TimeConverter.ConvertUtcToUserTime(state.MeetingInfor.StartDateTime.Value, state.GetUserTimeZone());
                    var endAvailableTime = startAvailableTime.AddMinutes(availableTime);

                    // the current user may in non-working time
                    if (availabilityResult.AvailabilityViewList.Last().StartsWith("0") || availabilityResult.AvailabilityViewList.Last().StartsWith("3"))
                    {
                        // both attendee and current user is available
                        state.MeetingInfor.IsOrgnizerAvailable = true;

                        var responseParams = new StringDictionary()
                        {
                            { "StartTime", startAvailableTime.ToString(CommonStrings.DisplayTime) },
                            { "EndTime", endAvailableTime.ToString(CommonStrings.DisplayTime) },
                            { "Date", DisplayHelper.ToDisplayDate(endAvailableTime, state.GetUserTimeZone()) },
                            { "UserName", state.MeetingInfor.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfor.ContactInfor.Contacts[0].Address }
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
                            { "Date", DisplayHelper.ToDisplayDate(endAvailableTime, state.GetUserTimeZone()) },
                            { "UserName", state.MeetingInfor.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfor.ContactInfor.Contacts[0].Address }
                        };

                        var conflictMeetingTitleList = new List<EventModel>();
                        foreach (var meeting in availabilityResult.MySchedule)
                        {
                            if (state.MeetingInfor.StartDateTime.Value >= meeting.StartTime && state.MeetingInfor.StartDateTime.Value < meeting.EndTime)
                            {
                                conflictMeetingTitleList.Add(meeting);
                            }
                        }

                        if (conflictMeetingTitleList.Count == 1)
                        {
                            responseParams.Add("Title", conflictMeetingTitleList.First().Title);
                            responseParams.Add("EventStartTime", TimeConverter.ConvertUtcToUserTime(conflictMeetingTitleList.First().StartTime, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime));
                            responseParams.Add("EventEndTime", TimeConverter.ConvertUtcToUserTime(conflictMeetingTitleList.First().EndTime, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime));

                            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CheckAvailableResponses.AttendeeIsAvailableOrgnizerIsUnavailableWithOneConflict, responseParams));
                        }
                        else
                        {
                            responseParams.Add("ConflictEventsCount", conflictMeetingTitleList.Count.ToString());
                            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CheckAvailableResponses.AttendeeIsAvailableOrgnizerIsUnavailableWithMutipleConflicts, responseParams));
                        }
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
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var responseParams = new StringDictionary()
                {
                    { "UserName", state.MeetingInfor.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfor.ContactInfor.Contacts[0].Address }
                };

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = ResponseManager.GetResponse(CheckAvailableResponses.AskForNextAvailableTime, responseParams),
                    RetryPrompt = ResponseManager.GetResponse(CheckAvailableResponses.AskForNextAvailableTime, responseParams)
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
                    state.MeetingInfor.IsOrgnizerAvailable = true;
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
                        var queryAvailableTime = state.MeetingInfor.StartDateTime.Value;

                        var startAvailableTime = queryAvailableTime.AddMinutes(startAvailableTimeIndex * CalendarCommonUtil.AvailabilityViewInterval);
                        var endAvailableTime = queryAvailableTime.AddMinutes((endAvailableTimeIndex + 1) * CalendarCommonUtil.AvailabilityViewInterval);

                        state.MeetingInfor.StartDateTime = startAvailableTime;

                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CheckAvailableResponses.NextBothAvailableTime, new StringDictionary()
                        {
                            { "UserName", state.MeetingInfor.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfor.ContactInfor.Contacts[0].Address },
                            { "StartTime", TimeConverter.ConvertUtcToUserTime(state.MeetingInfor.StartDateTime.Value, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime) },
                            { "EndTime", TimeConverter.ConvertUtcToUserTime(endAvailableTime, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime) },
                            { "EndDate", DisplayHelper.ToDisplayDate(TimeConverter.ConvertUtcToUserTime(endAvailableTime, state.GetUserTimeZone()), state.GetUserTimeZone()) }
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
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var responseId = state.MeetingInfor.IsOrgnizerAvailable ? CheckAvailableResponses.AskForCreateNewMeeting : CheckAvailableResponses.AskForCreateNewMeetingAnyway;
                var responseParams = new StringDictionary()
                {
                    { "UserName", state.MeetingInfor.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfor.ContactInfor.Contacts[0].Address },
                    { "StartTime", TimeConverter.ConvertUtcToUserTime(state.MeetingInfor.StartDateTime.Value, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime) }
                };

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = ResponseManager.GetResponse(responseId, responseParams),
                    RetryPrompt = ResponseManager.GetResponse(responseId, responseParams)
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
