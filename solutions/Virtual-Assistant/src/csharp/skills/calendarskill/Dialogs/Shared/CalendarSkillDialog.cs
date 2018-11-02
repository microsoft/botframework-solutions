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
using Microsoft.Recognizers.Text.DateTime;
using static Microsoft.Recognizers.Text.Culture;
using CalendarSkill.Common;

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
                    return await sc.PromptAsync(LocalModeAuth, new PromptOptions() { RetryPrompt = sc.Context.Activity.CreateReply(CalendarSharedResponses.NoAuth, _responseBuilder), });
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
            var activity = promptContext.Recognized.Value;
            if (activity != null)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public async Task<bool> ChoiceValidator(PromptValidatorContext<FoundChoice> pc, CancellationToken cancellationToken)
        {

            var state = await _accessor.GetAsync(pc.Context);
            var luisResult = state.GeneralLuisResult;
            var topIntent = luisResult?.TopIntent().intent;

            // TODO: The signature for validators has changed to return bool -- Need new way to handle this logic
            // If user want to show more recipient end current choice dialog and return the intent to next step.
            if (topIntent == Luis.General.Intent.Next || topIntent == Luis.General.Intent.Previous)
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
            var state = await _accessor.GetAsync(dc.Context);
            foreach (var item in events)
            {
                var meetingCard = item.ToAdaptiveCardData(state.GetUserTimeZone(), showDate);
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

        public static async Task<List<EventModel>> GetEventsByTime(DateTime? startDate, DateTime? startTime, DateTime? endDate, DateTime? endTime, TimeZoneInfo userTimeZone, ICalendar calendarService)
        {
            // todo: check input datetime is utc
            var rawEvents = new List<EventModel>();
            var resultEvents = new List<EventModel>();

            bool searchByStartTime = startTime != null && endDate == null && endTime == null;

            startDate = startDate ?? TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, userTimeZone);
            endDate = endDate ?? startDate ?? TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, userTimeZone);

            var searchStartTime = startTime == null ? new DateTime(startDate.Value.Year, startDate.Value.Month, startDate.Value.Day) :
                new DateTime(startDate.Value.Year, startDate.Value.Month, startDate.Value.Day, startTime.Value.Hour, startTime.Value.Minute, startTime.Value.Second);
            searchStartTime = TimeZoneInfo.ConvertTimeToUtc(searchStartTime, userTimeZone);
            var searchEndTime = endTime == null ? new DateTime(endDate.Value.Year, endDate.Value.Month, endDate.Value.Day, 23, 59, 59) :
                new DateTime(endDate.Value.Year, endDate.Value.Month, endDate.Value.Day, endTime.Value.Hour, endTime.Value.Minute, endTime.Value.Second);
            searchEndTime = TimeZoneInfo.ConvertTimeToUtc(searchEndTime, userTimeZone);

            if (searchByStartTime)
            {
                rawEvents = await calendarService.GetEventsByStartTime(searchStartTime);
            }
            else
            {
                rawEvents = await calendarService.GetEventsByTime(searchStartTime, searchEndTime);
            }

            foreach (var item in rawEvents)
            {
                if (item.StartTime >= searchStartTime && item.IsCancelled != true)
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

                if (entity.Duration != null)
                {
                    foreach (var datetimeItem in entity.datetime)
                    {
                        if (datetimeItem.Type == "duration")
                        {
                            var culture = dc.Context.Activity.Locale ?? English;
                            List<DateTimeResolution> result = RecognizeDateTime(entity.Duration[0], culture);
                            if (result != null)
                            {
                                if (result[0].Value != null)
                                {
                                    state.Duration = int.Parse(result[0].Value);
                                }
                            }

                            break;
                        }
                    }
                }

                if (entity.MeetingRoom != null)
                {
                    state.Location = entity.MeetingRoom[0];
                }

                if (entity.Location != null)
                {
                    state.Location = entity.Location[0];
                }

                if (entity.StartDate != null)
                {
                    var culture = dc.Context.Activity.Locale ?? English;
                    List<DateTimeResolution> results = RecognizeDateTime(entity.StartDate[0], culture);
                    if (results != null)
                    {
                        var result = results[results.Count - 1];
                        if (result.Value != null)
                        {
                            var dateTime = DateTime.Parse(result.Value);
                            var dateTimeConvertType = result.Timex;

                            if (dateTime != null)
                            {
                                bool isRelativeTime = IsRelativeTime(entity.StartDate[0], result.Value, result.Timex);
                                state.StartDate = isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : dateTime;
                            }
                        }
                    }
                }

                if (entity.StartTime != null)
                {
                    var culture = dc.Context.Activity.Locale ?? English;
                    List<DateTimeResolution> result = RecognizeDateTime(entity.StartTime[0], culture);
                    if (result != null)
                    {
                        if (result[0].Value != null)
                        {
                            var dateTime = DateTime.Parse(result[0].Value);
                            var dateTimeConvertType = result[0].Timex;

                            if (dateTime != null)
                            {
                                bool isRelativeTime = IsRelativeTime(entity.StartTime[0], result[0].Value, result[0].Timex);
                                state.StartTime = isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : dateTime;
                            }
                        }
                        else
                        {
                            var startTime = DateTime.Parse(result[0].Start);
                            var endTime = DateTime.Parse(result[0].End);
                            state.StartTime = startTime;
                            state.EndTime = endTime;
                        }
                    }
                }

                if (entity.EndDate != null)
                {
                    var culture = dc.Context.Activity.Locale ?? English;
                    List<DateTimeResolution> results = RecognizeDateTime(entity.EndDate[0], culture);
                    if (results != null)
                    {
                        var result = results[results.Count - 1];
                        if (result.Value != null)
                        {
                            var dateTime = DateTime.Parse(result.Value);
                            var dateTimeConvertType = result.Timex;

                            if (dateTime != null)
                            {
                                bool isRelativeTime = IsRelativeTime(entity.EndDate[0], result.Value, result.Timex);
                                state.EndDate = isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : dateTime;
                            }
                        }
                    }
                }

                if (entity.EndTime != null)
                {
                    var culture = dc.Context.Activity.Locale ?? English;
                    List<DateTimeResolution> result = RecognizeDateTime(entity.EndTime[0], culture);
                    if (result != null && result[0].Value != null)
                    {
                        var dateTime = DateTime.Parse(result[0].Value);
                        var dateTimeConvertType = result[0].Timex;

                        if (dateTime != null)
                        {
                            bool isRelativeTime = IsRelativeTime(entity.EndTime[0], result[0].Value, result[0].Timex);
                            state.EndTime = isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : dateTime;
                        }
                    }
                }

                if (entity.OriginalStartDate != null)
                {
                    var culture = dc.Context.Activity.Locale ?? English;
                    List<DateTimeResolution> results = RecognizeDateTime(entity.OriginalStartDate[0], culture);
                    if (results != null)
                    {
                        var result = results[results.Count - 1];
                        if (result.Value != null)
                        {
                            var dateTime = DateTime.Parse(result.Value);
                            var dateTimeConvertType = result.Timex;

                            if (dateTime != null)
                            {
                                bool isRelativeTime = IsRelativeTime(entity.OriginalStartDate[0], result.Value, result.Timex);
                                state.OriginalStartDate = isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : dateTime;
                            }
                        }
                    }
                }

                if (entity.OriginalStartTime != null)
                {
                    var culture = dc.Context.Activity.Locale ?? English;
                    List<DateTimeResolution> result = RecognizeDateTime(entity.OriginalStartTime[0], culture);
                    if (result != null)
                    {
                        if (result[0].Value != null)
                        {
                            var dateTime = DateTime.Parse(result[0].Value);
                            var dateTimeConvertType = result[0].Timex;

                            if (dateTime != null)
                            {
                                bool isRelativeTime = IsRelativeTime(entity.OriginalStartTime[0], result[0].Value, result[0].Timex);
                                state.OriginalStartTime = isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : dateTime;
                            }
                        }
                        else
                        {
                            var startTime = DateTime.Parse(result[0].Start);
                            var endTime = DateTime.Parse(result[0].End);
                            state.OriginalStartTime = startTime;
                            state.OriginalEndTime = endTime;
                        }
                    }
                }

                if (entity.OriginalEndDate != null)
                {
                    var culture = dc.Context.Activity.Locale ?? English;
                    List<DateTimeResolution> results = RecognizeDateTime(entity.OriginalEndDate[0], culture);
                    if (results != null)
                    {
                        var result = results[results.Count - 1];
                        if (result.Value != null)
                        {
                            var dateTime = DateTime.Parse(result.Value);
                            var dateTimeConvertType = result.Timex;

                            if (dateTime != null)
                            {
                                bool isRelativeTime = IsRelativeTime(entity.OriginalEndDate[0], result.Value, result.Timex);
                                state.OriginalEndDate = isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : dateTime;
                            }
                        }
                    }
                }

                if (entity.OriginalEndTime != null)
                {
                    var culture = dc.Context.Activity.Locale ?? English;
                    List<DateTimeResolution> result = RecognizeDateTime(entity.OriginalEndTime[0], culture);
                    if (result != null && result[0].Value != null)
                    {
                        var dateTime = DateTime.Parse(result[0].Value);
                        var dateTimeConvertType = result[0].Timex;

                        if (dateTime != null)
                        {
                            bool isRelativeTime = IsRelativeTime(entity.OriginalEndTime[0], result[0].Value, result[0].Timex);
                            state.OriginalEndTime = isRelativeTime ? TimeZoneInfo.ConvertTimeToUtc(dateTime, TimeZoneInfo.Local) :
                                TimeConverter.ConvertLuisLocalToUtc(dateTime, state.GetUserTimeZone());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // put log here
            }
        }

        private List<DateTimeResolution> RecognizeDateTime(string dateTimeString, string culture)
        {
            var results = DateTimeRecognizer.RecognizeDateTime(dateTimeString, culture);
            if (results.Count > 0)
            {
                // Return list of resolutions from first match
                var result = new List<DateTimeResolution>();
                var values = (List<Dictionary<string, string>>)results[0].Resolution["values"];
                foreach (var value in values)
                {
                    result.Add(ReadResolution(value));
                }

                return result;
            }

            return null;
        }

        private DateTimeResolution ReadResolution(IDictionary<string, string> resolution)
        {
            var result = new DateTimeResolution();

            if (resolution.TryGetValue("timex", out var timex))
            {
                result.Timex = timex;
            }

            if (resolution.TryGetValue("value", out var value))
            {
                result.Value = value;
            }

            if (resolution.TryGetValue("start", out var start))
            {
                result.Start = start;
            }

            if (resolution.TryGetValue("end", out var end))
            {
                result.End = end;
            }

            return result;
        }

        public async Task HandleDialogExceptions(WaterfallStepContext sc)
        {
            var state = await _accessor.GetAsync(sc.Context);
            state.Clear();
            await sc.CancelAllDialogsAsync();
        }
    }
}
