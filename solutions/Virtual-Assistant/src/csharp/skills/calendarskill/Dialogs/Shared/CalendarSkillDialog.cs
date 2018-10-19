using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Recognizers.Text;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace CalendarSkill
{
    public class CalendarSkillDialog : ComponentDialog
    {
        // Constants
        public const string SkillModeAuth = "SkillAuth";
        public const string LocalModeAuth = "LocalAuth";

        // Fields
        protected SkillConfiguration _services;
        protected IStatePropertyAccessor<CalendarSkillState> _accessor;
        protected IServiceManager _serviceManager;
        protected CalendarSkillResponseBuilder _responseBuilder = new CalendarSkillResponseBuilder();

        public CalendarSkillDialog(
            string dialogId,
            SkillConfiguration services,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager)
            : base(dialogId)
        {
            _services = services;
            _accessor = accessor;
            _serviceManager = serviceManager;

            var oauthSettings = new OAuthPromptSettings()
            {
                ConnectionName = _services.AuthConnectionName,
                Text = $"Authentication",
                Title = "Signin",
                Timeout = 300000, // User has 5 minutes to login
            };

            AddDialog(new EventPrompt(SkillModeAuth, "tokens/response", TokenResponseValidator));
            AddDialog(new OAuthPrompt(LocalModeAuth, oauthSettings, AuthPromptValidator));
            AddDialog(new TextPrompt(Actions.Prompt));
            AddDialog(new ConfirmPrompt(Actions.TakeFurtherAction, null, Culture.English) { Style = ListStyle.SuggestedAction });
            AddDialog(new DateTimePrompt(Actions.DateTimePrompt, null, Culture.English));
            AddDialog(new DateTimePrompt(Actions.DateTimePromptForUpdateDelete, DateTimePromptValidator, Culture.English));
            AddDialog(new ChoicePrompt(Actions.Choice, ChoiceValidator, Culture.English) { Style = ListStyle.None, });
            AddDialog(new ChoicePrompt(Actions.EventChoice, null, Culture.English) { Style = ListStyle.Inline, ChoiceOptions = new ChoiceFactoryOptions { InlineSeparator = string.Empty, InlineOr = string.Empty, InlineOrMore = string.Empty, IncludeNumbers = false } });
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _accessor.GetAsync(dc.Context);
            await DigestCalendarLuisResult(dc, state.LuisResult);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _accessor.GetAsync(dc.Context);
            await DigestCalendarLuisResult(dc, state.LuisResult);
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        // Shared steps
        public async Task<DialogTurnResult> GetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;

                // If in Skill mode we ask the calling Bot for the token
                if (skillOptions != null && skillOptions.SkillMode)
                {
                    // We trigger a Token Request from the Parent Bot by sending a "TokenRequest" event back and then waiting for a "TokenResponse"
                    // TODO Error handling - if we get a new activity that isn't an event
                    var response = sc.Context.Activity.CreateReply();
                    response.Type = ActivityTypes.Event;
                    response.Name = "tokens/request";

                    // Send the tokens/request Event
                    await sc.Context.SendActivityAsync(response);

                    // Wait for the tokens/response event
                    return await sc.PromptAsync(SkillModeAuth, new PromptOptions());
                }
                else
                {
                    return await sc.PromptAsync(LocalModeAuth, new PromptOptions());
                }
            }
            catch
            {
                // await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SharedResponses.AuthFailed));
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> AfterGetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // When the user authenticates interactively we pass on the tokens/Response event which surfaces as a JObject
                // When the token is cached we get a TokenResponse object.
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                TokenResponse tokenResponse;
                if (skillOptions.SkillMode)
                {
                    var resultType = sc.Context.Activity.Value.GetType();
                    if (resultType == typeof(TokenResponse))
                    {
                        tokenResponse = sc.Context.Activity.Value as TokenResponse;
                    }
                    else
                    {
                        var tokenResponseObject = sc.Context.Activity.Value as JObject;
                        tokenResponse = tokenResponseObject?.ToObject<TokenResponse>();
                    }
                }
                else
                {
                    tokenResponse = sc.Result as TokenResponse;
                }

                if (tokenResponse != null)
                {
                    var state = await _accessor.GetAsync(sc.Context);
                    state.APIToken = tokenResponse.Token;
                }

                return await sc.NextAsync();
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        // Validators
        public Task<bool> TokenResponseValidator(PromptValidatorContext<Activity> pc, CancellationToken cancellationToken)
        {
            var activity = pc.Recognized.Value;
            if (activity != null && activity.Type == ActivityTypes.Event)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> AuthPromptValidator(PromptValidatorContext<TokenResponse> promptContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public async Task<bool> ChoiceValidator(PromptValidatorContext<FoundChoice> pc, CancellationToken cancellationToken)
        {

            var state = await _accessor.GetAsync(pc.Context);
            var luisResult = state.LuisResult;
            var topIntent = luisResult?.TopIntent().intent;

            // TODO: The signature for validators has changed to return bool -- Need new way to handle this logic
            // If user want to show more recipient end current choice dialog and return the intent to next step.
            if (topIntent == Luis.Calendar.Intent.ShowNext || topIntent == Luis.Calendar.Intent.ShowPrevious)
            {
                // pc.End(topIntent);
                return true;
            }
            else
            {
                if (!pc.Recognized.Succeeded || pc.Recognized == null)
                {
                    // do nothing when not recognized.
                }
                else
                {
                    // pc.End(pc.Recognized.Value);
                    return true;
                }
            }

            return false;
        }

        public Task<bool> DateTimePromptValidator(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        // Helpers
        public async Task ShowMeetingList(DialogContext dc, List<EventModel> events, bool showDate = true)
        {
            var replyToConversation = dc.Context.Activity.CreateReply();
            replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            replyToConversation.Attachments = new List<Microsoft.Bot.Schema.Attachment>();

            var cardsData = new List<CalendarCardData>();
            foreach (var item in events)
            {
                var meetingCard = item.ToAdaptiveCardData(showDate);
                var replyTemp = dc.Context.Activity.CreateAdaptiveCardReply(CalendarMainResponses.GreetingMessage, item.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", meetingCard);
                replyToConversation.Attachments.Add(replyTemp.Attachments[0]);
            }

            await dc.Context.SendActivityAsync(replyToConversation);
        }

        public static bool IsRelativeTime(string userInput, string resolverResult, string timex)
        {
            if (userInput.Contains("ago") ||
                userInput.Contains("before") ||
                userInput.Contains("later") ||
                userInput.Contains("next"))
            {
                return true;
            }

            if (userInput.Contains("today") ||
                userInput.Contains("now") ||
                userInput.Contains("yesterday") ||
                userInput.Contains("tomorrow"))
            {
                return true;
            }

            if (timex == "PRESENT_REF")
            {
                return true;
            }

            return false;
        }

        public static async Task<List<EventModel>> GetEventsByTime(DateTime? startDate, DateTime? startTime, DateTime? endDateTime, TimeZoneInfo userTimeZone, ICalendar calendarService)
        {
            if (startDate == null)
            {
                return null;
            }

            var rawEvents = new List<EventModel>();
            var resultEvents = new List<EventModel>();
            DateTime searchStartTime;

            if (startTime == null || endDateTime != null)
            {
                searchStartTime = new DateTime(startDate.Value.Year, startDate.Value.Month, startDate.Value.Day);
                var endTime = new DateTime(startDate.Value.Year, startDate.Value.Month, startDate.Value.Day, 23, 59, 59);
                if (endDateTime != null)
                {
                    searchStartTime = new DateTime(searchStartTime.Year, searchStartTime.Month, searchStartTime.Day, startTime.Value.Hour, startTime.Value.Minute, startTime.Value.Second);
                    endTime = endDateTime.Value;
                }

                var startTimeUtc = TimeZoneInfo.ConvertTimeToUtc(searchStartTime, userTimeZone);
                var endTimeUtc = TimeZoneInfo.ConvertTimeToUtc(endTime, userTimeZone);
                rawEvents = await calendarService.GetEventsByTime(startTimeUtc, endTimeUtc);
            }
            else
            {
                var searchTime = TimeZoneInfo.ConvertTime(startTime.Value, TimeZoneInfo.Local, userTimeZone);
                searchStartTime = new DateTime(startDate.Value.Year, startDate.Value.Month, startDate.Value.Day, searchTime.Hour, searchTime.Minute, 0);
                rawEvents = await calendarService.GetEventsByStartTime(searchStartTime);
            }

            foreach (var item in rawEvents)
            {
                if (item.StartTime >= startDate && item.StartTime >= searchStartTime && item.IsCancelled != true)
                {
                    resultEvents.Add(item);
                }
            }

            return resultEvents;
        }

        public static bool ContainsTime(string timex)
        {
            return timex.Contains("T");
        }

        public static DateTime ParseToDateTime(string dateTimeString)
        {
            string[] formats =
            {
                "yyyy-MM-ddTHH",
                "yyyy-MM-ddTHH:mm",
                "yyyy-MM-ddTHH:mm:ss",
            };
            return DateTime.ParseExact(dateTimeString, formats, null);
        }

        public static DateTime ParseToTime(string timeString)
        {
            string[] formats =
            {
                "THH",
                "THH:mm",
                "THH:mm:ss",
            };
            return DateTime.ParseExact(timeString, formats, null);
        }

        public static void ParseToTimeRange(string dateTimeString, out DateTime? startDateTime, out DateTime? endDateTime)
        {
            var timeRange = dateTimeString.Split("T")[1];
            var date = DateTime.Parse(dateTimeString.Split("T")[0]);
            if (timeRange == "MO")
            {
                startDateTime = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
                endDateTime = new DateTime(date.Year, date.Month, date.Day, 11, 59, 59);
            }
            else if (timeRange == "AF")
            {
                startDateTime = new DateTime(date.Year, date.Month, date.Day, 12, 0, 0);
                endDateTime = new DateTime(date.Year, date.Month, date.Day, 16, 59, 59);
            }
            else if (timeRange == "EV")
            {
                startDateTime = new DateTime(date.Year, date.Month, date.Day, 17, 0, 0);
                endDateTime = new DateTime(date.Year, date.Month, date.Day, 20, 59, 59);
            }
            else if (timeRange == "NI")
            {
                startDateTime = new DateTime(date.Year, date.Month, date.Day, 21, 0, 0);
                endDateTime = new DateTime(date.Year, date.Month, date.Day, 23, 59, 59);
            }
            else
            {
                startDateTime = null;
                endDateTime = null;
            }
        }

        private async Task DigestCalendarLuisResult(DialogContext dc, Calendar luisResult)
        {
            try
            {
                var state = await _accessor.GetAsync(dc.Context);

                var entity = luisResult.Entities;
                if (entity.Subject != null)
                {
                    state.Title = entity.Subject[0];
                }

                if (entity.ContactName != null)
                {
                    foreach (var name in entity.ContactName)
                    {
                        if (!state.AttendeesNameList.Contains(name))
                        {
                            state.AttendeesNameList.Add(name);
                        }
                    }
                }

                if (entity.ordinal != null)
                {
                    try
                    {
                        var eventList = state.SummaryEvents;
                        var value = entity.ordinal[0];
                        var num = int.Parse(value.ToString());
                        if (eventList != null && num > 0)
                        {
                            var currentList = eventList.GetRange(0, Math.Min(CalendarSkillState.PageSize, eventList.Count));
                            if (num <= currentList.Count)
                            {
                                state.ReadOutEvents.Clear();
                                state.ReadOutEvents.Add(currentList[num - 1]);
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }

                if (entity.number != null && entity.ordinal != null && entity.ordinal.Length == 0)
                {
                    try
                    {
                        var eventList = state.SummaryEvents;
                        var value = entity.ordinal[0];
                        var num = int.Parse(value.ToString());
                        if (eventList != null && num > 0)
                        {
                            var currentList = eventList.GetRange(0, Math.Min(CalendarSkillState.PageSize, eventList.Count));
                            if (num <= currentList.Count)
                            {
                                state.ReadOutEvents.Clear();
                                state.ReadOutEvents.Add(currentList[num - 1]);
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }

                if (entity.StartDate != null)
                {
                    state.StartDateString = entity.StartDate[0];
                    foreach (var datetimeItem in entity.datetime)
                    {
                        if (datetimeItem.Type == "date")
                        {
                            var date = DateTime.Parse(entity.datetime[0].Expressions[0]);
                            state.StartDate = date;
                        }
                        else if (datetimeItem.Type == "datetime")
                        {
                            var date = ParseToDateTime(datetimeItem.Expressions[0]);
                            state.StartDate = date;
                            state.StartTime = date;
                        }
                        else if (entity.datetime[0].Type == "datetimerange")
                        {
                            var date = DateTime.Parse(datetimeItem.Expressions[0].Split("T")[0]);
                            state.StartDate = date;
                            ParseToTimeRange(datetimeItem.Expressions[0], out var startDateTime, out var endDateTime);
                            if (startDateTime != null)
                            {
                                state.StartTime = startDateTime;
                            }

                            if (endDateTime != null)
                            {
                                state.EndDateTime = endDateTime;
                            }
                        }
                    }
                }

                if (entity.StartTime != null)
                {
                    state.StartTimeString = entity.StartTime[0];
                    foreach (var datetimeItem in entity.datetime)
                    {
                        if (datetimeItem.Type == "time")
                        {
                            state.StartTime = ParseToTime(datetimeItem.Expressions[0]);
                            if (state.StartDate == null)
                            {
                                state.StartDate = DateTime.Now;
                            }
                        }
                        else if (datetimeItem.Type == "datetime")
                        {
                            state.StartTime = ParseToDateTime(datetimeItem.Expressions[0]);
                            if (state.StartDate == null)
                            {
                                state.StartDate = DateTime.Now;
                            }
                        }
                    }
                }

                if (entity.Duration != null)
                {
                    foreach (var datetimeItem in entity.datetime)
                    {
                        if (datetimeItem.Type == "duration")
                        {
                            TimeSpan ts = XmlConvert.ToTimeSpan(datetimeItem.Expressions[0]);
                            state.Duration = (int)ts.TotalSeconds;
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // put log here
            }
        }

        public async Task HandleDialogExceptions(WaterfallStepContext sc)
        {
            var state = await _accessor.GetAsync(sc.Context);
            state.Clear();
            await sc.CancelAllDialogsAsync();
        }
    }
}
