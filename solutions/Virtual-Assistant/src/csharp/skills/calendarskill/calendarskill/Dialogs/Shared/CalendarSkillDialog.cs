using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Common;
using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared.Prompts;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Dialogs.Shared.Resources.Strings;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Data;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Prompts;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Newtonsoft.Json.Linq;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Dialogs.Shared
{
    public class CalendarSkillDialog : ComponentDialog
    {
        // Constants
        private const string SkillModeAuth = "SkillAuth";

        public CalendarSkillDialog(
            string dialogId,
            SkillConfigurationBase services,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(dialogId)
        {
            Services = services;
            Accessor = accessor;
            ServiceManager = serviceManager;
            TelemetryClient = telemetryClient;

            if (!Services.AuthenticationConnections.Any())
            {
                throw new Exception("You must configure an authentication connection in your bot file before using this component.");
            }

            AddDialog(new EventPrompt(SkillModeAuth, "tokens/response", TokenResponseValidator));
            AddDialog(new MultiProviderAuthDialog(services));
            AddDialog(new TextPrompt(Actions.Prompt));
            AddDialog(new ConfirmPrompt(Actions.TakeFurtherAction, null, Culture.English) { Style = ListStyle.SuggestedAction });
            AddDialog(new DateTimePrompt(Actions.DateTimePrompt, DateTimeValidator, Culture.English));
            AddDialog(new DateTimePrompt(Actions.DateTimePromptForUpdateDelete, DateTimePromptValidator, Culture.English));
            AddDialog(new ChoicePrompt(Actions.Choice, ChoiceValidator, Culture.English) { Style = ListStyle.None, });
            AddDialog(new ChoicePrompt(Actions.EventChoice, null, Culture.English) { Style = ListStyle.Inline, ChoiceOptions = new ChoiceFactoryOptions { InlineSeparator = string.Empty, InlineOr = string.Empty, InlineOrMore = string.Empty, IncludeNumbers = false } });
            AddDialog(new TimePrompt(Actions.TimePrompt));
            AddDialog(new GetEventPrompt(Actions.GetEventPrompt));
        }

        protected SkillConfigurationBase Services { get; set; }

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

        protected async Task<DialogTurnResult> AfterGetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // When the user authenticates interactively we pass on the tokens/Response event which surfaces as a JObject
                // When the token is cached we get a TokenResponse object.
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                ProviderTokenResponse providerTokenResponse;
                if (skillOptions != null && skillOptions.SkillMode)
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

        protected async Task<List<EventModel>> GetEventsByTime(List<DateTime> startDateList, List<DateTime> startTimeList, List<DateTime> endDateList, List<DateTime> endTimeList, TimeZoneInfo userTimeZone, ICalendarService calendarService)
        {
            // todo: check input datetime is utc
            var rawEvents = new List<EventModel>();
            var resultEvents = new List<EventModel>();

            DateTime? startDate = null;
            if (startDateList.Any())
            {
                startDate = startDateList.Last();
            }

            DateTime? endDate = null;
            if (endDateList.Any())
            {
                endDate = endDateList.Last();
            }

            bool searchByStartTime = startTimeList.Any() && endDate == null && !endTimeList.Any();

            startDate = startDate ?? TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, userTimeZone);
            endDate = endDate ?? startDate ?? TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, userTimeZone);

            var searchStartTimeList = new List<DateTime>();
            var searchEndTimeList = new List<DateTime>();

            if (startTimeList.Any())
            {
                foreach (var time in startTimeList)
                {
                    searchStartTimeList.Add(TimeZoneInfo.ConvertTimeToUtc(
                        new DateTime(startDate.Value.Year, startDate.Value.Month, startDate.Value.Day, time.Hour, time.Minute, time.Second),
                        userTimeZone));
                }
            }
            else
            {
                searchStartTimeList.Add(TimeZoneInfo.ConvertTimeToUtc(
                    new DateTime(startDate.Value.Year, startDate.Value.Month, startDate.Value.Day), userTimeZone));
            }

            if (endTimeList.Any())
            {
                foreach (var time in endTimeList)
                {
                    searchEndTimeList.Add(TimeZoneInfo.ConvertTimeToUtc(
                        new DateTime(endDate.Value.Year, endDate.Value.Month, endDate.Value.Day, time.Hour, time.Minute, time.Second),
                        userTimeZone));
                }
            }
            else
            {
                searchEndTimeList.Add(TimeZoneInfo.ConvertTimeToUtc(
                    new DateTime(endDate.Value.Year, endDate.Value.Month, endDate.Value.Day, 23, 59, 59), userTimeZone));
            }

            DateTime? searchStartTime = null;

            if (searchByStartTime)
            {
                foreach (var startTime in searchStartTimeList)
                {
                    rawEvents = await calendarService.GetEventsByStartTime(startTime);
                    if (rawEvents.Any())
                    {
                        searchStartTime = startTime;
                        break;
                    }
                }
            }
            else
            {
                for (var i = 0; i < searchStartTimeList.Count(); i++)
                {
                    rawEvents = await calendarService.GetEventsByTime(
                        searchStartTimeList[i],
                        searchEndTimeList.Count() > i ? searchEndTimeList[i] : searchEndTimeList[0]);
                    if (rawEvents.Any())
                    {
                        searchStartTime = searchStartTimeList[i];
                        break;
                    }
                }
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

                if (!isBeginDialog)
                {
                    return;
                }

                switch (intent)
                {
                    case Calendar.Intent.FindMeetingRoom:
                    case Calendar.Intent.CreateCalendarEntry:
                        {
                            state.CreateHasDetail = false;
                            if (entity.Subject != null)
                            {
                                state.CreateHasDetail = true;
                                state.Title = GetSubjectFromEntity(entity);
                            }

                            if (entity.ContactName != null)
                            {
                                state.CreateHasDetail = true;
                                state.AttendeesNameList = GetAttendeesFromEntity(entity, luisResult.Text, state.AttendeesNameList);
                            }

                            if (entity.FromDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromDate[0]);
                                var date = GetTimeFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true);
                                if (date != null)
                                {
                                    state.CreateHasDetail = true;
                                    state.StartDate = date;
                                }

                                date = GetTimeFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false);
                                if (date != null)
                                {
                                    state.CreateHasDetail = true;
                                    state.EndDate = date;
                                }
                            }

                            if (entity.ToDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone());
                                if (date != null)
                                {
                                    state.CreateHasDetail = true;
                                    state.EndDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true);
                                if (time != null)
                                {
                                    state.CreateHasDetail = true;
                                    state.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false);
                                if (time != null)
                                {
                                    state.CreateHasDetail = true;
                                    state.EndTime = time;
                                }
                            }

                            if (entity.ToTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone());
                                if (time != null)
                                {
                                    state.CreateHasDetail = true;
                                    state.EndTime = time;
                                }
                            }

                            if (entity.Duration != null)
                            {
                                int duration = GetDurationFromEntity(entity, dc.Context.Activity.Locale);
                                if (duration != -1)
                                {
                                    state.CreateHasDetail = true;
                                    state.Duration = duration;
                                }
                            }

                            if (entity.MeetingRoom != null)
                            {
                                state.CreateHasDetail = true;
                                state.Location = GetMeetingRoomFromEntity(entity);
                            }

                            if (entity.Location != null)
                            {
                                state.CreateHasDetail = true;
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
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromDate[0]);
                                var date = GetTimeFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true);
                                if (date != null)
                                {
                                    state.StartDate = date;
                                }

                                date = GetTimeFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false);
                                if (date != null)
                                {
                                    state.EndDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true);
                                if (time != null)
                                {
                                    state.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false);
                                if (time != null)
                                {
                                    state.EndTime = time;
                                }
                            }

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
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromDate[0]);
                                var date = GetTimeFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true);
                                if (date != null)
                                {
                                    state.OriginalStartDate = date;
                                }

                                date = GetTimeFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false);
                                if (date != null)
                                {
                                    state.OriginalEndDate = date;
                                }
                            }

                            if (entity.ToDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone());
                                if (date != null)
                                {
                                    state.StartDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true);
                                if (time != null)
                                {
                                    state.OriginalStartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false);
                                if (time != null)
                                {
                                    state.OriginalEndTime = time;
                                }
                            }

                            if (entity.ToTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true);
                                if (time != null)
                                {
                                    state.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false);
                                if (time != null)
                                {
                                    state.EndTime = time;
                                }
                            }

                            if (entity.MoveEarlierTimeSpan != null)
                            {
                                state.MoveTimeSpan = GetMoveTimeSpanFromEntity(entity.MoveEarlierTimeSpan[0], dc.Context.Activity.Locale, false);
                            }

                            if (entity.MoveLaterTimeSpan != null)
                            {
                                state.MoveTimeSpan = GetMoveTimeSpanFromEntity(entity.MoveLaterTimeSpan[0], dc.Context.Activity.Locale, true);
                            }

                            if (entity.datetime != null)
                            {
                                var match = entity._instance.datetime.ToList().Find(w => w.Text.ToLower() == CalendarCommonStrings.DailyToken
                                || w.Text.ToLower() == CalendarCommonStrings.WeeklyToken
                                || w.Text.ToLower() == CalendarCommonStrings.MonthlyToken);
                                if (match != null)
                                {
                                    state.RecurrencePattern = match.Text.ToLower();
                                }
                            }

                            break;
                        }

                    case Calendar.Intent.FindCalendarEntry:
                        {
                            if (entity.OrderReference != null)
                            {
                                state.OrderReference = GetOrderReferenceFromEntity(entity);
                            }

                            if (entity.FromDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromDate[0]);
                                var date = GetTimeFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true);
                                if (date != null)
                                {
                                    state.StartDate = date;
                                    state.StartDateString = dateString;
                                }

                                date = GetTimeFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false);
                                if (date != null)
                                {
                                    state.EndDate = date;
                                }
                            }

                            if (entity.ToDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone());
                                if (date != null)
                                {
                                    state.EndDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true);
                                if (time != null)
                                {
                                    state.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false);
                                if (time != null)
                                {
                                    state.EndTime = time;
                                }
                            }

                            if (entity.ToTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone());
                                if (time != null)
                                {
                                    state.EndTime = time;
                                }
                            }

                            if (entity.AskParameter != null)
                            {
                                state.AskParameterContent = GetAskParameterFromEntity(entity);
                            }

                            break;
                        }

                    case Calendar.Intent.ConnectToMeeting:
                    case Calendar.Intent.TimeRemaining:
                        {
                            if (entity.FromDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone());
                                if (date != null)
                                {
                                    state.StartDate = date;
                                }
                            }

                            if (entity.ToDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone());
                                if (date != null)
                                {
                                    state.EndDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true);
                                if (time != null)
                                {
                                    state.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false);
                                if (time != null)
                                {
                                    state.EndTime = time;
                                }
                            }

                            if (entity.ToTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone());
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

        // This method is called by any waterfall step that throws an exception to ensure consistency
        protected async Task HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace);

            // log exception
            TelemetryClient.TrackExceptionEx(ex, sc.Context.Activity, sc.ActiveDialog?.Id);

            // send error message to bot user
            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarSharedResponses.CalendarErrorMessage));

            // clear state
            var state = await Accessor.GetAsync(sc.Context);
            state.Clear();
            await sc.CancelAllDialogsAsync();

            return;
        }

        // This method is called by any waterfall step that throws a SkillException to ensure consistency
        protected async Task HandleDialogExceptions(WaterfallStepContext sc, SkillException ex)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace);

            // log exception
            TelemetryClient.TrackExceptionEx(ex, sc.Context.Activity, sc.ActiveDialog?.Id);

            // send error message to bot user
            if (ex.ExceptionType == SkillExceptionType.APIAccessDenied)
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarSharedResponses.CalendarErrorMessageBotProblem));
            }
            else
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarSharedResponses.CalendarErrorMessage));
            }

            // clear state
            var state = await Accessor.GetAsync(sc.Context);
            state.Clear();
        }

        // This method is called by any waterfall step that throws a SkillException to ensure consistency
        protected async Task HandleExpectedDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace);

            // log exception
            TelemetryClient.TrackExceptionEx(ex, sc.Context.Activity, sc.ActiveDialog?.Id);
        }

        protected override Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            var resultString = result?.ToString();
            if (!string.IsNullOrWhiteSpace(resultString) && resultString.Equals(CommonUtil.DialogTurnResultCancelAllDialogs, StringComparison.InvariantCultureIgnoreCase))
            {
                return outerDc.CancelAllDialogsAsync();
            }
            else
            {
                return base.EndComponentAsync(outerDc, result, cancellationToken);
            }
        }

        protected string GetSubjectFromEntity(Calendar._Entities entity)
        {
            return entity.Subject[0];
        }

        private string GetAskParameterFromEntity(Calendar._Entities entity)
        {
            return entity.AskParameter[0];
        }

        protected List<string> GetAttendeesFromEntity(Calendar._Entities entity, string inputString, List<string> attendees = null)
        {
            if (attendees == null)
            {
                attendees = new List<string>();
            }

            // As luis result for email address often contains extra spaces for word breaking
            // (e.g. send email to test@test.com, email address entity will be test @ test . com)
            // So use original user input as email address.
            var rawEntity = entity._instance.ContactName;
            foreach (var name in rawEntity)
            {
                var contactName = inputString.Substring(name.StartIndex, name.EndIndex - name.StartIndex);
                if (!attendees.Contains(contactName))
                {
                    attendees.Add(contactName);
                }
            }

            return attendees;
        }

        private int GetDurationFromEntity(Calendar._Entities entity, string local)
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

            return -1;
        }

        private int GetMoveTimeSpanFromEntity(string timeSpan, string local, bool later)
        {
            var culture = local ?? English;
            List<DateTimeResolution> result = RecognizeDateTime(timeSpan, culture);
            if (result != null)
            {
                if (result[0].Value != null)
                {
                    if (later)
                    {
                        return int.Parse(result[0].Value);
                    }
                    else
                    {
                        return -int.Parse(result[0].Value);
                    }
                }
            }

            return 0;
        }

        private string GetMeetingRoomFromEntity(Calendar._Entities entity)
        {
            return entity.MeetingRoom[0];
        }

        private string GetLocationFromEntity(Calendar._Entities entity)
        {
            return entity.Location[0];
        }

        private string GetDateTimeStringFromInstanceData(string inputString, InstanceData data)
        {
            return inputString.Substring(data.StartIndex, data.EndIndex - data.StartIndex);
        }

        private List<DateTime> GetDateFromDateTimeString(string date, string local, TimeZoneInfo userTimeZone)
        {
            var culture = local ?? English;
            List<DateTimeResolution> results = RecognizeDateTime(date, culture);
            var dateTimeResults = new List<DateTime>();
            if (results != null)
            {
                foreach (var result in results)
                {
                    var dateTime = DateTime.Parse(result.Value);
                    var dateTimeConvertType = result.Timex;

                    if (dateTime != null)
                    {
                        bool isRelativeTime = IsRelativeTime(date, result.Value, result.Timex);
                        dateTimeResults.Add(isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, userTimeZone) : dateTime);
                    }
                }
            }

            return dateTimeResults;
        }

        private List<DateTime> GetTimeFromDateTimeString(string time, string local, TimeZoneInfo userTimeZone, bool isStart = true)
        {
            var culture = local ?? English;
            List<DateTimeResolution> results = RecognizeDateTime(time, culture);
            var dateTimeResults = new List<DateTime>();
            if (results != null)
            {
                foreach (var result in results)
                {
                    if (result.Value != null)
                    {
                        if (!isStart)
                        {
                            break;
                        }

                        var dateTime = DateTime.Parse(result.Value);
                        var dateTimeConvertType = result.Timex;

                        if (dateTime != null)
                        {
                            bool isRelativeTime = IsRelativeTime(time, result.Value, result.Timex);
                            dateTimeResults.Add(isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, userTimeZone) : dateTime);
                        }
                    }
                    else
                    {
                        var startTime = DateTime.Parse(result.Start);
                        var endTime = DateTime.Parse(result.End);
                        if (isStart)
                        {
                            dateTimeResults.Add(startTime);
                        }
                        else
                        {
                            dateTimeResults.Add(endTime);
                        }
                    }
                }
            }

            return dateTimeResults;
        }

        private string GetOrderReferenceFromEntity(Calendar._Entities entity)
        {
            return entity.OrderReference[0];
        }

        /// <summary>
        /// implement the basic validation. Advanced validation done in upper level dialogs.
        /// </summary>
        /// <param name="prompt">datetime prompt.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>validation result.</returns>
        private Task<bool> DateTimeValidator(PromptValidatorContext<IList<DateTimeResolution>> prompt, CancellationToken cancellationToken)
        {
            if (prompt.Recognized.Succeeded)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }
    }
}