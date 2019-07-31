using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.TimeRemaining;
using CalendarSkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace CalendarSkill.Dialogs
{
    public class TimeRemainingDialog : CalendarSkillDialogBase
    {
        private ResourceMultiLanguageGenerator _lgMultiLangEngine;

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
            _lgMultiLangEngine = new ResourceMultiLanguageGenerator("TimeRemainingDialog.lg");
            TelemetryClient = telemetryClient;

            var timeRemain = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                CheckTimeRemain,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.ShowTimeRemaining, timeRemain) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.ShowTimeRemaining;
        }

        public async Task<DialogTurnResult> CheckTimeRemain(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.APIToken))
                {
                    return await sc.EndDialogAsync(true);
                }

                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);

                var eventList = await calendarService.GetUpcomingEvents();
                var nextEventList = new List<EventModel>();
                foreach (var item in eventList)
                {
                    var itemUserTimeZoneTime = TimeZoneInfo.ConvertTime(item.StartTime, TimeZoneInfo.Utc, state.GetUserTimeZone());
                    if (item.IsCancelled != true && nextEventList.Count == 0)
                    {
                        if (state.OrderReference.ToLower().Contains(CalendarCommonStrings.Next))
                        {
                            nextEventList.Add(item);
                        }
                        else if (state.StartDate.Any() && itemUserTimeZoneTime.DayOfYear == state.StartDate[0].DayOfYear)
                        {
                            nextEventList.Add(item);
                        }
                        else if (state.StartTime.Any() && itemUserTimeZoneTime == state.StartTime[0])
                        {
                            nextEventList.Add(item);
                        }
                        else if (state.Title != null && item.Title.Equals(state.Title, StringComparison.CurrentCultureIgnoreCase))
                        {
                            nextEventList.Add(item);
                        }
                    }
                }

                if (nextEventList.Count == 0)
                {
                    var showNoMeetingLGResult = await _lgMultiLangEngine.Generate(sc.Context, "[ShowNoMeetingMessage]", null);
                    var showNoMeetingPrompt = await new TextMessageActivityGenerator().CreateActivityFromText(sc.Context, showNoMeetingLGResult, null);

                    var prompt = (Activity)showNoMeetingPrompt;
                    await sc.Context.SendActivityAsync(prompt);
                    return await sc.EndDialogAsync();
                }
                else
                {
                    var userTimeZone = state.GetUserTimeZone();
                    var timeNow = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, userTimeZone);
                    var timeDiff = TimeZoneInfo.ConvertTime(nextEventList[0].StartTime, TimeZoneInfo.Utc, userTimeZone) - timeNow;
                    var timeDiffMinutes = (int)timeDiff.TotalMinutes % 60;
                    var timeDiffHours = (int)timeDiff.TotalMinutes / 60;
                    var timeDiffDays = timeDiff.Days;

                    var tokens = new StringDictionary()
                    {
                        { "remainingTime", string.Empty },
                        { "title", string.Empty },
                        { "time", string.Empty },
                        { "timeSpeak", string.Empty }
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
                    tokens["remainingTime"] = remainingTime;
                    if (state.OrderReference == "next")
                    {
                        var showTimeRemainingLGResult = await _lgMultiLangEngine.Generate(sc.Context, "[ShowNextMeetingTimeRemainingMessage]", tokens);
                        var showTimeRemainingPrompt = await new TextMessageActivityGenerator().CreateActivityFromText(sc.Context, showTimeRemainingLGResult, null);

                        await sc.Context.SendActivityAsync(showTimeRemainingPrompt);
                        return await sc.EndDialogAsync();
                    }
                    else
                    {
                        var timeToken = string.Empty;
                        var timeSpeakToken = string.Empty;

                        if (state.StartDate.Any())
                        {
                            timeSpeakToken += $"{state.StartDate[0].ToSpeechDateString()} ";
                            timeToken += $"{state.StartDate[0].ToShortDateString()} ";
                        }

                        if (state.StartTime.Any())
                        {
                            timeSpeakToken += $"{state.StartTime[0].ToSpeechTimeString()}";
                            timeToken += $"{state.StartTime[0].ToShortTimeString()}";
                        }

                        if (timeSpeakToken.Length > 0)
                        {
                            tokens["timeSpeak"] = CommonStrings.SpokenTimePrefix_One + " " + timeSpeakToken;
                        }

                        if (timeToken.Length > 0)
                        {
                            tokens["time"] = CommonStrings.SpokenTimePrefix_One + " " + timeToken;
                        }

                        if (state.Title != null)
                        {
                            tokens["title"] = string.Format(CalendarCommonStrings.WithTheSubject, state.Title);
                        }

                        var showTimeRemainingLGResult = await _lgMultiLangEngine.Generate(sc.Context, "[ShowTimeRemainingMessage]", tokens);
                        var showTimeRemainingPrompt = await new TextMessageActivityGenerator().CreateActivityFromText(sc.Context, showTimeRemainingLGResult, null);

                        await sc.Context.SendActivityAsync(showTimeRemainingPrompt);
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
    }
}