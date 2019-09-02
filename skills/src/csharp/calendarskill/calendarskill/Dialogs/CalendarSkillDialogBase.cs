using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Prompts;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.Summary;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Microsoft.Recognizers.Text.DateTime;
using static Microsoft.Recognizers.Text.Culture;
using Constants = Microsoft.Recognizers.Text.DataTypes.TimexExpression.Constants;

namespace CalendarSkill.Dialogs
{
    public class CalendarSkillDialogBase : ComponentDialog
    {
        private ConversationState _conversationState;

        public CalendarSkillDialogBase(
            string dialogId,
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(dialogId)
        {
            Settings = settings;
            Services = services;
            ResponseManager = responseManager;
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            Accessor = _conversationState.CreateProperty<CalendarSkillState>(nameof(CalendarSkillState));
            ServiceManager = serviceManager;
            TelemetryClient = telemetryClient;

            AddDialog(new MultiProviderAuthDialog(settings.OAuthConnections, appCredentials));
            AddDialog(new TextPrompt(Actions.Prompt));
            AddDialog(new ConfirmPrompt(Actions.TakeFurtherAction, null, Culture.English) { Style = ListStyle.SuggestedAction });
            AddDialog(new ChoicePrompt(Actions.Choice, ChoiceValidator, Culture.English) { Style = ListStyle.None, });
            AddDialog(new TimePrompt(Actions.TimePrompt));
            AddDialog(new GetEventPrompt(Actions.GetEventPrompt));
        }

        protected BotSettings Settings { get; set; }

        protected BotServices Services { get; set; }

        protected IStatePropertyAccessor<CalendarSkillState> Accessor { get; set; }

        protected IServiceManager ServiceManager { get; set; }

        protected ResponseManager ResponseManager { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(dc.Context);

            // find contact dialog is not a start dialog, should not run luis part.
            if (state.LuisResult != null && Id != nameof(FindContactDialog))
            {
                await DigestCalendarLuisResult(dc, state.LuisResult, state.GeneralLuisResult, true);
            }

            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(dc.Context);
            if (state.LuisResult != null)
            {
                await DigestCalendarLuisResult(dc, state.LuisResult, state.GeneralLuisResult, false);
            }

            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        // Shared steps
        protected async Task<DialogTurnResult> GetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions());
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
                if (sc.Result is ProviderTokenResponse providerTokenResponse)
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

        protected async Task<DialogTurnResult> SearchEventsWithEntities(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);

                // search by time without cancelled meeting
                if (!state.ShowMeetingInfor.ShowingMeetings.Any())
                {
                    var searchedMeeting = await CalendarCommonUtil.GetEventsByTime(state.MeetingInfor.StartDate, state.MeetingInfor.StartTime, state.MeetingInfor.EndDate, state.MeetingInfor.EndTime, state.GetUserTimeZone(), calendarService);
                    foreach (var item in searchedMeeting)
                    {
                        if (item.IsCancelled != true)
                        {
                            state.ShowMeetingInfor.ShowingMeetings.Add(item);
                        }
                    }
                }

                // search by title without cancelled meeting
                if (!state.ShowMeetingInfor.ShowingMeetings.Any() && !string.IsNullOrEmpty(state.MeetingInfor.Title))
                {
                    var searchedMeeting = await calendarService.GetEventsByTitleAsync(state.MeetingInfor.Title);
                    foreach (var item in searchedMeeting)
                    {
                        if (item.IsCancelled != true)
                        {
                            state.ShowMeetingInfor.ShowingMeetings.Add(item);
                        }
                    }
                }

                // search next meeting without cancelled meeting
                if (!state.ShowMeetingInfor.ShowingMeetings.Any())
                {
                    if (state.MeetingInfor.OrderReference != null && state.MeetingInfor.OrderReference.ToLower().Contains(CalendarCommonStrings.Next))
                    {
                        var upcomingMeetings = await calendarService.GetUpcomingEventsAsync();
                        foreach (var item in upcomingMeetings)
                        {
                            if (item.IsCancelled != true && (!state.ShowMeetingInfor.ShowingMeetings.Any() || state.ShowMeetingInfor.ShowingMeetings[0].StartTime == item.StartTime))
                            {
                                state.ShowMeetingInfor.ShowingMeetings.Add(item);
                            }
                        }
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

        protected async Task<DialogTurnResult> CheckFocusedEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.ShowMeetingInfor.FocusedEvents.Any())
                {
                    return await sc.NextAsync();
                }
                else
                {
                    return await sc.BeginDialogAsync(Actions.FindEvent, sc.Options);
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

        protected async Task<DialogTurnResult> AddConflictFlag(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // can't get conflict flag from api, so label them here
                var state = await Accessor.GetAsync(sc.Context);
                for (var i = 0; i < state.ShowMeetingInfor.ShowingMeetings.Count - 1; i++)
                {
                    for (var j = i + 1; j < state.ShowMeetingInfor.ShowingMeetings.Count; j++)
                    {
                        if (state.ShowMeetingInfor.ShowingMeetings[i].StartTime <= state.ShowMeetingInfor.ShowingMeetings[j].StartTime &&
                            state.ShowMeetingInfor.ShowingMeetings[i].EndTime > state.ShowMeetingInfor.ShowingMeetings[j].StartTime)
                        {
                            state.ShowMeetingInfor.ShowingMeetings[i].IsConflict = true;
                            state.ShowMeetingInfor.ShowingMeetings[j].IsConflict = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                // count the conflict meetings
                var totalConflictCount = 0;
                foreach (var eventItem in state.ShowMeetingInfor.ShowingMeetings)
                {
                    if (eventItem.IsConflict)
                    {
                        totalConflictCount++;
                    }
                }

                state.ShowMeetingInfor.TotalConflictCount = totalConflictCount;

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

        protected async Task<DialogTurnResult> ChooseEventPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                if (sc.Result != null)
                {
                    state.ShowMeetingInfor.ShowingMeetings = sc.Result as List<EventModel>;
                }

                if (state.ShowMeetingInfor.ShowingMeetings.Count == 0)
                {
                    // should not doto this part. add log here for safe
                    await HandleDialogExceptions(sc, new Exception("Unexpect zero events count"));
                    return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                }
                else if (state.ShowMeetingInfor.ShowingMeetings.Count > 1)
                {
                    if (string.IsNullOrEmpty(state.ShowMeetingInfor.ShowingCardTitle))
                    {
                        state.ShowMeetingInfor.ShowingCardTitle = CalendarCommonStrings.MeetingsToChoose;
                    }

                    var prompt = await GetGeneralMeetingListResponseAsync(sc.Context, state, false, CalendarSharedResponses.MultipleEventsFound, null);

                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt });
                }
                else
                {
                    state.ShowMeetingInfor.FocusedEvents.Add(state.ShowMeetingInfor.ShowingMeetings.First());
                    return await sc.EndDialogAsync(true);
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

        protected async Task<DialogTurnResult> AfterChooseEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                var luisResult = state.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;

                var generalLuisResult = state.GeneralLuisResult;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;
                generalTopIntent = MergeShowIntent(generalTopIntent, topIntent, luisResult);

                if ((generalTopIntent == General.Intent.ShowNext || topIntent == CalendarLuis.Intent.ShowNextCalendar) && state.ShowMeetingInfor.ShowingMeetings != null)
                {
                    if ((state.ShowMeetingInfor.ShowEventIndex + 1) * state.PageSize < state.ShowMeetingInfor.ShowingMeetings.Count)
                    {
                        state.ShowMeetingInfor.ShowEventIndex++;
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.CalendarNoMoreEvent));
                    }

                    return await sc.ReplaceDialogAsync(Actions.ChooseEvent, sc.Options);
                }
                else if ((generalTopIntent == General.Intent.ShowPrevious || topIntent == CalendarLuis.Intent.ShowPreviousCalendar) && state.ShowMeetingInfor.ShowingMeetings != null)
                {
                    if (state.ShowMeetingInfor.ShowEventIndex > 0)
                    {
                        state.ShowMeetingInfor.ShowEventIndex--;
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.CalendarNoPreviousEvent));
                    }

                    return await sc.ReplaceDialogAsync(Actions.ChooseEvent, sc.Options);
                }

                var filteredMeetingList = GetFilteredEvents(state, userInput, sc.Context.Activity.Locale ?? English, out var showingCardTitle);

                if (filteredMeetingList.Count == 1)
                {
                    state.ShowMeetingInfor.FocusedEvents = filteredMeetingList;
                }
                else if (filteredMeetingList.Count > 1)
                {
                    state.ShowMeetingInfor.Clear();
                    state.ShowMeetingInfor.ShowingCardTitle = showingCardTitle;
                    state.ShowMeetingInfor.ShowingMeetings = filteredMeetingList;
                    return await sc.ReplaceDialogAsync(Actions.ChooseEvent, sc.Options);
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

        protected async Task<DialogTurnResult> ChooseEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.ChooseEvent, sc.Options);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterGetEventsPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                if (sc.Result != null)
                {
                    state.ShowMeetingInfor.ShowingMeetings = sc.Result as List<EventModel>;
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // Validators
        protected async Task<bool> ChoiceValidator(PromptValidatorContext<FoundChoice> pc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(pc.Context);
            var generalLuisResult = state.GeneralLuisResult;
            var generalTopIntent = generalLuisResult?.TopIntent().intent;
            var calendarLuisResult = state.LuisResult;
            var calendarTopIntent = calendarLuisResult?.TopIntent().intent;

            // TODO: The signature for validators has changed to return bool -- Need new way to handle this logic
            // If user want to show more recipient end current choice dialog and return the intent to next step.
            if (generalTopIntent == Luis.General.Intent.ShowNext || generalTopIntent == Luis.General.Intent.ShowPrevious || calendarTopIntent == CalendarLuis.Intent.ShowNextCalendar || calendarTopIntent == CalendarLuis.Intent.ShowPreviousCalendar)
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

        protected General.Intent? MergeShowIntent(General.Intent? generalIntent, CalendarLuis.Intent? calendarIntent, CalendarLuis calendarLuisResult)
        {
            if (generalIntent == General.Intent.ShowNext || generalIntent == General.Intent.ShowPrevious)
            {
                return generalIntent;
            }

            if (calendarIntent == CalendarLuis.Intent.ShowNextCalendar)
            {
                return General.Intent.ShowNext;
            }

            if (calendarIntent == CalendarLuis.Intent.ShowPreviousCalendar)
            {
                return General.Intent.ShowPrevious;
            }

            if (calendarIntent == CalendarLuis.Intent.FindCalendarEntry)
            {
                if (calendarLuisResult.Entities.OrderReference != null)
                {
                    var orderReference = GetOrderReferenceFromEntity(calendarLuisResult.Entities);
                    if (orderReference == "next")
                    {
                        return General.Intent.ShowNext;
                    }
                }
            }

            return generalIntent;
        }

        // Helpers
        protected async Task<Activity> GetOverviewMeetingListResponseAsync(
            ITurnContext context,
            CalendarSkillState state,
            string templateId = null,
            StringDictionary tokens = null)
        {
            var currentEvents = GetCurrentPageMeetings(state, out var firstIndex, out var lastIndex);

            var overviewCard = new Card()
            {
                Name = GetDivergedCardName(context, "CalendarOverview"),
                Data = new CalendarMeetingListCardData()
                {
                    ListTitle = CalendarCommonStrings.OverviewTitle,
                    TotalEventCount = state.ShowMeetingInfor.ShowingMeetings.Count.ToString(),
                    OverlapEventCount = state.ShowMeetingInfor.TotalConflictCount.ToString(),
                    TotalEventCountUnit = string.Format(
                        state.ShowMeetingInfor.ShowingMeetings.Count == 1 ? CalendarCommonStrings.OverviewTotalMeetingOne : CalendarCommonStrings.OverviewTotalMeetingPlural,
                        state.MeetingInfor.StartDateString ?? CalendarCommonStrings.TodayLower),
                    OverlapEventCountUnit = CalendarCommonStrings.OverviewOverlapMeeting,
                    Provider = string.Format(CalendarCommonStrings.OverviewEventSource, currentEvents[0].SourceString()),
                    UserPhoto = await GetMyPhotoUrlAsync(context),
                    Indicator = string.Format(CalendarCommonStrings.ShowMeetingsIndicator, (firstIndex + 1).ToString(), lastIndex.ToString(), state.ShowMeetingInfor.ShowingMeetings.Count.ToString())
                }
            };

            var eventItemList = await GetMeetingCardListAsync(state, currentEvents);

            return ResponseManager.GetCardResponse(templateId, overviewCard, tokens, "EventItemContainer", eventItemList);
        }

        protected async Task<Activity> GetGeneralMeetingListResponseAsync(
            ITurnContext context,
            CalendarSkillState state,
            bool isShowAll = false,
            string templateId = null,
            StringDictionary tokens = null)
        {
            List<EventModel> currentEvents;
            int firstIndex = 0;
            int lastIndex = state.ShowMeetingInfor.ShowingMeetings.Count;

            if (isShowAll)
            {
                currentEvents = state.ShowMeetingInfor.ShowingMeetings;
            }
            else
            {
                currentEvents = GetCurrentPageMeetings(state, out firstIndex, out lastIndex);
            }

            var overviewCard = new Card()
            {
                Name = GetDivergedCardName(context, "CalendarGeneralMeetingList"),
                Data = new CalendarMeetingListCardData()
                {
                    ListTitle = state.ShowMeetingInfor.ShowingCardTitle,
                    TotalEventCount = null,
                    OverlapEventCount = null,
                    TotalEventCountUnit = null,
                    OverlapEventCountUnit = null,
                    Provider = string.Format(CalendarCommonStrings.OverviewEventSource, currentEvents[0].SourceString()),
                    Indicator = string.Format(CalendarCommonStrings.ShowMeetingsIndicator, (firstIndex + 1).ToString(), lastIndex.ToString(), state.ShowMeetingInfor.ShowingMeetings.Count.ToString())
                }
            };

            var eventItemList = await GetMeetingCardListAsync(state, currentEvents);

            return ResponseManager.GetCardResponse(templateId, overviewCard, tokens, "EventItemContainer", eventItemList);
        }

        protected async Task<Activity> GetDetailMeetingResponseAsync(DialogContext dc, EventModel eventItem, string templateId, StringDictionary tokens = null)
        {
            var state = await Accessor.GetAsync(dc.Context);

            var detailCard = new Card()
            {
                Name = eventItem.OnlineMeetingUrl == null ? "CalendarDetailNoJoinButton" : "CalendarDetail",
                Data = new CalendarDetailCardData()
                {
                    Content = eventItem.ContentPreview,
                    MeetingLink = eventItem.OnlineMeetingUrl,
                }
            };

            var participantContainerList = new List<Card>();

            var participantContainerCard = new Card()
            {
                Name = eventItem.Attendees.Count == 0 ? GetDivergedCardName(dc.Context, "CalendarDetailContainerNoParticipants") :
                    eventItem.Attendees.Count > 5 ? GetDivergedCardName(dc.Context, "CalendarDetailContainerParticipantsMore") : GetDivergedCardName(dc.Context, "CalendarDetailContainerParticipantsLess"),
                Data = new CalendarDetailContainerCardData()
                {
                    Title = eventItem.Title,
                    Date = TimeConverter.ConvertUtcToUserTime(eventItem.StartTime, state.GetUserTimeZone()).ToString("dddd M/d"),
                    Time = TimeConverter.ConvertUtcToUserTime(eventItem.StartTime, state.GetUserTimeZone()).ToString("h:mm tt"),
                    Location = eventItem.Location,
                    ParticipantPhoto1 = await GetPhotoByIndexAsync(dc.Context, eventItem.Attendees, 0),
                    ParticipantPhoto2 = await GetPhotoByIndexAsync(dc.Context, eventItem.Attendees, 1),
                    ParticipantPhoto3 = await GetPhotoByIndexAsync(dc.Context, eventItem.Attendees, 2),
                    ParticipantPhoto4 = await GetPhotoByIndexAsync(dc.Context, eventItem.Attendees, 3),
                    ParticipantPhoto5 = await GetPhotoByIndexAsync(dc.Context, eventItem.Attendees, 4),
                    OmittedParticipantCount = eventItem.Attendees.Count - 4,
                    LocationIcon = string.IsNullOrEmpty(eventItem.Location) ? AdaptiveCardHelper.BlankIcon : AdaptiveCardHelper.LocationIcon,
                    Duration = eventItem.ToDisplayDurationString(),
                }
            };

            participantContainerList.Add(participantContainerCard);

            return ResponseManager.GetCardResponse(templateId, detailCard, tokens, "CalendarDetailContainer", participantContainerList);
        }

        protected List<EventModel> GetFilteredEvents(CalendarSkillState state, string userInput, string locale, out string showingCardTitle)
        {
            var luisResult = state.LuisResult;
            var filteredMeetingList = new List<EventModel>();
            showingCardTitle = null;

            // filter meetings with start time
            var timeResult = RecognizeDateTime(userInput, locale, state.GetUserTimeZone(), false);
            if (filteredMeetingList.Count <= 0 && timeResult != null)
            {
                foreach (var result in timeResult)
                {
                    var dateTimeConvertTypeString = result.Timex;
                    var dateTimeConvertType = new TimexProperty(dateTimeConvertTypeString);
                    if (result.Value != null || (dateTimeConvertType.Types.Contains(Constants.TimexTypes.Time) || dateTimeConvertType.Types.Contains(Constants.TimexTypes.DateTime)))
                    {
                        var dateTime = DateTime.Parse(result.Value);

                        if (dateTime != null)
                        {
                            var utcStartTime = TimeZoneInfo.ConvertTimeToUtc(dateTime, state.GetUserTimeZone());
                            foreach (var meeting in GetCurrentPageMeetings(state))
                            {
                                if (meeting.StartTime.TimeOfDay == utcStartTime.TimeOfDay)
                                {
                                    filteredMeetingList.Add(meeting);
                                    showingCardTitle = string.Format(CalendarCommonStrings.MeetingsAt, string.Format("{0:H:mm}", dateTime));
                                }
                            }
                        }
                    }
                }
            }

            // filter meetings with number
            if (filteredMeetingList.Count <= 0 && state.ShowMeetingInfor.UserSelectIndex >= 0)
            {
                var currentList = GetCurrentPageMeetings(state);
                if (state.ShowMeetingInfor.UserSelectIndex < currentList.Count)
                {
                    filteredMeetingList.Add(currentList[state.ShowMeetingInfor.UserSelectIndex]);
                }
            }

            // filter meetings with subject
            if (filteredMeetingList.Count <= 0)
            {
                var subject = userInput;
                if (luisResult.Entities.Subject != null)
                {
                    subject = GetSubjectFromEntity(luisResult.Entities);
                }

                foreach (var meeting in GetCurrentPageMeetings(state))
                {
                    if (meeting.Title.ToLower().Contains(subject.ToLower()))
                    {
                        filteredMeetingList.Add(meeting);
                        showingCardTitle = string.Format(CalendarCommonStrings.MeetingsAbout, subject);
                    }
                }
            }

            // filter meetings with contact name
            if (filteredMeetingList.Count <= 0)
            {
                var contactNameList = new List<string>() { userInput };
                if (luisResult.Entities.personName != null)
                {
                    contactNameList = GetAttendeesFromEntity(luisResult.Entities, userInput);
                }

                foreach (var meeting in GetCurrentPageMeetings(state))
                {
                    var containsAllContacts = true;
                    foreach (var contactName in contactNameList)
                    {
                        if (!meeting.ContainsAttendee(contactName))
                        {
                            containsAllContacts = false;
                            break;
                        }
                    }

                    if (containsAllContacts)
                    {
                        filteredMeetingList.Add(meeting);
                        showingCardTitle = string.Format(CalendarCommonStrings.MeetingsWith, string.Join(", ", contactNameList));
                    }
                }
            }

            return filteredMeetingList;
        }

        protected async Task<string> GetMyPhotoUrlAsync(ITurnContext context)
        {
            var state = await Accessor.GetAsync(context);
            var token = state.APIToken;
            var service = ServiceManager.InitUserService(token, state.EventSource);

            PersonModel me = null;

            try
            {
                me = await service.GetMeAsync();
                if (me != null && !string.IsNullOrEmpty(me.Photo))
                {
                    return me.Photo;
                }

                var displayName = me == null ? AdaptiveCardHelper.DefaultMe : me.DisplayName ?? me.UserPrincipalName ?? AdaptiveCardHelper.DefaultMe;
                return string.Format(AdaptiveCardHelper.DefaultAvatarIconPathFormat, displayName);
            }
            catch (Exception)
            {
            }

            return string.Format(AdaptiveCardHelper.DefaultAvatarIconPathFormat, AdaptiveCardHelper.DefaultMe);
        }

        protected async Task<string> GetUserPhotoUrlAsync(ITurnContext context, EventModel.Attendee attendee)
        {
            var state = await Accessor.GetAsync(context);
            var token = state.APIToken;
            var service = ServiceManager.InitUserService(token, state.EventSource);
            var displayName = attendee.DisplayName ?? attendee.Address;

            try
            {
                var url = await service.GetPhotoAsync(attendee.Address);
                if (!string.IsNullOrEmpty(url))
                {
                    return url;
                }

                return string.Format(AdaptiveCardHelper.DefaultAvatarIconPathFormat, displayName);
            }
            catch (Exception)
            {
            }

            return string.Format(AdaptiveCardHelper.DefaultAvatarIconPathFormat, displayName);
        }

        protected async Task DigestCalendarLuisResult(DialogContext dc, CalendarLuis luisResult, General generalLuisResult, bool isBeginDialog)
        {
            try
            {
                var state = await Accessor.GetAsync(dc.Context);

                var intent = luisResult.TopIntent().intent;

                var entity = luisResult.Entities;

                if (generalLuisResult.Entities.ordinal != null)
                {
                    var value = generalLuisResult.Entities.ordinal[0];
                    var num = int.Parse(value.ToString());
                    state.ShowMeetingInfor.UserSelectIndex = num - 1;
                }
                else if (generalLuisResult.Entities.number != null)
                {
                    var value = generalLuisResult.Entities.number[0];
                    var num = int.Parse(value.ToString());
                    state.ShowMeetingInfor.UserSelectIndex = num - 1;
                }

                if (!isBeginDialog)
                {
                    if (entity.RelationshipName != null)
                    {
                        state.MeetingInfor.CreateHasDetail = true;
                        state.MeetingInfor.ContactInfor.RelatedEntityInfoDict = GetRelatedEntityFromRelationship(entity, luisResult.Text, state.MeetingInfor.ContactInfor.RelatedEntityInfoDict);
                        if (state.MeetingInfor.ContactInfor.ContactsNameList == null)
                        {
                            state.MeetingInfor.ContactInfor.ContactsNameList = new List<string>();
                        }

                        state.MeetingInfor.ContactInfor.ContactsNameList.AddRange(state.MeetingInfor.ContactInfor.RelatedEntityInfoDict.Keys);
                    }

                    return;
                }

                switch (intent)
                {
                    case CalendarLuis.Intent.FindMeetingRoom:
                    case CalendarLuis.Intent.CreateCalendarEntry:
                        {
                            state.MeetingInfor.CreateHasDetail = false;
                            if (entity.Subject != null)
                            {
                                state.MeetingInfor.CreateHasDetail = true;
                                state.MeetingInfor.Title = GetSubjectFromEntity(entity);
                            }

                            if (entity.personName != null)
                            {
                                state.MeetingInfor.CreateHasDetail = true;
                                state.MeetingInfor.ContactInfor.ContactsNameList = GetAttendeesFromEntity(entity, luisResult.Text, state.MeetingInfor.ContactInfor.ContactsNameList);
                            }

                            if (entity.RelationshipName != null)
                            {
                                state.MeetingInfor.CreateHasDetail = true;
                                state.MeetingInfor.ContactInfor.RelatedEntityInfoDict = GetRelatedEntityFromRelationship(entity, luisResult.Text, state.MeetingInfor.ContactInfor.RelatedEntityInfoDict);
                                if (state.MeetingInfor.ContactInfor.ContactsNameList == null)
                                {
                                    state.MeetingInfor.ContactInfor.ContactsNameList = new List<string>();
                                }

                                state.MeetingInfor.ContactInfor.ContactsNameList.AddRange(state.MeetingInfor.ContactInfor.RelatedEntityInfoDict.Keys);
                            }

                            if (entity.FromDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (date != null)
                                {
                                    state.MeetingInfor.CreateHasDetail = true;
                                    state.MeetingInfor.StartDate = date;
                                }

                                // get end date from time range
                                date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (date != null)
                                {
                                    state.MeetingInfor.CreateHasDetail = true;
                                    state.MeetingInfor.EndDate = date;
                                }
                            }

                            if (entity.ToDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, false);
                                if (date != null)
                                {
                                    state.MeetingInfor.CreateHasDetail = true;
                                    state.MeetingInfor.EndDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (time != null)
                                {
                                    state.MeetingInfor.CreateHasDetail = true;
                                    state.MeetingInfor.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (time != null)
                                {
                                    state.MeetingInfor.CreateHasDetail = true;
                                    state.MeetingInfor.EndTime = time;
                                }
                            }

                            if (entity.ToTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, false);
                                if (time != null)
                                {
                                    state.MeetingInfor.CreateHasDetail = true;
                                    state.MeetingInfor.EndTime = time;
                                }
                            }

                            if (entity.Duration != null)
                            {
                                var duration = GetDurationFromEntity(entity, dc.Context.Activity.Locale, state.GetUserTimeZone());
                                if (duration != -1)
                                {
                                    state.MeetingInfor.CreateHasDetail = true;
                                    state.MeetingInfor.Duration = duration;
                                }
                            }

                            if (entity.MeetingRoom != null)
                            {
                                state.MeetingInfor.CreateHasDetail = true;
                                state.MeetingInfor.Location = GetMeetingRoomFromEntity(entity);
                            }

                            if (entity.Location != null)
                            {
                                state.MeetingInfor.CreateHasDetail = true;
                                state.MeetingInfor.Location = GetLocationFromEntity(entity);
                            }

                            break;
                        }

                    case CalendarLuis.Intent.ConnectToMeeting:
                    case CalendarLuis.Intent.TimeRemaining:
                    case CalendarLuis.Intent.AcceptEventEntry:
                    case CalendarLuis.Intent.DeleteCalendarEntry:
                        {
                            if (entity.OrderReference != null)
                            {
                                state.MeetingInfor.OrderReference = GetOrderReferenceFromEntity(entity);
                            }

                            if (entity.Subject != null)
                            {
                                state.MeetingInfor.Title = GetSubjectFromEntity(entity);
                            }

                            if (entity.FromDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (date != null)
                                {
                                    state.MeetingInfor.StartDate = date;
                                }

                                date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (date != null)
                                {
                                    state.MeetingInfor.EndDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (time != null)
                                {
                                    state.MeetingInfor.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (time != null)
                                {
                                    state.MeetingInfor.EndTime = time;
                                }
                            }

                            if (entity.RelationshipName != null)
                            {
                                state.MeetingInfor.CreateHasDetail = true;
                                state.MeetingInfor.ContactInfor.RelatedEntityInfoDict = GetRelatedEntityFromRelationship(entity, luisResult.Text, state.MeetingInfor.ContactInfor.RelatedEntityInfoDict);
                                if (state.MeetingInfor.ContactInfor.ContactsNameList == null)
                                {
                                    state.MeetingInfor.ContactInfor.ContactsNameList = new List<string>();
                                }

                                state.MeetingInfor.ContactInfor.ContactsNameList.AddRange(state.MeetingInfor.ContactInfor.RelatedEntityInfoDict.Keys);
                            }

                            break;
                        }

                    case CalendarLuis.Intent.ChangeCalendarEntry:
                        {
                            if (entity.Subject != null)
                            {
                                state.MeetingInfor.Title = GetSubjectFromEntity(entity);
                            }

                            if (entity.FromDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (date != null)
                                {
                                    state.MeetingInfor.StartDate = date;
                                }

                                date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (date != null)
                                {
                                    state.MeetingInfor.EndDate = date;
                                }
                            }

                            if (entity.ToDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (date != null)
                                {
                                    state.UpdateMeetingInfor.NewStartDate = date;
                                }

                                date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (date != null)
                                {
                                    state.UpdateMeetingInfor.NewEndDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (time != null)
                                {
                                    state.MeetingInfor.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (time != null)
                                {
                                    state.MeetingInfor.EndTime = time;
                                }
                            }

                            if (entity.ToTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (time != null)
                                {
                                    state.UpdateMeetingInfor.NewStartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (time != null)
                                {
                                    state.UpdateMeetingInfor.NewEndTime = time;
                                }
                            }

                            if (entity.MoveEarlierTimeSpan != null)
                            {
                                state.UpdateMeetingInfor.MoveTimeSpan = GetMoveTimeSpanFromEntity(entity.MoveEarlierTimeSpan[0], dc.Context.Activity.Locale, false, state.GetUserTimeZone());
                            }

                            if (entity.MoveLaterTimeSpan != null)
                            {
                                state.UpdateMeetingInfor.MoveTimeSpan = GetMoveTimeSpanFromEntity(entity.MoveLaterTimeSpan[0], dc.Context.Activity.Locale, true, state.GetUserTimeZone());
                            }

                            if (entity.datetime != null)
                            {
                                var match = entity._instance.datetime.ToList().Find(w => w.Text.ToLower() == CalendarCommonStrings.DailyToken
                                || w.Text.ToLower() == CalendarCommonStrings.WeeklyToken
                                || w.Text.ToLower() == CalendarCommonStrings.MonthlyToken);
                                if (match != null)
                                {
                                    state.UpdateMeetingInfor.RecurrencePattern = match.Text.ToLower();
                                }
                            }

                            if (entity.RelationshipName != null)
                            {
                                state.MeetingInfor.CreateHasDetail = true;
                                state.MeetingInfor.ContactInfor.RelatedEntityInfoDict = GetRelatedEntityFromRelationship(entity, luisResult.Text, state.MeetingInfor.ContactInfor.RelatedEntityInfoDict);
                                if (state.MeetingInfor.ContactInfor.ContactsNameList == null)
                                {
                                    state.MeetingInfor.ContactInfor.ContactsNameList = new List<string>();
                                }

                                state.MeetingInfor.ContactInfor.ContactsNameList.AddRange(state.MeetingInfor.ContactInfor.RelatedEntityInfoDict.Keys);
                            }

                            break;
                        }

                    case CalendarLuis.Intent.FindCalendarEntry:
                    case CalendarLuis.Intent.FindCalendarDetail:
                    case CalendarLuis.Intent.FindCalendarWhen:
                    case CalendarLuis.Intent.FindCalendarWhere:
                    case CalendarLuis.Intent.FindCalendarWho:
                    case CalendarLuis.Intent.FindDuration:
                        {
                            if (entity.OrderReference != null)
                            {
                                state.MeetingInfor.OrderReference = GetOrderReferenceFromEntity(entity);
                            }

                            if (entity.FromDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (date != null)
                                {
                                    state.MeetingInfor.StartDate = date;
                                    state.MeetingInfor.StartDateString = dateString;
                                }

                                date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (date != null)
                                {
                                    state.MeetingInfor.EndDate = date;
                                }
                            }

                            if (entity.ToDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, false);
                                if (date != null)
                                {
                                    state.MeetingInfor.EndDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (time != null)
                                {
                                    state.MeetingInfor.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (time != null)
                                {
                                    state.MeetingInfor.EndTime = time;
                                }
                            }

                            if (entity.ToTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, false);
                                if (time != null)
                                {
                                    state.MeetingInfor.EndTime = time;
                                }
                            }

                            if (entity.RelationshipName != null)
                            {
                                state.MeetingInfor.CreateHasDetail = true;
                                state.MeetingInfor.ContactInfor.RelatedEntityInfoDict = GetRelatedEntityFromRelationship(entity, luisResult.Text, state.MeetingInfor.ContactInfor.RelatedEntityInfoDict);
                                if (state.MeetingInfor.ContactInfor.ContactsNameList == null)
                                {
                                    state.MeetingInfor.ContactInfor.ContactsNameList = new List<string>();
                                }

                                state.MeetingInfor.ContactInfor.ContactsNameList.AddRange(state.MeetingInfor.ContactInfor.RelatedEntityInfoDict.Keys);
                            }

                            state.ShowMeetingInfor.AskParameterContent = luisResult.Text;

                            break;
                        }

                    case CalendarLuis.Intent.None:
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

        protected List<DateTimeResolution> RecognizeDateTime(string dateTimeString, string culture, TimeZoneInfo userTimeZone, bool convertToDate = true)
        {
            var userNow = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, userTimeZone);
            var results = DateTimeRecognizer.RecognizeDateTime(DateTimeHelper.ConvertNumberToDateTimeString(dateTimeString, convertToDate), culture, DateTimeOptions.CalendarMode, userNow);

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
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            // send error message to bot user
            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CalendarSharedResponses.CalendarErrorMessage));

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
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            // send error message to bot user
            if (ex.ExceptionType == SkillExceptionType.APIAccessDenied)
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CalendarSharedResponses.CalendarErrorMessageBotProblem));
            }
            else
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CalendarSharedResponses.CalendarErrorMessage));
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
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });
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

        protected bool IsEmail(string emailString)
        {
            return Regex.IsMatch(emailString, @"\w[-\w.+]*@([A-Za-z0-9][-A-Za-z0-9]+\.)+[A-Za-z]{2,14}");
        }

        protected async Task<string> GetReadyToSendNameListStringAsync(WaterfallStepContext sc)
        {
            var state = await Accessor.GetAsync(sc?.Context);
            var unionList = state.MeetingInfor.ContactInfor.ContactsNameList.ToList();
            if (unionList.Count == 1)
            {
                return unionList.First();
            }

            var nameString = string.Join(", ", unionList.ToArray().SkipLast(1)) + string.Format(CommonStrings.SeparatorFormat, CommonStrings.And) + unionList.Last();
            return nameString;
        }

        protected (List<PersonModel> formattedPersonList, List<PersonModel> formattedUserList) FormatRecipientList(List<PersonModel> personList, List<PersonModel> userList)
        {
            // Remove dup items
            var formattedPersonList = new List<PersonModel>();
            var formattedUserList = new List<PersonModel>();

            foreach (var person in personList)
            {
                var mailAddress = person.Emails[0] ?? person.UserPrincipalName;
                if (mailAddress == null || !IsEmail(mailAddress))
                {
                    continue;
                }

                var isDup = false;
                foreach (var formattedPerson in formattedPersonList)
                {
                    var formattedMailAddress = formattedPerson.Emails[0] ?? formattedPerson.UserPrincipalName;

                    if (mailAddress.Equals(formattedMailAddress))
                    {
                        isDup = true;
                        break;
                    }
                }

                if (!isDup)
                {
                    formattedPersonList.Add(person);
                }
            }

            foreach (var user in userList)
            {
                var mailAddress = user.Emails[0] ?? user.UserPrincipalName;
                if (mailAddress == null || !IsEmail(mailAddress))
                {
                    continue;
                }

                var isDup = false;
                foreach (var formattedPerson in formattedPersonList)
                {
                    var formattedMailAddress = formattedPerson.Emails[0] ?? formattedPerson.UserPrincipalName;

                    if (mailAddress.Equals(formattedMailAddress))
                    {
                        isDup = true;
                        break;
                    }
                }

                if (!isDup)
                {
                    foreach (var formattedUser in formattedUserList)
                    {
                        var formattedMailAddress = formattedUser.Emails[0] ?? formattedUser.UserPrincipalName;

                        if (mailAddress.Equals(formattedMailAddress))
                        {
                            isDup = true;
                            break;
                        }
                    }
                }

                if (!isDup)
                {
                    formattedUserList.Add(user);
                }
            }

            return (formattedPersonList, formattedUserList);
        }

        protected async Task<List<PersonModel>> GetContactsAsync(WaterfallStepContext sc, string name)
        {
            var result = new List<PersonModel>();
            var state = await Accessor.GetAsync(sc.Context);
            var token = state.APIToken;
            var service = ServiceManager.InitUserService(token, state.EventSource);

            // Get users.
            result = await service.GetContactsAsync(name);
            return result;
        }

        protected async Task<List<PersonModel>> GetPeopleWorkWithAsync(WaterfallStepContext sc, string name)
        {
            var result = new List<PersonModel>();
            var state = await Accessor.GetAsync(sc.Context);
            var token = state.APIToken;
            var service = ServiceManager.InitUserService(token, state.EventSource);

            // Get users.
            result = await service.GetPeopleAsync(name);

            return result;
        }

        protected async Task<List<PersonModel>> GetUserAsync(WaterfallStepContext sc, string name)
        {
            var result = new List<PersonModel>();
            var state = await Accessor.GetAsync(sc.Context);
            var token = state.APIToken;
            var service = ServiceManager.InitUserService(token, state.EventSource);

            // Get users.
            result = await service.GetUserAsync(name);

            return result;
        }

        protected async Task<PersonModel> GetMyManager(WaterfallStepContext sc)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var token = state.APIToken;
            var service = ServiceManager.InitUserService(token, state.EventSource);
            return await service.GetMyManagerAsync();
        }

        protected async Task<PersonModel> GetManager(WaterfallStepContext sc, string name)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var token = state.APIToken;
            var service = ServiceManager.InitUserService(token, state.EventSource);
            return await service.GetManagerAsync(name);
        }

        protected async Task<PersonModel> GetMe(ITurnContext context)
        {
            var state = await Accessor.GetAsync(context);
            var token = state.APIToken;
            var service = ServiceManager.InitUserService(token, state.EventSource);
            return await service.GetMeAsync();
        }

        protected string GetSelectPromptString(PromptOptions selectOption, bool containNumbers)
        {
            var result = string.Empty;
            result += selectOption.Prompt.Text + "\r\n";
            for (var i = 0; i < selectOption.Choices.Count; i++)
            {
                var choice = selectOption.Choices[i];
                result += "  ";
                if (containNumbers)
                {
                    result += (i + 1) + "-";
                }

                result += choice.Value + "\r\n";
            }

            return result;
        }

        private string GetSubjectFromEntity(CalendarLuis._Entities entity)
        {
            return entity.Subject[0];
        }

        private List<string> GetAttendeesFromEntity(CalendarLuis._Entities entity, string inputString, List<string> attendees = null)
        {
            if (attendees == null)
            {
                attendees = new List<string>();
            }

            // As luis result for email address often contains extra spaces for word breaking
            // (e.g. send email to test@test.com, email address entity will be test @ test . com)
            // So use original user input as email address.
            var rawEntity = entity._instance.personName;
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

        protected Dictionary<string, CalendarSkillState.RelatedEntityInfo> GetRelatedEntityFromRelationship(CalendarLuis._Entities entity, string inputString, Dictionary<string, CalendarSkillState.RelatedEntityInfo> entities = null )
        {
            if (entities == null)
            {
                entities = new Dictionary<string, CalendarSkillState.RelatedEntityInfo>();
            }

            int index = 0;
            var rawRelationships = entity._instance.RelationshipName;
            var rawPronouns = entity._instance.PossessivePronoun;
            if (rawRelationships != null && rawPronouns != null)
            {
                foreach (var relationship in rawRelationships)
                {
                    string relationshipName = relationship.Text;
                    for (int i = 0; i < entity.PossessivePronoun.Length; i++)
                    {
                        string pronounType = entity.PossessivePronoun[i][0];
                        string pronounName = rawPronouns[i].Text;
                        if (relationship.EndIndex > rawPronouns[i].StartIndex)
                        {
                            var originalName = inputString.Substring(rawPronouns[i].StartIndex, relationship.EndIndex - rawPronouns[i].StartIndex);
                            if (Regex.IsMatch(originalName, "^" + pronounName + "( )?" + relationshipName + "$", RegexOptions.IgnoreCase) && !entities.ContainsKey(originalName))
                            {
                                entities.Add(originalName, new CalendarSkillState.RelatedEntityInfo { PronounType = pronounType, RelationshipName = relationshipName });
                            }
                        }
                    }

                    index++;
                }
            }

            return entities;
        }

        // Workaround until adaptive card renderer in teams is upgraded to v1.2
        private string GetDivergedCardName(ITurnContext turnContext, string card)
        {
            if (Channel.GetChannelId(turnContext) == Channels.Msteams)
            {
                return card + ".1.0";
            }
            else
            {
                return card;
            }
        }

        private List<EventModel> GetCurrentPageMeetings(CalendarSkillState state)
        {
            return GetCurrentPageMeetings(state, out var firstIndex, out var lastIndex);
        }

        private List<EventModel> GetCurrentPageMeetings(CalendarSkillState state, out int firstIndex, out int lastIndex)
        {
            firstIndex = state.ShowMeetingInfor.ShowEventIndex * state.PageSize;
            var count = Math.Min(state.PageSize, state.ShowMeetingInfor.ShowingMeetings.Count - (state.ShowMeetingInfor.ShowEventIndex * state.PageSize));
            lastIndex = firstIndex + count;
            return state.ShowMeetingInfor.ShowingMeetings.GetRange(firstIndex, count);
        }

        private async Task<string> GetPhotoByIndexAsync(ITurnContext context, List<EventModel.Attendee> attendees, int index)
        {
            if (attendees.Count <= index)
            {
                return AdaptiveCardHelper.BlankIcon;
            }

            return await GetUserPhotoUrlAsync(context, attendees[index]);
        }

        private async Task<List<Card>> GetMeetingCardListAsync(CalendarSkillState state, List<EventModel> events)
        {
            var eventItemList = new List<Card>();

            DateTime? currentAddedDateUser = null;
            foreach (var item in events)
            {
                var itemDateUser = TimeConverter.ConvertUtcToUserTime(item.StartTime, state.GetUserTimeZone());
                if (currentAddedDateUser == null || !currentAddedDateUser.Value.Date.Equals(itemDateUser.Date))
                {
                    currentAddedDateUser = itemDateUser;
                    eventItemList.Add(new Card()
                    {
                        Name = "CalendarDate",
                        Data = new CalendarDateCardData()
                        {
                            Date = currentAddedDateUser.Value.ToString("dddd, MMMM d").ToUpper()
                        }
                    });
                }

                eventItemList.Add(new Card()
                {
                    Name = "CalendarItem",
                    Data = item.ToAdaptiveCardData(state.GetUserTimeZone())
                });
            }

            return eventItemList;
        }

        private int GetDurationFromEntity(CalendarLuis._Entities entity, string local, TimeZoneInfo userTimeZone)
        {
            var culture = local ?? English;
            var result = RecognizeDateTime(entity.Duration[0], culture, userTimeZone);
            if (result != null)
            {
                if (result[0].Value != null)
                {
                    return int.Parse(result[0].Value);
                }
            }

            return -1;
        }

        private int GetMoveTimeSpanFromEntity(string timeSpan, string local, bool later, TimeZoneInfo userTimeZone)
        {
            var culture = local ?? English;
            var result = RecognizeDateTime(timeSpan, culture, userTimeZone);
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

        private string GetMeetingRoomFromEntity(CalendarLuis._Entities entity)
        {
            return entity.MeetingRoom[0];
        }

        private string GetLocationFromEntity(CalendarLuis._Entities entity)
        {
            return entity.Location[0];
        }

        private string GetDateTimeStringFromInstanceData(string inputString, InstanceData data)
        {
            return inputString.Substring(data.StartIndex, data.EndIndex - data.StartIndex);
        }

        private List<DateTime> GetDateFromDateTimeString(string date, string local, TimeZoneInfo userTimeZone, bool isStart, bool isTargetTimeRange)
        {
            // if isTargetTimeRange is true, will only parse the time range
            var culture = local ?? English;
            var results = RecognizeDateTime(date, culture, userTimeZone, true);
            var dateTimeResults = new List<DateTime>();
            if (results != null)
            {
                foreach (var result in results)
                {
                    if (result.Value != null)
                    {
                        if (isTargetTimeRange)
                        {
                            break;
                        }

                        var dateTime = DateTime.Parse(result.Value);
                        var dateTimeConvertType = result.Timex;

                        if (dateTime != null)
                        {
                            dateTimeResults.Add(dateTime);
                        }
                    }
                    else
                    {
                        var startDate = DateTime.Parse(result.Start);
                        var endDate = DateTime.Parse(result.End);
                        if (isStart)
                        {
                            dateTimeResults.Add(startDate);
                        }
                        else
                        {
                            dateTimeResults.Add(endDate);
                        }
                    }
                }
            }

            return dateTimeResults;
        }

        private List<DateTime> GetTimeFromDateTimeString(string time, string local, TimeZoneInfo userTimeZone, bool isStart, bool isTargetTimeRange)
        {
            // if isTargetTimeRange is true, will only parse the time range
            var culture = local ?? English;
            var results = RecognizeDateTime(time, culture, userTimeZone, false);
            var dateTimeResults = new List<DateTime>();
            if (results != null)
            {
                foreach (var result in results)
                {
                    if (result.Value != null)
                    {
                        if (isTargetTimeRange)
                        {
                            break;
                        }

                        var dateTime = DateTime.Parse(result.Value);
                        var dateTimeConvertType = result.Timex;

                        if (dateTime != null)
                        {
                            dateTimeResults.Add(dateTime);
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

        private string GetOrderReferenceFromEntity(CalendarLuis._Entities entity)
        {
            return entity.OrderReference[0];
        }
    }
}