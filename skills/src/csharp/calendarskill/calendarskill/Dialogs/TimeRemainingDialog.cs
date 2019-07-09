using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.TimeRemaining;
using CalendarSkill.Services;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;

namespace CalendarSkill.Dialogs
{
    public class TimeRemainingDialog : CalendarSkillDialogBase
    {
        public TimeRemainingDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(TimeRemainingDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            var timeRemain = new WaterfallStep[]
            {
                InitTimeRemainingDialogState,
                GetAuthToken,
                AfterGetAuthToken,
                CheckTimeRemain,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new CalendarWaterfallDialog(Actions.ShowTimeRemaining, timeRemain, CalendarStateAccessor) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.ShowTimeRemaining;
        }

        public async Task<DialogTurnResult> CheckTimeRemain(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CalendarDialogStateBase)sc.State.Dialog[CalendarStateKey];

                if (string.IsNullOrEmpty(userState.APIToken))
                {
                    return await sc.EndDialogAsync(true);
                }

                var calendarService = ServiceManager.InitCalendarService(userState.APIToken, userState.EventSource);

                var eventList = await calendarService.GetUpcomingEvents();
                var nextEventList = new List<EventModel>();
                foreach (var item in eventList)
                {
                    var itemUserTimeZoneTime = TimeZoneInfo.ConvertTime(item.StartTime, TimeZoneInfo.Utc, userState.GetUserTimeZone());
                    if (item.IsCancelled != true && nextEventList.Count == 0)
                    {
                        if (dialogState.OrderReference.ToLower().Contains(CalendarCommonStrings.Next))
                        {
                            nextEventList.Add(item);
                        }
                        else if (dialogState.StartDate.Any() && itemUserTimeZoneTime.DayOfYear == dialogState.StartDate[0].DayOfYear)
                        {
                            nextEventList.Add(item);
                        }
                        else if (dialogState.StartTime.Any() && itemUserTimeZoneTime == dialogState.StartTime[0])
                        {
                            nextEventList.Add(item);
                        }
                        else if (dialogState.Title != null && item.Title.Equals(dialogState.Title, StringComparison.CurrentCultureIgnoreCase))
                        {
                            nextEventList.Add(item);
                        }
                    }
                }

                if (nextEventList.Count == 0)
                {
                    var prompt = ResponseManager.GetResponse(TimeRemainingResponses.ShowNoMeetingMessage);
                    await sc.Context.SendActivityAsync(prompt);
                    return await sc.EndDialogAsync();
                }
                else
                {
                    var userTimeZone = userState.GetUserTimeZone();
                    var timeNow = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, userTimeZone);
                    var timeDiff = TimeZoneInfo.ConvertTime(nextEventList[0].StartTime, TimeZoneInfo.Utc, userTimeZone) - timeNow;
                    var timeDiffMinutes = (int)timeDiff.TotalMinutes % 60;
                    var timeDiffHours = (int)timeDiff.TotalMinutes / 60;
                    var timeDiffDays = timeDiff.Days;

                    var tokens = new StringDictionary()
                    {
                        { "RemainingTime", string.Empty },
                        { "Title", string.Empty },
                        { "Time", string.Empty },
                        { "TimeSpeak", string.Empty }
                    };

                    var remainingMinutes = string.Empty;
                    var remainingHours = string.Empty;
                    var remainingDays = string.Empty;

                    if (timeDiffMinutes > 0)
                    {
                        if (timeDiffMinutes > 1)
                        {
                            remainingMinutes = string.Format(CommonStrings.TimeFormatMinutes, timeDiffMinutes) + " ";
                        }
                        else
                        {
                            remainingMinutes = string.Format(CommonStrings.TimeFormatMinute, timeDiffMinutes) + " ";
                        }
                    }

                    if (timeDiffHours > 0)
                    {
                        if (timeDiffHours > 1)
                        {
                            remainingHours = string.Format(CommonStrings.TimeFormatHours, timeDiffHours) + " ";
                        }
                        else
                        {
                            remainingHours = string.Format(CommonStrings.TimeFormatHour, timeDiffHours) + " ";
                        }
                    }

                    if (timeDiffDays > 0)
                    {
                        if (timeDiffDays > 1)
                        {
                            remainingDays = string.Format(CommonStrings.TimeFormatDays, timeDiffDays) + " ";
                        }
                        else
                        {
                            remainingDays = string.Format(CommonStrings.TimeFormatDay, timeDiffDays) + " ";
                        }
                    }

                    var remainingTime = $"{remainingDays}{remainingHours}{remainingMinutes}";
                    tokens["RemainingTime"] = remainingTime;
                    if (dialogState.OrderReference == "next")
                    {
                        var prompt = ResponseManager.GetResponse(TimeRemainingResponses.ShowNextMeetingTimeRemainingMessage, tokens);
                        await sc.Context.SendActivityAsync(prompt);
                        return await sc.EndDialogAsync();
                    }
                    else
                    {
                        var timeToken = string.Empty;
                        var timeSpeakToken = string.Empty;

                        if (dialogState.StartDate.Any())
                        {
                            timeSpeakToken += $"{dialogState.StartDate[0].ToSpeechDateString()} ";
                            timeToken += $"{dialogState.StartDate[0].ToShortDateString()} ";
                        }

                        if (dialogState.StartTime.Any())
                        {
                            timeSpeakToken += $"{dialogState.StartTime[0].ToSpeechTimeString()}";
                            timeToken += $"{dialogState.StartTime[0].ToShortTimeString()}";
                        }

                        if (timeSpeakToken.Length > 0)
                        {
                            tokens["TimeSpeak"] = CommonStrings.SpokenTimePrefix_One + " " + timeSpeakToken;
                        }

                        if (timeToken.Length > 0)
                        {
                            tokens["Time"] = CommonStrings.SpokenTimePrefix_One + " " + timeToken;
                        }

                        if (dialogState.Title != null)
                        {
                            tokens["Title"] = string.Format(CalendarCommonStrings.WithTheSubject, dialogState.Title);
                        }

                        var prompt = ResponseManager.GetResponse(TimeRemainingResponses.ShowTimeRemainingMessage, tokens);
                        await sc.Context.SendActivityAsync(prompt);
                        return await sc.EndDialogAsync();
                    }
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

        private async Task<DialogTurnResult> InitTimeRemainingDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = new CalendarDialogStateBase();

                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = Services.CognitiveModelSets[locale];

                // Update state with email luis result and entities --- todo: use luis result in adaptive dialog
                var luisResult = await localeConfig.LuisServices["calendar"].RecognizeAsync<calendarLuis>(sc.Context);
                userState.LuisResult = luisResult;
                localeConfig.LuisServices.TryGetValue("general", out var luisService);
                var generalLuisResult = await luisService.RecognizeAsync<General>(sc.Context);
                userState.GeneralLuisResult = generalLuisResult;

                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (skillOptions != null && skillOptions.SubFlowMode)
                {
                    dialogState = userState?.CacheModel != null ? new CalendarDialogStateBase(userState?.CacheModel) : dialogState;
                }

                var newState = await DigestTimeRemainingLuisResult(sc, userState.LuisResult, userState.GeneralLuisResult, dialogState, true);
                sc.State.Dialog.Add(CalendarStateKey, newState);

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> SaveTimeRemainingDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var dialogState = skillOptions?.DialogState != null ? skillOptions?.DialogState : new CalendarDialogStateBase();

                if (skillOptions != null && skillOptions.DialogState != null)
                {
                    if (skillOptions.DialogState is CalendarDialogStateBase)
                    {
                        dialogState = skillOptions.DialogState;
                    }
                    else
                    {
                        dialogState = skillOptions.DialogState != null ? new CalendarDialogStateBase(skillOptions.DialogState) : dialogState;
                    }
                }

                var userState = await CalendarStateAccessor.GetAsync(sc.Context);

                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = Services.CognitiveModelSets[locale];

                // Update state with email luis result and entities --- todo: use luis result in adaptive dialog
                var luisResult = await localeConfig.LuisServices["calendar"].RecognizeAsync<calendarLuis>(sc.Context);
                userState.LuisResult = luisResult;
                localeConfig.LuisServices.TryGetValue("general", out var luisService);
                var generalLuisResult = await luisService.RecognizeAsync<General>(sc.Context);
                userState.GeneralLuisResult = generalLuisResult;

                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                var newState = await DigestTimeRemainingLuisResult(sc, userState.LuisResult, userState.GeneralLuisResult, dialogState as CalendarDialogStateBase, false);
                sc.State.Dialog.Add(CalendarStateKey, newState);

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<CalendarDialogStateBase> DigestTimeRemainingLuisResult(DialogContext dc, calendarLuis luisResult, General generalLuisResult, CalendarDialogStateBase state, bool isBeginDialog)
        {
            try
            {
                var userState = await CalendarStateAccessor.GetAsync(dc.Context);

                var intent = luisResult.TopIntent().intent;

                var entity = luisResult.Entities;

                if (!isBeginDialog)
                {
                    return state;
                }

                switch (intent)
                {
                    case calendarLuis.Intent.TimeRemaining:
                        {
                            if (entity.FromDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, userState.GetUserTimeZone());
                                if (date != null)
                                {
                                    state.StartDate = date;
                                }
                            }

                            if (entity.ToDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, userState.GetUserTimeZone());
                                if (date != null)
                                {
                                    state.EndDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, userState.GetUserTimeZone(), true);
                                if (time != null)
                                {
                                    state.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, userState.GetUserTimeZone(), false);
                                if (time != null)
                                {
                                    state.EndTime = time;
                                }
                            }

                            if (entity.ToTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, userState.GetUserTimeZone());
                                if (time != null)
                                {
                                    state.EndTime = time;
                                }
                            }

                            if (entity.OrderReference != null)
                            {
                                state.OrderReference = GetOrderReferenceFromEntity(entity);
                            }

                            if (entity.Subject != null)
                            {
                                state.Title = entity._instance.Subject[0].Text;
                            }

                            break;
                        }

                }

                return state;
            }
            catch
            {
                await ClearAllState(dc.Context);
                await dc.CancelAllDialogsAsync();
                throw;
            }
        }
    }
}