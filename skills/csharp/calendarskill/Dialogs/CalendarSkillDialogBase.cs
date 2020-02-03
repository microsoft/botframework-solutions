// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Options;
using CalendarSkill.Prompts;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.CreateEvent;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.Summary;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.Number;
using static CalendarSkill.Models.CreateEventStateModel;
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
            ConversationState conversationState,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(dialogId)
        {
            Settings = settings;
            Services = services;
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            Accessor = _conversationState.CreateProperty<CalendarSkillState>(nameof(CalendarSkillState));
            ServiceManager = serviceManager;
            TelemetryClient = telemetryClient;
            TemplateEngine = localeTemplateEngineManager;

            AddDialog(new MultiProviderAuthDialog(settings.OAuthConnections));
            AddDialog(new TextPrompt(Actions.Prompt));
            AddDialog(new ConfirmPrompt(Actions.TakeFurtherAction, null, Culture.English) { Style = ListStyle.SuggestedAction });
            AddDialog(new ChoicePrompt(Actions.Choice, ChoiceValidator, Culture.English) { Style = ListStyle.None, });
            AddDialog(new TimePrompt(Actions.TimePrompt));
            AddDialog(new GetEventPrompt(Actions.GetEventPrompt));
        }

        protected LocaleTemplateEngineManager TemplateEngine { get; set; }

        protected BotSettings Settings { get; set; }

        protected BotServices Services { get; set; }

        protected IStatePropertyAccessor<CalendarSkillState> Accessor { get; set; }

        protected IServiceManager ServiceManager { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(dc.Context);

            // find contact dialog is not a start dialog, should not run luis part.
            var luisResult = dc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
            var generalLuisResult = dc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
            if (luisResult != null && Id != nameof(FindContactDialog))
            {
                await DigestCalendarLuisResult(dc, luisResult, generalLuisResult, true);
            }

            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(dc.Context);
            var luisResult = dc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
            var generalLuisResult = dc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
            if (luisResult != null)
            {
                await DigestCalendarLuisResult(dc, luisResult, generalLuisResult, false);
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

                    if (sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token))
                    {
                        sc.Context.TurnState[StateProperties.APITokenKey] = providerTokenResponse.TokenResponse.Token;
                    }
                    else
                    {
                        sc.Context.TurnState[StateProperties.APITokenKey] = providerTokenResponse.TokenResponse.Token;
                    }

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
                sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
                var calendarService = ServiceManager.InitCalendarService(token as string, state.EventSource);

                // search by time without cancelled meeting
                if (!state.ShowMeetingInfo.ShowingMeetings.Any())
                {
                    var searchedMeeting = await CalendarCommonUtil.GetEventsByTime(state.MeetingInfo.StartDate, state.MeetingInfo.StartTime, state.MeetingInfo.EndDate, state.MeetingInfo.EndTime, state.GetUserTimeZone(), calendarService);
                    foreach (var item in searchedMeeting)
                    {
                        if (item.IsCancelled != true)
                        {
                            state.ShowMeetingInfo.ShowingMeetings.Add(item);
                            state.ShowMeetingInfo.Condition = CalendarSkillState.ShowMeetingInfomation.SearchMeetingCondition.Time;
                        }
                    }
                }

                // search by title without cancelled meeting
                if (!state.ShowMeetingInfo.ShowingMeetings.Any() && !string.IsNullOrEmpty(state.MeetingInfo.Title))
                {
                    var searchedMeeting = await calendarService.GetEventsByTitleAsync(state.MeetingInfo.Title);
                    foreach (var item in searchedMeeting)
                    {
                        if (item.IsCancelled != true)
                        {
                            state.ShowMeetingInfo.ShowingMeetings.Add(item);
                            state.ShowMeetingInfo.Condition = CalendarSkillState.ShowMeetingInfomation.SearchMeetingCondition.Title;
                        }
                    }
                }

                // search by participants without cancelled meeting
                if (!state.ShowMeetingInfo.ShowingMeetings.Any() && state.MeetingInfo.ContactInfor.ContactsNameList.Any())
                {
                    var utcNow = DateTime.UtcNow;
                    var searchedMeeting = await calendarService.GetEventsByTimeAsync(utcNow, utcNow.AddDays(14));

                    foreach (var item in searchedMeeting)
                    {
                        var containsAllContacts = true;
                        foreach (var contactName in state.MeetingInfo.ContactInfor.ContactsNameList)
                        {
                            if (!item.ContainsAttendee(contactName))
                            {
                                containsAllContacts = false;
                                break;
                            }
                        }

                        if (containsAllContacts && item.IsCancelled != true)
                        {
                            state.ShowMeetingInfo.ShowingMeetings.Add(item);
                            state.ShowMeetingInfo.Condition = CalendarSkillState.ShowMeetingInfomation.SearchMeetingCondition.Attendee;
                        }
                    }
                }

                // search by location without cancelled meeting
                if (!state.ShowMeetingInfo.ShowingMeetings.Any() && !string.IsNullOrEmpty(state.MeetingInfo.Location))
                {
                    var utcNow = DateTime.UtcNow;
                    var searchedMeeting = await calendarService.GetEventsByTimeAsync(utcNow, utcNow.AddDays(14));

                    foreach (var item in searchedMeeting)
                    {
                        if (item.Location.Contains(state.MeetingInfo.Location) && item.IsCancelled != true)
                        {
                            state.ShowMeetingInfo.ShowingMeetings.Add(item);
                            state.ShowMeetingInfo.Condition = CalendarSkillState.ShowMeetingInfomation.SearchMeetingCondition.Location;
                        }
                    }
                }

                // search next meeting without cancelled meeting
                if (!state.ShowMeetingInfo.ShowingMeetings.Any())
                {
                    if (state.MeetingInfo.OrderReference != null && state.MeetingInfo.OrderReference.ToLower().Contains(CalendarCommonStrings.Next))
                    {
                        var upcomingMeetings = await calendarService.GetUpcomingEventsAsync();
                        foreach (var item in upcomingMeetings)
                        {
                            if (item.IsCancelled != true && (!state.ShowMeetingInfo.ShowingMeetings.Any() || state.ShowMeetingInfo.ShowingMeetings[0].StartTime == item.StartTime))
                            {
                                state.ShowMeetingInfo.ShowingMeetings.Add(item);
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
                if (state.ShowMeetingInfo.FocusedEvents.Any())
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
                for (var i = 0; i < state.ShowMeetingInfo.ShowingMeetings.Count - 1; i++)
                {
                    for (var j = i + 1; j < state.ShowMeetingInfo.ShowingMeetings.Count; j++)
                    {
                        if (state.ShowMeetingInfo.ShowingMeetings[i].StartTime <= state.ShowMeetingInfo.ShowingMeetings[j].StartTime &&
                            state.ShowMeetingInfo.ShowingMeetings[i].EndTime > state.ShowMeetingInfo.ShowingMeetings[j].StartTime)
                        {
                            state.ShowMeetingInfo.ShowingMeetings[i].IsConflict = true;
                            state.ShowMeetingInfo.ShowingMeetings[j].IsConflict = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                // count the conflict meetings
                var totalConflictCount = 0;
                foreach (var eventItem in state.ShowMeetingInfo.ShowingMeetings)
                {
                    if (eventItem.IsConflict)
                    {
                        totalConflictCount++;
                    }
                }

                state.ShowMeetingInfo.TotalConflictCount = totalConflictCount;

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
                    state.ShowMeetingInfo.ShowingMeetings = sc.Result as List<EventModel>;
                }

                if (state.ShowMeetingInfo.ShowingMeetings.Count == 0)
                {
                    // should not doto this part. add log here for safe
                    await HandleDialogExceptions(sc, new Exception("Unexpect zero events count"));
                    return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                }
                else if (state.ShowMeetingInfo.ShowingMeetings.Count > 1)
                {
                    if (string.IsNullOrEmpty(state.ShowMeetingInfo.ShowingCardTitle))
                    {
                        state.ShowMeetingInfo.ShowingCardTitle = CalendarCommonStrings.MeetingsToChoose;
                    }

                    var prompt = await GetGeneralMeetingListResponseAsync(sc, state, false, CalendarSharedResponses.MultipleEventsFound);

                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt });
                }
                else
                {
                    state.ShowMeetingInfo.FocusedEvents.Add(state.ShowMeetingInfo.ShowingMeetings.First());
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

                var luisResult = sc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
                var topIntent = luisResult?.TopIntent().intent;

                var generalLuisResult = sc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                var generalTopIntent = generalLuisResult?.TopIntent().intent;
                generalTopIntent = MergeShowIntent(generalTopIntent, topIntent, luisResult);

                if ((generalTopIntent == General.Intent.ShowNext || topIntent == CalendarLuis.Intent.ShowNextCalendar) && state.ShowMeetingInfo.ShowingMeetings != null)
                {
                    if ((state.ShowMeetingInfo.ShowEventIndex + 1) * state.PageSize < state.ShowMeetingInfo.ShowingMeetings.Count)
                    {
                        state.ShowMeetingInfo.ShowEventIndex++;
                    }
                    else
                    {
                        var activity = TemplateEngine.GenerateActivityForLocale(SummaryResponses.CalendarNoMoreEvent);
                        await sc.Context.SendActivityAsync(activity);
                    }

                    return await sc.ReplaceDialogAsync(Actions.ChooseEvent, sc.Options);
                }
                else if ((generalTopIntent == General.Intent.ShowPrevious || topIntent == CalendarLuis.Intent.ShowPreviousCalendar) && state.ShowMeetingInfo.ShowingMeetings != null)
                {
                    if (state.ShowMeetingInfo.ShowEventIndex > 0)
                    {
                        state.ShowMeetingInfo.ShowEventIndex--;
                    }
                    else
                    {
                        var activity = TemplateEngine.GenerateActivityForLocale(SummaryResponses.CalendarNoPreviousEvent);
                        await sc.Context.SendActivityAsync(activity);
                    }

                    return await sc.ReplaceDialogAsync(Actions.ChooseEvent, sc.Options);
                }

                var filteredMeetingList = GetFilteredEvents(state, luisResult, userInput, sc.Context.Activity.Locale ?? English, out var showingCardTitle);

                if (filteredMeetingList.Count == 1)
                {
                    state.ShowMeetingInfo.FocusedEvents = filteredMeetingList;
                }
                else if (filteredMeetingList.Count > 1)
                {
                    state.ShowMeetingInfo.Clear();
                    state.ShowMeetingInfo.ShowingCardTitle = showingCardTitle;
                    state.ShowMeetingInfo.ShowingMeetings = filteredMeetingList;
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
                    state.ShowMeetingInfo.ShowingMeetings = sc.Result as List<EventModel>;
                }
                else if (!state.ShowMeetingInfo.ShowingMeetings.Any())
                {
                    // user has tried 3 times but can't get result
                    var activity = TemplateEngine.GenerateActivityForLocale(CalendarSharedResponses.RetryTooManyResponse);
                    await sc.Context.SendActivityAsync(activity);

                    return await sc.CancelAllDialogsAsync();
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CollectStartDate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (!state.MeetingInfo.StartDate.Any() && state.MeetingInfo.StartDateTime == null)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateStartDateForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound), cancellationToken);
                }
                else
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

        protected async Task<DialogTurnResult> CollectStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfo.StartDateTime == null && (state.MeetingInfo.RecreateState == null || state.MeetingInfo.RecreateState == RecreateEventState.Time))
                {
                    return await sc.BeginDialogAsync(Actions.UpdateStartTimeForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound), cancellationToken);
                }
                else
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

        protected async Task<DialogTurnResult> CollectDuration(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.MeetingInfo.EndDateTime == null)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateDurationForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound), cancellationToken);
                }
                else
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

        // update start date waterfall steps
        protected async Task<DialogTurnResult> UpdateStartDateForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isStartDateSkipByDefault = false;
                isStartDateSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventStartDate")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfo.CreateHasDetail && isStartDateSkipByDefault.GetValueOrDefault() && state.MeetingInfo.RecreateState != RecreateEventState.Time)
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                return await sc.PromptAsync(Actions.DatePromptForCreate, new DatePromptOptions
                {
                    Prompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.NoStartDate) as Activity,
                    RetryPrompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.NoStartDateRetry) as Activity,
                    TimeZone = state.GetUserTimeZone(),
                    MaxReprompt = CalendarCommonUtil.MaxRepromptCount
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterUpdateStartDateForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isStartDateSkipByDefault = false;
                isStartDateSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventStartDate")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfo.CreateHasDetail && isStartDateSkipByDefault.GetValueOrDefault() && state.MeetingInfo.RecreateState != RecreateEventState.Time)
                {
                    var datetime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, state.GetUserTimeZone());
                    var defaultValue = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventStartDate")?.DefaultValue;
                    if (int.TryParse(defaultValue, out var startDateOffset))
                    {
                        datetime = datetime.AddDays(startDateOffset);
                    }

                    state.MeetingInfo.StartDate.Add(datetime);
                }
                else if (sc.Result != null)
                {
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
                                    if (CalendarCommonUtil.ContainsTime(dateTimeConvertType))
                                    {
                                        state.MeetingInfo.StartTime.Add(dateTime);
                                    }

                                    state.MeetingInfo.StartDate.Add(dateTime);
                                }
                            }
                            catch (FormatException ex)
                            {
                                await HandleExpectedDialogExceptions(sc, ex);
                            }
                        }
                    }
                }
                else
                {
                    // user has tried 5 times but can't get result
                    var activity = TemplateEngine.GenerateActivityForLocale(CalendarSharedResponses.RetryTooManyResponse);
                    await sc.Context.SendActivityAsync(activity);
                    return await sc.CancelAllDialogsAsync();
                }

                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // update start time waterfall steps
        protected async Task<DialogTurnResult> UpdateStartTimeForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (!state.MeetingInfo.StartTime.Any())
                {
                    return await sc.PromptAsync(Actions.TimePromptForCreate, new TimePromptOptions
                    {
                        Prompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.NoStartTime) as Activity,
                        RetryPrompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.NoStartTimeRetry) as Activity,
                        NoSkipPrompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.NoStartTimeNoSkip) as Activity,
                        TimeZone = state.GetUserTimeZone(),
                        MaxReprompt = CalendarCommonUtil.MaxRepromptCount
                    }, cancellationToken);
                }
                else
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

        protected async Task<DialogTurnResult> AfterUpdateStartTimeForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (!state.MeetingInfo.StartTime.Any())
                {
                    if (sc.Result != null)
                    {
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
                                        state.MeetingInfo.StartTime.Add(dateTime);
                                    }
                                }
                                catch (FormatException ex)
                                {
                                    await HandleExpectedDialogExceptions(sc, ex);
                                }
                            }
                        }
                    }
                    else
                    {
                        // user has tried 5 times but can't get result
                        var activity = TemplateEngine.GenerateActivityForLocale(CalendarSharedResponses.RetryTooManyResponse);
                        await sc.Context.SendActivityAsync(activity);
                        return await sc.CancelAllDialogsAsync();
                    }
                }

                var userNow = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone());
                var startDate = state.MeetingInfo.StartDate.Last();
                var endDate = state.MeetingInfo.EndDate.Any() ? state.MeetingInfo.EndDate.Last() : startDate;

                List<DateTime> startTimes = new List<DateTime>();
                List<DateTime> endTimes = new List<DateTime>();
                foreach (var time in state.MeetingInfo.StartTime)
                {
                    startTimes.Add(startDate.Date.AddSeconds(time.TimeOfDay.TotalSeconds));
                }

                foreach (var time in state.MeetingInfo.EndTime)
                {
                    endTimes.Add(endDate.Date.AddSeconds(time.TimeOfDay.TotalSeconds));
                }

                var isStartTimeRestricted = Settings.RestrictedValue?.MeetingTime?.First(item => item.Name == "WorkTimeStart")?.IsRestricted;
                var isEndTimeRestricted = Settings.RestrictedValue?.MeetingTime?.First(item => item.Name == "WorkTimeEnd")?.IsRestricted;
                DateTime baseTime = startDate.Date;
                DateTime startTimeRestricted = isStartTimeRestricted.GetValueOrDefault() ? baseTime.AddSeconds(DateTime.Parse(Settings.RestrictedValue?.MeetingTime?.First(item => item.Name == "WorkTimeStart")?.Value).TimeOfDay.TotalSeconds) : baseTime;
                DateTime endTimeRestricted = isEndTimeRestricted.GetValueOrDefault() ? baseTime.AddSeconds(DateTime.Parse(Settings.RestrictedValue?.MeetingTime?.First(item => item.Name == "WorkTimeEnd")?.Value).TimeOfDay.TotalSeconds) : baseTime.AddDays(1);

                state.MeetingInfo.StartDateTime = DateTimeHelper.ChooseStartTime(startTimes, endTimes, startTimeRestricted, endTimeRestricted, userNow);
                state.MeetingInfo.StartDateTime = TimeZoneInfo.ConvertTimeToUtc(state.MeetingInfo.StartDateTime.Value, state.GetUserTimeZone());
                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // update duration waterfall steps
        protected async Task<DialogTurnResult> UpdateDurationForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isDurationSkipByDefault = false;
                isDurationSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventDuration")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfo.Duration > 0 || state.MeetingInfo.EndTime.Any() || state.MeetingInfo.EndDate.Any() || (state.MeetingInfo.CreateHasDetail && isDurationSkipByDefault.GetValueOrDefault() && state.MeetingInfo.RecreateState != RecreateEventState.Time && state.MeetingInfo.RecreateState != RecreateEventState.Duration))
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                return await sc.PromptAsync(Actions.DurationPromptForCreate, new CalendarPromptOptions
                {
                    Prompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.NoDuration) as Activity,
                    RetryPrompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.NoDurationRetry) as Activity,
                    MaxReprompt = CalendarCommonUtil.MaxRepromptCount
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterUpdateDurationForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isDurationSkipByDefault = false;
                isDurationSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventDuration")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfo.EndDate.Any() || state.MeetingInfo.EndTime.Any())
                {
                    var startDate = !state.MeetingInfo.StartDate.Any() ? TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone()) : state.MeetingInfo.StartDate.Last();
                    var endDate = startDate;
                    if (state.MeetingInfo.EndDate.Any())
                    {
                        endDate = state.MeetingInfo.EndDate.Last();
                    }

                    if (state.MeetingInfo.EndTime.Any())
                    {
                        foreach (var endtime in state.MeetingInfo.EndTime)
                        {
                            var endDateTime = new DateTime(
                                endDate.Year,
                                endDate.Month,
                                endDate.Day,
                                endtime.Hour,
                                endtime.Minute,
                                endtime.Second);
                            endDateTime = TimeZoneInfo.ConvertTimeToUtc(endDateTime, state.GetUserTimeZone());
                            if (state.MeetingInfo.EndDateTime == null)
                            {
                                state.MeetingInfo.EndDateTime = endDateTime;
                            }

                            if (endDateTime >= state.MeetingInfo.StartDateTime)
                            {
                                state.MeetingInfo.EndDateTime = endDateTime;
                                break;
                            }
                        }
                    }
                    else
                    {
                        state.MeetingInfo.EndDateTime = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59);
                        state.MeetingInfo.EndDateTime = TimeZoneInfo.ConvertTimeToUtc(state.MeetingInfo.EndDateTime.Value, state.GetUserTimeZone());
                    }

                    var ts = state.MeetingInfo.StartDateTime.Value.Subtract(state.MeetingInfo.EndDateTime.Value).Duration();
                    state.MeetingInfo.Duration = (int)ts.TotalSeconds;
                }

                if (state.MeetingInfo.Duration <= 0 && state.MeetingInfo.CreateHasDetail && isDurationSkipByDefault.GetValueOrDefault() && state.MeetingInfo.RecreateState != RecreateEventState.Time && state.MeetingInfo.RecreateState != RecreateEventState.Duration)
                {
                    var defaultValue = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventDuration")?.DefaultValue;
                    if (int.TryParse(defaultValue, out var durationMinutes))
                    {
                        state.MeetingInfo.Duration = durationMinutes * 60;
                    }
                    else
                    {
                        state.MeetingInfo.Duration = 1800;
                    }
                }

                if (state.MeetingInfo.Duration <= 0)
                {
                    if (sc.Result != null)
                    {
                        sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);

                        IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                        if (dateTimeResolutions.First().Value != null)
                        {
                            int.TryParse(dateTimeResolutions.First().Value, out var duration);
                            state.MeetingInfo.Duration = duration;
                        }
                    }
                    else
                    {
                        // user has tried 5 times but can't get result
                        var activity = TemplateEngine.GenerateActivityForLocale(CalendarSharedResponses.RetryTooManyResponse);
                        await sc.Context.SendActivityAsync(activity);
                        return await sc.CancelAllDialogsAsync();
                    }
                }

                if (state.MeetingInfo.Duration > 0)
                {
                    state.MeetingInfo.EndDateTime = state.MeetingInfo.StartDateTime.Value.AddSeconds(state.MeetingInfo.Duration);
                }
                else
                {
                    // should not go to this part in current logic.
                    // place an error handling for save.
                    await HandleDialogExceptions(sc, new Exception("Unexpect Error On get duration"));
                }

                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
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
            var generalLuisResult = pc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
            var generalTopIntent = generalLuisResult?.TopIntent().intent;
            var calendarLuisResult = pc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
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

        protected async Task<Activity> GetOverviewMeetingListResponseAsync(
            DialogContext dc,
            string templateId,
            object tokens = null)
        {
            var state = await Accessor.GetAsync(dc.Context);
            var currentEvents = GetCurrentPageMeetings(state, out var firstIndex, out var lastIndex);
            var eventItemList = await GetMeetingCardListAsync(dc, currentEvents);
            var overviewCardParams = new
            {
                listTitle = CalendarCommonStrings.OverviewTitle,
                totalEventCount = state.ShowMeetingInfo.ShowingMeetings.Count.ToString(),
                overlapEventCount = state.ShowMeetingInfo.TotalConflictCount.ToString(),
                dateTimeString = state.MeetingInfo.StartDateString,
                indicator = string.Format(CalendarCommonStrings.ShowMeetingsIndicator, (firstIndex + 1).ToString(), lastIndex.ToString(), state.ShowMeetingInfo.ShowingMeetings.Count.ToString()),
                userPhoto = await GetMyPhotoUrlAsync(dc.Context),
                provider = string.Format(CalendarCommonStrings.OverviewEventSource, currentEvents[0].SourceString()),
                timezone = state.GetUserTimeZone().Id,
                itemData = eventItemList,
                isOverview = true
            };

            var showMeetingPrompt = TemplateEngine.GenerateActivityForLocale(templateId, tokens) as Activity;
            var cardName = GetDivergedCardName(dc.Context, SummaryResponses.MeetingListCard);
            var meetingListCard = TemplateEngine.GenerateActivityForLocale(cardName, overviewCardParams) as Activity;
            showMeetingPrompt.Attachments = meetingListCard.Attachments;
            return showMeetingPrompt;
        }

        protected async Task<Activity> GetGeneralMeetingListResponseAsync(
            DialogContext dc,
            CalendarSkillState state,
            bool isShowAll = false,
            string templateId = null,
            object tokens = null)
        {
            int firstIndex = 0;
            int lastIndex = state.ShowMeetingInfo.ShowingMeetings.Count;
            int totalCount = -1;

            List<EventModel> currentEvents;
            if (isShowAll)
            {
                currentEvents = state.ShowMeetingInfo.ShowingMeetings;
            }
            else
            {
                currentEvents = GetCurrentPageMeetings(state, out firstIndex, out lastIndex);
            }

            var eventItemList = await GetMeetingCardListAsync(dc, currentEvents);

            var overviewCardParams = new
            {
                listTitle = state.ShowMeetingInfo.ShowingCardTitle,
                totalEventCount = 0,
                overlapEventCount = 0,
                dateTimeString = string.Empty,
                indicator = string.Format(CalendarCommonStrings.ShowMeetingsIndicator, (firstIndex + 1).ToString(), lastIndex.ToString(), totalCount.ToString()),
                userPhoto = string.Empty,
                provider = string.Format(CalendarCommonStrings.OverviewEventSource, currentEvents[0].SourceString()),
                timezone = state.GetUserTimeZone().Id,
                itemData = eventItemList,
                isOverview = false
            };

            var cardName = GetDivergedCardName(dc.Context, SummaryResponses.MeetingListCard);
            if (templateId == null)
            {
                var meetingListCard = TemplateEngine.GenerateActivityForLocale(cardName, overviewCardParams) as Activity;
                return meetingListCard;
            }
            else
            {
                var showMeetingPrompt = TemplateEngine.GenerateActivityForLocale(templateId, tokens) as Activity;
                var meetingListCard = TemplateEngine.GenerateActivityForLocale(cardName, overviewCardParams) as Activity;
                showMeetingPrompt.Attachments = meetingListCard.Attachments;
                return showMeetingPrompt;
            }
        }

        protected async Task<Activity> GetDetailMeetingResponseAsync(
           DialogContext dc,
           EventModel eventItem,
           string templateId,
           object tokens = null)
        {
            var state = await Accessor.GetAsync(dc.Context);

            var taskList = new Task<string>[AdaptiveCardHelper.MaxDisplayRecipientNum];
            for (int i = 0; i < AdaptiveCardHelper.MaxDisplayRecipientNum; i++)
            {
                taskList[i] = GetPhotoByIndexAsync(dc.Context, eventItem.Attendees, i);
            }

            Task.WaitAll(taskList);

            var attendeePhotoList = new List<string>();
            attendeePhotoList.AddRange(taskList.Select(li => li.Result));

            var data = new
            {
                startDateTime = eventItem.StartTime,
                endDateTime = eventItem.EndTime,
                timezone = state.GetUserTimeZone().Id,
                attendees = eventItem.Attendees,
                attendeePhotoList,
                subject = eventItem.Title,
                location = eventItem.Location,
                content = eventItem.ContentPreview ?? eventItem.Content,
                meetingLink = eventItem.OnlineMeetingUrl
            };

            var cardName = GetDivergedCardName(dc.Context, SummaryResponses.MeetingDetailCard);
            if (templateId == null)
            {
                var meetingDetailCard = TemplateEngine.GenerateActivityForLocale(cardName, data) as Activity;
                return meetingDetailCard;
            }
            else
            {
                var showMeetingPrompt = TemplateEngine.GenerateActivityForLocale(templateId, tokens) as Activity;
                var meetingDetailCard = TemplateEngine.GenerateActivityForLocale(cardName, data) as Activity;
                showMeetingPrompt.Attachments = meetingDetailCard.Attachments;
                return showMeetingPrompt;
            }
        }

        protected string GetSearchConditionString(CalendarSkillState state)
        {
            switch (state.ShowMeetingInfo.Condition)
            {
                case CalendarSkillState.ShowMeetingInfomation.SearchMeetingCondition.Time:
                    {
                        if (string.IsNullOrEmpty(state.MeetingInfo.StartDateString) ||
                            state.MeetingInfo.StartDateString.Equals(CalendarCommonStrings.TodayLower, StringComparison.InvariantCultureIgnoreCase) ||
                            state.MeetingInfo.StartDateString.Equals(CalendarCommonStrings.TomorrowLower, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return (state.MeetingInfo.StartDateString ?? CalendarCommonStrings.TodayLower).ToLower();
                        }

                        return string.Format(CalendarCommonStrings.ShowEventDateCondition, state.MeetingInfo.StartDateString);
                    }

                case CalendarSkillState.ShowMeetingInfomation.SearchMeetingCondition.Title:
                    return string.Format(CalendarCommonStrings.ShowEventTitleCondition, state.MeetingInfo.Title);
                case CalendarSkillState.ShowMeetingInfomation.SearchMeetingCondition.Attendee:
                    return string.Format(CalendarCommonStrings.ShowEventContactCondition, string.Join(", ", state.MeetingInfo.ContactInfor.ContactsNameList));
                case CalendarSkillState.ShowMeetingInfomation.SearchMeetingCondition.Location:
                    return string.Format(CalendarCommonStrings.ShowEventLocationCondition, state.MeetingInfo.Location);
            }

            return null;
        }

        protected List<EventModel> GetFilteredEvents(CalendarSkillState state, CalendarLuis luisResult, string userInput, string locale, out string showingCardTitle)
        {
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
            if (filteredMeetingList.Count <= 0 && state.ShowMeetingInfo.UserSelectIndex >= 0)
            {
                var currentList = GetCurrentPageMeetings(state);
                if (state.ShowMeetingInfo.UserSelectIndex < currentList.Count)
                {
                    filteredMeetingList.Add(currentList[state.ShowMeetingInfo.UserSelectIndex]);
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
            context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
            var service = ServiceManager.InitUserService(token as string, state.EventSource);

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
            context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
            var service = ServiceManager.InitUserService(token as string, state.EventSource);
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
                    if (int.TryParse(value.ToString(), out var num))
                    {
                        state.ShowMeetingInfo.UserSelectIndex = num - 1;
                    }
                }
                else if (generalLuisResult.Entities.number != null)
                {
                    var value = generalLuisResult.Entities.number[0];
                    if (int.TryParse(value.ToString(), out var num))
                    {
                        state.ShowMeetingInfo.UserSelectIndex = num - 1;
                    }
                }

                string luisResultText = string.IsNullOrEmpty(luisResult.AlteredText) ? luisResult.Text : luisResult.AlteredText;

                if (!isBeginDialog)
                {
                    if (entity.RelationshipName != null)
                    {
                        state.MeetingInfo.CreateHasDetail = true;
                        state.MeetingInfo.ContactInfor.RelatedEntityInfoDict = GetRelatedEntityFromRelationship(entity, luisResultText);
                        if (state.MeetingInfo.ContactInfor.ContactsNameList == null)
                        {
                            state.MeetingInfo.ContactInfor.ContactsNameList = new List<string>();
                        }

                        state.MeetingInfo.ContactInfor.ContactsNameList.AddRange(state.MeetingInfo.ContactInfor.RelatedEntityInfoDict.Keys);
                    }

                    return;
                }

                switch (intent)
                {
                    case CalendarLuis.Intent.FindMeetingRoom:
                    case CalendarLuis.Intent.CreateCalendarEntry:
                        {
                            state.MeetingInfo.CreateHasDetail = false;
                            if (entity.Subject != null)
                            {
                                state.MeetingInfo.CreateHasDetail = true;
                                state.MeetingInfo.Title = GetSubjectFromEntity(entity);
                            }

                            if (entity.personName != null)
                            {
                                state.MeetingInfo.CreateHasDetail = true;
                                state.MeetingInfo.ContactInfor.ContactsNameList = GetAttendeesFromEntity(entity, luisResultText, state.MeetingInfo.ContactInfor.ContactsNameList);
                            }

                            if (entity.RelationshipName != null)
                            {
                                state.MeetingInfo.CreateHasDetail = true;
                                state.MeetingInfo.ContactInfor.RelatedEntityInfoDict = GetRelatedEntityFromRelationship(entity, luisResultText);
                                if (state.MeetingInfo.ContactInfor.ContactsNameList == null)
                                {
                                    state.MeetingInfo.ContactInfor.ContactsNameList = new List<string>();
                                }

                                state.MeetingInfo.ContactInfor.ContactsNameList.AddRange(state.MeetingInfo.ContactInfor.RelatedEntityInfoDict.Keys);
                            }

                            if (entity.FromDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResultText, entity._instance.FromDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (date != null)
                                {
                                    state.MeetingInfo.CreateHasDetail = true;
                                    state.MeetingInfo.StartDate = date;
                                }

                                // get end date from time range
                                date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (date != null)
                                {
                                    state.MeetingInfo.CreateHasDetail = true;
                                    state.MeetingInfo.EndDate = date;
                                }
                            }

                            if (entity.ToDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResultText, entity._instance.ToDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, false);
                                if (date != null)
                                {
                                    state.MeetingInfo.CreateHasDetail = true;
                                    state.MeetingInfo.EndDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResultText, entity._instance.FromTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (time != null)
                                {
                                    state.MeetingInfo.CreateHasDetail = true;
                                    state.MeetingInfo.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (time != null)
                                {
                                    state.MeetingInfo.CreateHasDetail = true;
                                    state.MeetingInfo.EndTime = time;
                                }
                            }

                            if (entity.ToTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResultText, entity._instance.ToTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, false);
                                if (time != null)
                                {
                                    state.MeetingInfo.CreateHasDetail = true;
                                    state.MeetingInfo.EndTime = time;
                                }
                            }

                            if (entity.Duration != null)
                            {
                                var duration = GetDurationFromEntity(entity, dc.Context.Activity.Locale, state.GetUserTimeZone());
                                if (duration != -1)
                                {
                                    state.MeetingInfo.CreateHasDetail = true;
                                    state.MeetingInfo.Duration = duration;
                                }
                            }

                            if (entity.MeetingRoom != null || entity.MeetingRoomPatternAny != null)
                            {
                                state.MeetingInfo.CreateHasDetail = true;
                                state.MeetingInfo.MeetingRoomName = state.MeetingInfo.Location = GetMeetingRoomFromEntity(entity);
                            }

                            if (entity.Building != null)
                            {
                                state.MeetingInfo.CreateHasDetail = true;
                                state.MeetingInfo.Building = GetBuildingFromEntity(entity);
                            }

                            if (entity.FloorNumber != null)
                            {
                                state.MeetingInfo.CreateHasDetail = true;
                                state.MeetingInfo.FloorNumber = GetFloorNumberFromEntity(entity, dc.Context.Activity.Locale);
                            }

                            if (entity.Location != null)
                            {
                                state.MeetingInfo.CreateHasDetail = true;
                                state.MeetingInfo.Location = GetLocationFromEntity(entity);
                            }

                            break;
                        }

                    case CalendarLuis.Intent.CheckAvailability:
                    case CalendarLuis.Intent.ConnectToMeeting:
                    case CalendarLuis.Intent.TimeRemaining:
                    case CalendarLuis.Intent.AcceptEventEntry:
                    case CalendarLuis.Intent.DeleteCalendarEntry:
                        {
                            if (entity.OrderReference != null)
                            {
                                state.MeetingInfo.OrderReference = GetOrderReferenceFromEntity(entity);
                            }

                            if (entity.personName != null)
                            {
                                state.MeetingInfo.CreateHasDetail = true;
                                state.MeetingInfo.ContactInfor.ContactsNameList = GetAttendeesFromEntity(entity, luisResultText, state.MeetingInfo.ContactInfor.ContactsNameList);
                            }

                            if (entity.Subject != null)
                            {
                                state.MeetingInfo.Title = GetSubjectFromEntity(entity);
                            }

                            if (entity.personName != null)
                            {
                                state.MeetingInfo.ContactInfor.ContactsNameList = GetAttendeesFromEntity(entity, luisResult.Text, state.MeetingInfo.ContactInfor.ContactsNameList);
                            }

                            if (entity.FromDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResultText, entity._instance.FromDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (date != null)
                                {
                                    state.MeetingInfo.StartDate = date;
                                }

                                date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (date != null)
                                {
                                    state.MeetingInfo.EndDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResultText, entity._instance.FromTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (time != null)
                                {
                                    state.MeetingInfo.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (time != null)
                                {
                                    state.MeetingInfo.EndTime = time;
                                }
                            }

                            if (entity.RelationshipName != null)
                            {
                                state.MeetingInfo.CreateHasDetail = true;
                                state.MeetingInfo.ContactInfor.RelatedEntityInfoDict = GetRelatedEntityFromRelationship(entity, luisResultText);
                                if (state.MeetingInfo.ContactInfor.ContactsNameList == null)
                                {
                                    state.MeetingInfo.ContactInfor.ContactsNameList = new List<string>();
                                }

                                state.MeetingInfo.ContactInfor.ContactsNameList.AddRange(state.MeetingInfo.ContactInfor.RelatedEntityInfoDict.Keys);
                            }

                            if (entity.MeetingRoom != null || entity.MeetingRoomPatternAny != null)
                            {
                                state.MeetingInfo.CreateHasDetail = true;
                                state.MeetingInfo.MeetingRoomName = state.MeetingInfo.Location = GetMeetingRoomFromEntity(entity);
                            }

                            if (entity.Building != null)
                            {
                                state.MeetingInfo.CreateHasDetail = true;
                                state.MeetingInfo.Building = GetBuildingFromEntity(entity);
                            }

                            if (entity.FloorNumber != null)
                            {
                                state.MeetingInfo.CreateHasDetail = true;
                                state.MeetingInfo.FloorNumber = GetFloorNumberFromEntity(entity, dc.Context.Activity.Locale);
                            }

                            break;
                        }

                    case CalendarLuis.Intent.ChangeCalendarEntry:
                        {
                            if (entity.Subject != null)
                            {
                                state.MeetingInfo.Title = GetSubjectFromEntity(entity);
                            }

                            if (entity.personName != null)
                            {
                                state.MeetingInfo.CreateHasDetail = true;
                                state.MeetingInfo.ContactInfor.ContactsNameList = GetAttendeesFromEntity(entity, luisResultText, state.MeetingInfo.ContactInfor.ContactsNameList);
                            }

                            if (entity.FromDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResultText, entity._instance.FromDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (date != null)
                                {
                                    state.MeetingInfo.StartDate = date;
                                }

                                date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (date != null)
                                {
                                    state.MeetingInfo.EndDate = date;
                                }
                            }

                            if (entity.ToDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResultText, entity._instance.ToDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (date != null)
                                {
                                    state.UpdateMeetingInfo.NewStartDate = date;
                                }

                                date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (date != null)
                                {
                                    state.UpdateMeetingInfo.NewEndDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResultText, entity._instance.FromTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (time != null)
                                {
                                    state.MeetingInfo.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (time != null)
                                {
                                    state.MeetingInfo.EndTime = time;
                                }
                            }

                            if (entity.ToTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResultText, entity._instance.ToTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (time != null)
                                {
                                    state.UpdateMeetingInfo.NewStartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (time != null)
                                {
                                    state.UpdateMeetingInfo.NewEndTime = time;
                                }
                            }

                            if (entity.MoveEarlierTimeSpan != null)
                            {
                                state.UpdateMeetingInfo.MoveTimeSpan = GetMoveTimeSpanFromEntity(entity.MoveEarlierTimeSpan[0], dc.Context.Activity.Locale, false, state.GetUserTimeZone());
                            }

                            if (entity.MoveLaterTimeSpan != null)
                            {
                                state.UpdateMeetingInfo.MoveTimeSpan = GetMoveTimeSpanFromEntity(entity.MoveLaterTimeSpan[0], dc.Context.Activity.Locale, true, state.GetUserTimeZone());
                            }

                            if (entity.datetime != null)
                            {
                                var match = entity._instance.datetime.ToList().Find(w => w.Text.ToLower() == CalendarCommonStrings.DailyToken
                                || w.Text.ToLower() == CalendarCommonStrings.WeeklyToken
                                || w.Text.ToLower() == CalendarCommonStrings.MonthlyToken);
                                if (match != null)
                                {
                                    state.UpdateMeetingInfo.RecurrencePattern = match.Text.ToLower();
                                }
                            }

                            if (entity.RelationshipName != null)
                            {
                                state.MeetingInfo.CreateHasDetail = true;
                                state.MeetingInfo.ContactInfor.RelatedEntityInfoDict = GetRelatedEntityFromRelationship(entity, luisResultText);
                                if (state.MeetingInfo.ContactInfor.ContactsNameList == null)
                                {
                                    state.MeetingInfo.ContactInfor.ContactsNameList = new List<string>();
                                }

                                state.MeetingInfo.ContactInfor.ContactsNameList.AddRange(state.MeetingInfo.ContactInfor.RelatedEntityInfoDict.Keys);
                            }

                            break;
                        }

                    case CalendarLuis.Intent.AddCalendarEntryAttribute:
                        {
                            if (entity.OrderReference != null)
                            {
                                state.MeetingInfo.OrderReference = GetOrderReferenceFromEntity(entity);
                            }

                            if (entity.personName != null)
                            {
                                state.MeetingInfo.CreateHasDetail = true;
                                state.MeetingInfo.ContactInfor.ContactsNameList = GetAttendeesFromEntity(entity, luisResultText, state.MeetingInfo.ContactInfor.ContactsNameList);
                            }

                            if (entity.Subject != null)
                            {
                                state.MeetingInfo.Title = GetSubjectFromEntity(entity);
                            }

                            if (entity.personName != null)
                            {
                                state.MeetingInfo.ContactInfor.ContactsNameList = GetAttendeesFromEntity(entity, luisResult.Text, state.MeetingInfo.ContactInfor.ContactsNameList);
                            }

                            if (entity.FromDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResultText, entity._instance.FromDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (date != null)
                                {
                                    state.MeetingInfo.StartDate = date;
                                }

                                date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (date != null)
                                {
                                    state.MeetingInfo.EndDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResultText, entity._instance.FromTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (time != null)
                                {
                                    state.MeetingInfo.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (time != null)
                                {
                                    state.MeetingInfo.EndTime = time;
                                }
                            }

                            if (entity.ToDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResultText, entity._instance.ToDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (date != null && state.MeetingInfo.StartDate.Count == 0)
                                {
                                    state.MeetingInfo.StartDate = date;
                                }

                                date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (date != null && state.MeetingInfo.EndDate.Count == 0)
                                {
                                    state.MeetingInfo.EndDate = date;
                                }
                            }

                            if (entity.ToTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResultText, entity._instance.ToTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (time != null && state.MeetingInfo.StartTime.Count == 0)
                                {
                                    state.MeetingInfo.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (time != null && state.MeetingInfo.EndTime.Count == 0)
                                {
                                    state.MeetingInfo.EndTime = time;
                                }
                            }

                            if (entity.MeetingRoom != null || entity.MeetingRoomPatternAny != null)
                            {
                                state.MeetingInfo.CreateHasDetail = true;
                                state.MeetingInfo.MeetingRoomName = state.MeetingInfo.Location = GetMeetingRoomFromEntity(entity);
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
                                state.MeetingInfo.OrderReference = GetOrderReferenceFromEntity(entity);
                            }

                            if (entity.Subject != null)
                            {
                                state.MeetingInfo.Title = GetSubjectFromEntity(entity);
                            }

                            if (entity.personName != null)
                            {
                                state.MeetingInfo.ContactInfor.ContactsNameList = GetAttendeesFromEntity(entity, luisResultText, state.MeetingInfo.ContactInfor.ContactsNameList);
                            }

                            if (entity.Location != null)
                            {
                                state.MeetingInfo.Location = GetLocationFromEntity(entity);
                            }

                            if (entity.FromDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResultText, entity._instance.FromDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (date != null)
                                {
                                    state.MeetingInfo.StartDate = date;
                                    state.MeetingInfo.StartDateString = dateString;
                                }

                                date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (date != null)
                                {
                                    state.MeetingInfo.EndDate = date;
                                }
                            }

                            if (entity.ToDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResultText, entity._instance.ToDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, false);
                                if (date != null)
                                {
                                    state.MeetingInfo.EndDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResultText, entity._instance.FromTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), true, false);
                                if (time != null)
                                {
                                    state.MeetingInfo.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, true);
                                if (time != null)
                                {
                                    state.MeetingInfo.EndTime = time;
                                }
                            }

                            if (entity.ToTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResultText, entity._instance.ToTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, state.GetUserTimeZone(), false, false);
                                if (time != null)
                                {
                                    state.MeetingInfo.EndTime = time;
                                }
                            }

                            if (entity.RelationshipName != null)
                            {
                                state.MeetingInfo.CreateHasDetail = true;
                                state.MeetingInfo.ContactInfor.RelatedEntityInfoDict = GetRelatedEntityFromRelationship(entity, luisResultText);
                                if (state.MeetingInfo.ContactInfor.ContactsNameList == null)
                                {
                                    state.MeetingInfo.ContactInfor.ContactsNameList = new List<string>();
                                }

                                state.MeetingInfo.ContactInfor.ContactsNameList.AddRange(state.MeetingInfo.ContactInfor.RelatedEntityInfoDict.Keys);
                            }

                            state.ShowMeetingInfo.AskParameterContent = luisResultText;

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
            var activity = TemplateEngine.GenerateActivityForLocale(CalendarSharedResponses.CalendarErrorMessage);
            await sc.Context.SendActivityAsync(activity);

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
            if (ex.ExceptionType == SkillExceptionType.APIAccessDenied || ex.ExceptionType == SkillExceptionType.APIUnauthorized || ex.ExceptionType == SkillExceptionType.APIForbidden || ex.ExceptionType == SkillExceptionType.APIBadRequest)
            {
                var activity = TemplateEngine.GenerateActivityForLocale(CalendarSharedResponses.CalendarErrorMessageAccountProblem);
                await sc.Context.SendActivityAsync(activity);
            }
            else
            {
                var activity = TemplateEngine.GenerateActivityForLocale(CalendarSharedResponses.CalendarErrorMessage);
                await sc.Context.SendActivityAsync(activity);
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
            var unionList = state.MeetingInfo.ContactInfor.ContactsNameList.ToList();
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

                    if (mailAddress.Equals(formattedMailAddress, StringComparison.OrdinalIgnoreCase))
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
            sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
            var service = ServiceManager.InitUserService(token as string, state.EventSource);

            // Get users.
            result = await service.GetContactsAsync(name);
            return result;
        }

        protected async Task<List<PersonModel>> GetPeopleWorkWithAsync(WaterfallStepContext sc, string name)
        {
            var result = new List<PersonModel>();
            var state = await Accessor.GetAsync(sc.Context);
            sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
            var service = ServiceManager.InitUserService(token as string, state.EventSource);

            // Get users.
            result = await service.GetPeopleAsync(name);

            return result;
        }

        protected async Task<List<PersonModel>> GetUserAsync(WaterfallStepContext sc, string name)
        {
            var result = new List<PersonModel>();
            var state = await Accessor.GetAsync(sc.Context);
            sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
            var service = ServiceManager.InitUserService(token as string, state.EventSource);

            // Get users.
            result = await service.GetUserAsync(name);

            return result;
        }

        protected async Task<PersonModel> GetMyManager(WaterfallStepContext sc)
        {
            var state = await Accessor.GetAsync(sc.Context);
            sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
            var service = ServiceManager.InitUserService(token as string, state.EventSource);
            return await service.GetMyManagerAsync();
        }

        protected async Task<PersonModel> GetManager(WaterfallStepContext sc, string name)
        {
            var state = await Accessor.GetAsync(sc.Context);
            sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
            var service = ServiceManager.InitUserService(token as string, state.EventSource);
            return await service.GetManagerAsync(name);
        }

        protected async Task<PersonModel> GetMe(ITurnContext context)
        {
            var state = await Accessor.GetAsync(context);
            context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
            var service = ServiceManager.InitUserService(token as string, state.EventSource);
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

        protected string GetMeetingRoomFromEntity(CalendarLuis._Entities entity)
        {
            if (entity.MeetingRoom != null)
            {
                return entity.MeetingRoom[0];
            }

            return entity.MeetingRoomPatternAny[0];
        }

        protected string GetBuildingFromEntity(CalendarLuis._Entities entity)
        {
            return entity.Building[0];
        }

        protected string GetSlotAttributeFromEntity(CalendarLuis._Entities entity)
        {
            return entity.SlotAttribute[0];
        }

        protected int? ParseFloorNumber(string utterance, string local)
        {
            var culture = local ?? English;
            var model_ordinal = new NumberRecognizer(culture).GetOrdinalModel(culture);
            var result = model_ordinal.Parse(utterance);
            if (result.Any())
            {
                return int.Parse(result.First().Resolution["value"].ToString());
            }
            else
            {
                var model_number = new NumberRecognizer(culture).GetNumberModel(culture);
                result = model_number.Parse(utterance);
                if (result.Any())
                {
                    return int.Parse(result.First().Resolution["value"].ToString());
                }
            }

            return null;
        }

        protected string GetDateTimeStringFromInstanceData(string inputString, InstanceData data)
        {
            return inputString.Substring(data.StartIndex, data.EndIndex - data.StartIndex);
        }

        protected List<DateTime> GetDateFromDateTimeString(string date, string local, TimeZoneInfo userTimeZone, bool isStart, bool isTargetTimeRange)
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

        protected List<DateTime> GetTimeFromDateTimeString(string time, string local, TimeZoneInfo userTimeZone, bool isStart, bool isTargetTimeRange)
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

        private async Task<List<object>> GetMeetingCardListAsync(DialogContext dc, List<EventModel> events)
        {
            var state = await Accessor.GetAsync(dc.Context);

            var eventItemList = new List<object>();

            DateTime? currentAddedDateUser = null;
            foreach (var item in events)
            {
                var itemDateUser = TimeConverter.ConvertUtcToUserTime(item.StartTime, state.GetUserTimeZone());
                if (currentAddedDateUser == null || !currentAddedDateUser.Value.Date.Equals(itemDateUser.Date))
                {
                    currentAddedDateUser = itemDateUser;
                    eventItemList.Add(new
                    {
                        Name = "CalendarDate",
                        Date = item.StartTime
                    });
                }

                eventItemList.Add(new
                {
                    Name = "CalendarItem",
                    Event = item
                });
            }

            return eventItemList;
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

        private Dictionary<string, CalendarSkillState.RelatedEntityInfo> GetRelatedEntityFromRelationship(CalendarLuis._Entities entity, string inputString)
        {
            var entities = new Dictionary<string, CalendarSkillState.RelatedEntityInfo>();

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
            if (Microsoft.Bot.Builder.Dialogs.Choices.Channel.GetChannelId(turnContext) == Channels.Msteams)
            {
                return card + "V1";
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
            firstIndex = state.ShowMeetingInfo.ShowEventIndex * state.PageSize;
            var count = Math.Min(state.PageSize, state.ShowMeetingInfo.ShowingMeetings.Count - (state.ShowMeetingInfo.ShowEventIndex * state.PageSize));
            lastIndex = firstIndex + count;
            return state.ShowMeetingInfo.ShowingMeetings.GetRange(firstIndex, count);
        }

        private async Task<string> GetPhotoByIndexAsync(ITurnContext context, List<EventModel.Attendee> attendees, int index)
        {
            if (attendees.Count <= index)
            {
                return AdaptiveCardHelper.BlankIcon;
            }

            return await GetUserPhotoUrlAsync(context, attendees[index]);
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

        private string GetLocationFromEntity(CalendarLuis._Entities entity)
        {
            return entity.Location[0];
        }

        private string GetOrderReferenceFromEntity(CalendarLuis._Entities entity)
        {
            return entity.OrderReference[0];
        }

        private int? GetFloorNumberFromEntity(CalendarLuis._Entities entity, string culture)
        {
            return ParseFloorNumber(entity.FloorNumber[0], culture);
        }
    }
}