using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CalendarSkill.Common;
using CalendarSkill.Dialogs.Main.Resources;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Newtonsoft.Json.Linq;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill
{
    public class CalendarSkillDialog : ComponentDialog
    {
        // Constants
        public const string SkillModeAuth = "SkillAuth";
        public const string LocalModeAuth = "LocalAuth";

        public CalendarSkillDialog(
            string dialogId,
            SkillConfiguration services,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager)
            : base(dialogId)
        {
            Services = services;
            Accessor = accessor;
            ServiceManager = serviceManager;

            if (!Services.AuthenticationConnections.Any())
            {
                throw new Exception("You must configure an authentication connection in your bot file before using this component.");
            }

            foreach (var connection in services.AuthenticationConnections)
            {
                AddDialog(new OAuthPrompt(
                    connection.Key,
                    new OAuthPromptSettings
                    {
                        ConnectionName = connection.Value,
                        Text = $"Please login with your {connection.Key} account.",
                        Timeout = 30000,
                    },
                    AuthPromptValidator));
            }

            AddDialog(new EventPrompt(SkillModeAuth, "tokens/response", TokenResponseValidator));
            AddDialog(new MultiProviderAuthDialog(services));
            AddDialog(new TextPrompt(Actions.Prompt));
            AddDialog(new ConfirmPrompt(Actions.TakeFurtherAction, null, Culture.English) { Style = ListStyle.SuggestedAction });
            AddDialog(new DateTimePrompt(Actions.DateTimePrompt, null, Culture.English));
            AddDialog(new DateTimePrompt(Actions.DateTimePromptForUpdateDelete, DateTimePromptValidator, Culture.English));
            AddDialog(new ChoicePrompt(Actions.Choice, ChoiceValidator, Culture.English) { Style = ListStyle.None, });
            AddDialog(new ChoicePrompt(Actions.EventChoice, null, Culture.English) { Style = ListStyle.Inline, ChoiceOptions = new ChoiceFactoryOptions { InlineSeparator = string.Empty, InlineOr = string.Empty, InlineOrMore = string.Empty, IncludeNumbers = false } });
        }

        protected SkillConfiguration Services { get; set; }

        protected IStatePropertyAccessor<CalendarSkillState> Accessor { get; set; }

        protected IServiceManager ServiceManager { get; set; }

        protected CalendarSkillResponseBuilder ResponseBuilder { get; set; } = new CalendarSkillResponseBuilder();

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(dc.Context);
            await DigestCalendarLuisResult(dc, state.LuisResult, true);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(dc.Context);
            await DigestCalendarLuisResult(dc, state.LuisResult, false);
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        // Shared steps
        protected async Task<DialogTurnResult> GetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken)
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
                    return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions());
                }
            }
            catch
            {
                // await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SharedResponses.AuthFailed));
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        protected async Task<DialogTurnResult> AfterGetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // When the user authenticates interactively we pass on the tokens/Response event which surfaces as a JObject
                // When the token is cached we get a TokenResponse object.
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                ProviderTokenResponse providerTokenResponse;
                if (skillOptions.SkillMode)
                {
                    var resultType = sc.Context.Activity.Value.GetType();
                    if (resultType == typeof(ProviderTokenResponse))
                    {
                        providerTokenResponse = sc.Context.Activity.Value as ProviderTokenResponse;
                    }
                    else
                    {
                        var tokenResponseObject = sc.Context.Activity.Value as JObject;
                        providerTokenResponse = tokenResponseObject?.ToObject<ProviderTokenResponse>();
                    }
                }
                else
                {
                    providerTokenResponse = sc.Result as ProviderTokenResponse;
                }

                if (providerTokenResponse != null)
                {
                    var state = await Accessor.GetAsync(sc.Context);
                    state.APIToken = providerTokenResponse.TokenResponse.Token;

                    var provider = providerTokenResponse.AuthenticationProvider;

                    if (provider == OAuthProvider.AzureAD)
                    {
                        state.EventSource = EventSource.Microsoft;
                    }
                    else if (provider == OAuthProvider.Google)
                    {
                        state.EventSource = EventSource.Google;
                    }
                    else
                    {
                        throw new Exception($"The authentication provider \"{provider.ToString()}\" is not support by the Calendar Skill.");
                    }
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
        protected Task<bool> TokenResponseValidator(PromptValidatorContext<Activity> pc, CancellationToken cancellationToken)
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

        protected Task<bool> AuthPromptValidator(PromptValidatorContext<TokenResponse> promptContext, CancellationToken cancellationToken)
        {
            var token = promptContext.Recognized.Value;
            if (token != null)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        protected async Task<bool> ChoiceValidator(PromptValidatorContext<FoundChoice> pc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(pc.Context);
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

        protected Task<bool> DateTimePromptValidator(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        // Helpers
        protected async Task ShowMeetingList(DialogContext dc, List<EventModel> events, bool showDate = true)
        {
            var replyToConversation = dc.Context.Activity.CreateReply();
            replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            replyToConversation.Attachments = new List<Microsoft.Bot.Schema.Attachment>();

            var cardsData = new List<CalendarCardData>();
            var state = await Accessor.GetAsync(dc.Context);
            foreach (var item in events)
            {
                var meetingCard = item.ToAdaptiveCardData(state.GetUserTimeZone(), showDate);
                var replyTemp = dc.Context.Activity.CreateAdaptiveCardReply(CalendarMainResponses.GreetingMessage, item.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", meetingCard);
                replyToConversation.Attachments.Add(replyTemp.Attachments[0]);
            }

            await dc.Context.SendActivityAsync(replyToConversation);
        }

        protected bool IsRelativeTime(string userInput, string resolverResult, string timex)
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

        protected async Task<List<EventModel>> GetEventsByTime(DateTime? startDate, DateTime? startTime, DateTime? endDate, DateTime? endTime, TimeZoneInfo userTimeZone, ICalendar calendarService)
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

        protected bool ContainsTime(string timex)
        {
            return timex.Contains("T");
        }

        protected async Task DigestCalendarLuisResult(DialogContext dc, Calendar luisResult, bool isBeginDialog)
        {
            try
            {
                var state = await Accessor.GetAsync(dc.Context);

                var intent = luisResult.TopIntent().intent;

                var entity = luisResult.Entities;

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

                if (!isBeginDialog)
                {
                    return;
                }

                switch (intent)
                {
                    case Calendar.Intent.FindMeetingRoom:
                    case Calendar.Intent.CreateCalendarEntry:
                        {
                            if (entity.Subject != null)
                            {
                                state.Title = GetSubjectFromEntity(entity);
                            }

                            if (entity.ContactName != null)
                            {
                                state.AttendeesNameList = GetAttendeesFromEntity(entity, state.AttendeesNameList);
                            }

                            if (entity.FromDate != null)
                            {
                                var date = GetDateFromDateTimeString(entity.FromDate[0], dc.Context.Activity.Locale, state.GetUserTimeZone());
                                if (date != null)
                                {
                                    state.StartDate = date;
                                }
                            }


                            if (entity.ToDate != null)
                            {
                                var date = GetDateFromDateTimeString(entity.ToDate[0], dc.Context.Activity.Locale, state.GetUserTimeZone());
                                if (date != null)
                                {
                                    state.EndDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var time = GetTimeFromDateTimeString(entity.FromTime[0], dc.Context.Activity.Locale, state.GetUserTimeZone(), true);
                                if (time != null)
                                {
                                    state.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(entity.FromTime[0], dc.Context.Activity.Locale, state.GetUserTimeZone(), false);
                                if (time != null)
                                {
                                    state.EndTime = time;
                                }
                            }

                            if (entity.ToTime != null)
                            {
                                var time = GetTimeFromDateTimeString(entity.ToTime[0], dc.Context.Activity.Locale, state.GetUserTimeZone());
                                if (time != null)
                                {
                                    state.EndTime = time;
                                }
                            }

                            if (entity.Duration != null)
                            {
                                int duration = GetDurationFromEntity(entity, dc.Context.Activity.Locale);
                                if (duration != -1)
                                {
                                    state.Duration = duration;
                                }
                            }

                            if (entity.MeetingRoom != null)
                            {
                                state.Location = GetMeetingRoomFromEntity(entity);
                            }

                            if (entity.Location != null)
                            {
                                state.Location = GetLocationFromEntity(entity);
                            }

                            break;
                        }

                    case Calendar.Intent.DeleteCalendarEntry:
                        {
                            if (entity.Subject != null)
                            {
                                state.Title = GetSubjectFromEntity(entity);
                            }


                            if (entity.FromDate != null)
                            {
                                var date = GetDateFromDateTimeString(entity.FromDate[0], dc.Context.Activity.Locale, state.GetUserTimeZone());
                                if (date != null)
                                {
                                    state.StartDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var time = GetTimeFromDateTimeString(entity.FromTime[0], dc.Context.Activity.Locale, state.GetUserTimeZone(), true);
                                if (time != null)
                                {
                                    state.StartTime = time;
                                }
                            }

                            break;
                        }

                    case Calendar.Intent.NextMeeting:
                        {
                            break;
                        }

                    case Calendar.Intent.ChangeCalendarEntry:
                        {
                            if (entity.Subject != null)
                            {
                                state.Title = GetSubjectFromEntity(entity);
                            }


                            if (entity.FromDate != null)
                            {
                                var date = GetDateFromDateTimeString(entity.FromDate[0], dc.Context.Activity.Locale, state.GetUserTimeZone());
                                if (date != null)
                                {
                                    state.OriginalStartDate = date;
                                }
                            }


                            if (entity.ToDate != null)
                            {
                                var date = GetDateFromDateTimeString(entity.ToDate[0], dc.Context.Activity.Locale, state.GetUserTimeZone());
                                if (date != null)
                                {
                                    state.StartDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var time = GetTimeFromDateTimeString(entity.FromTime[0], dc.Context.Activity.Locale, state.GetUserTimeZone(), true);
                                if (time != null)
                                {
                                    state.OriginalStartTime = time;
                                }

                                time = GetTimeFromDateTimeString(entity.FromTime[0], dc.Context.Activity.Locale, state.GetUserTimeZone(), false);
                                if (time != null)
                                {
                                    state.OriginalEndTime = time;
                                }
                            }

                            if (entity.ToTime != null)
                            {
                                var time = GetTimeFromDateTimeString(entity.ToTime[0], dc.Context.Activity.Locale, state.GetUserTimeZone(), true);
                                if (time != null)
                                {
                                    state.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(entity.ToTime[0], dc.Context.Activity.Locale, state.GetUserTimeZone(), false);
                                if (time != null)
                                {
                                    state.EndTime = time;
                                }
                            }

                            break;
                        }

                    case Calendar.Intent.FindCalendarEntry:
                    case Calendar.Intent.Summary:
                        {
                            break;
                        }

                    case Calendar.Intent.None:
                        {
                            break;
                        }

                    default:
                        {
                            break;
                        }
                }
            }
            catch
            {
                var state = await Accessor.GetAsync(dc.Context);
                state.Clear();
                await dc.CancelAllDialogsAsync();
                throw;
            }
        }

        private string GetSubjectFromEntity(Calendar._Entities entity)
        {
            return entity.Subject[0];
        }

        private List<string> GetAttendeesFromEntity(Calendar._Entities entity, List<string> attendees = null)
        {
            if (attendees == null)
            {
                attendees = new List<string>();
            }

            foreach (var name in entity.ContactName)
            {
                if (!attendees.Contains(name))
                {
                    attendees.Add(name);
                }
            }

            return attendees;
        }

        private int GetDurationFromEntity(Calendar._Entities entity, string local)
        {
            foreach (var datetimeItem in entity.datetime)
            {
                if (datetimeItem.Type == "duration")
                {
                    var culture = local ?? English;
                    List<DateTimeResolution> result = RecognizeDateTime(entity.Duration[0], culture);
                    if (result != null)
                    {
                        if (result[0].Value != null)
                        {
                            return int.Parse(result[0].Value);
                        }
                    }

                    break;
                }
            }

            return -1;
        }

        private string GetMeetingRoomFromEntity(Calendar._Entities entity)
        {
            return entity.MeetingRoom[0];
        }

        private string GetLocationFromEntity(Calendar._Entities entity)
        {
            return entity.Location[0];
        }

        private DateTime? GetDateFromDateTimeString(string date, string local, TimeZoneInfo userTimeZone)
        {
            var culture = local ?? English;
            List<DateTimeResolution> results = RecognizeDateTime(date, culture);
            if (results != null)
            {
                var result = results[results.Count - 1];
                if (result.Value != null)
                {
                    var dateTime = DateTime.Parse(result.Value);
                    var dateTimeConvertType = result.Timex;

                    if (dateTime != null)
                    {
                        bool isRelativeTime = IsRelativeTime(date, result.Value, result.Timex);
                        return isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, userTimeZone) : dateTime;
                    }
                }
            }

            return null;
        }

        private DateTime? GetTimeFromDateTimeString(string time, string local, TimeZoneInfo userTimeZone, bool isStart = true)
        {
            var culture = local ?? English;
            List<DateTimeResolution> result = RecognizeDateTime(time, culture);
            if (result != null)
            {
                if (result[0].Value != null)
                {
                    if (!isStart)
                    {
                        return null;
                    }

                    var dateTime = DateTime.Parse(result[0].Value);
                    var dateTimeConvertType = result[0].Timex;

                    if (dateTime != null)
                    {
                        bool isRelativeTime = IsRelativeTime(time, result[0].Value, result[0].Timex);
                        return isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, userTimeZone) : dateTime;
                    }
                }
                else
                {
                    var startTime = DateTime.Parse(result[0].Start);
                    var endTime = DateTime.Parse(result[0].End);
                    if (isStart)
                    {
                        return startTime;
                    }

                    return endTime;
                }
            }

            return null;
        }

        protected List<DateTimeResolution> RecognizeDateTime(string dateTimeString, string culture)
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

        protected DateTimeResolution ReadResolution(IDictionary<string, string> resolution)
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

        protected async Task HandleDialogExceptions(WaterfallStepContext sc)
        {
            var state = await Accessor.GetAsync(sc.Context);
            state.Clear();
            await sc.CancelAllDialogsAsync();
        }
    }
}
