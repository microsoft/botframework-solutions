// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Prompts;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.CreateEvent;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Graph;
using static CalendarSkill.Models.CreateEventStateModel;

namespace CalendarSkill.Dialogs
{
    public class CreateEventDialog : CalendarSkillDialogBase
    {
        public CreateEventDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            FindContactDialog findContactDialog,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(CreateEventDialog), settings, services, conversationState, localeTemplateEngineManager, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            var createEvent = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                CollectAttendees,
                CollectTitle,
                CollectContent,
                CollectStartDate,
                CollectStartTime,
                CollectDuration,
                CollectLocation,
                GetAuthToken,
                AfterGetAuthToken,
                ShowEventInfo,
                ConfirmBeforeCreatePrompt,
                AfterConfirmBeforeCreatePrompt,
                GetAuthToken,
                AfterGetAuthToken,
                CreateEvent,
            };

            var collectTitle = new WaterfallStep[]
            {
                CollectTitlePrompt,
                AfterCollectTitlePrompt
            };

            var collectContent = new WaterfallStep[]
            {
                CollectContentPrompt,
                AfterCollectContentPrompt
            };

            var updateStartDate = new WaterfallStep[]
            {
                UpdateStartDateForCreate,
                AfterUpdateStartDateForCreate,
            };

            var updateStartTime = new WaterfallStep[]
            {
                UpdateStartTimeForCreate,
                AfterUpdateStartTimeForCreate,
            };

            var collectLocation = new WaterfallStep[]
            {
                CollectMeetingRoomPrompt,
                AfterCollectMeetingRoomPrompt,
                CollectLocationPrompt,
                AfterCollectLocationPrompt
            };

            var updateDuration = new WaterfallStep[]
            {
                UpdateDurationForCreate,
                AfterUpdateDurationForCreate,
            };

            var getRecreateInfo = new WaterfallStep[]
            {
                GetRecreateInfo,
                AfterGetRecreateInfo,
            };

            var showRestParticipants = new WaterfallStep[]
            {
                ShowRestParticipantsPrompt,
                AfterShowRestParticipantsPrompt,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CreateEvent, createEvent) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectTitle, collectTitle) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectContent, collectContent) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectLocation, collectLocation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateStartDateForCreate, updateStartDate) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateStartTimeForCreate, updateStartTime) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateDurationForCreate, updateDuration) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.GetRecreateInfo, getRecreateInfo) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowRestParticipants, showRestParticipants) { TelemetryClient = telemetryClient });
            AddDialog(new DatePrompt(Actions.DatePromptForCreate));
            AddDialog(new TimePrompt(Actions.TimePromptForCreate));
            AddDialog(new DurationPrompt(Actions.DurationPromptForCreate));
            AddDialog(new GetRecreateInfoPrompt(Actions.GetRecreateInfoPrompt));
            AddDialog(findContactDialog ?? throw new ArgumentNullException(nameof(findContactDialog)));

            // Set starting dialog for component
            InitialDialogId = Actions.CreateEvent;
        }

        // Create Event waterfall steps
        private async Task<DialogTurnResult> CollectTitle(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectTitle, sc.Options);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectTitlePrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isTitleSkipByDefault = false;
                isTitleSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventTitle")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.MeetingInfo.RecreateState == RecreateEventState.Subject)
                {
                    var prompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.NoTitleShort) as Activity;
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
                }
                else if (state.MeetingInfo.CreateHasDetail && isTitleSkipByDefault.GetValueOrDefault())
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else if (string.IsNullOrEmpty(state.MeetingInfo.Title))
                {
                    if (state.MeetingInfo.ContactInfor.Contacts.Count == 0 || state.MeetingInfo.ContactInfor.Contacts == null)
                    {
                        state.Clear();
                        return await sc.EndDialogAsync(true);
                    }

                    var userNameString = state.MeetingInfo.ContactInfor.Contacts.ToSpeechString(CommonStrings.And, li => $"{li.DisplayName ?? li.Address}");
                    var data = new { UserName = userNameString };
                    var prompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.NoTitle, data) as Activity;

                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
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

        private async Task<DialogTurnResult> AfterCollectTitlePrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isTitleSkipByDefault = false;
                isTitleSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventTitle")?.IsSkipByDefault;
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (sc.Result != null || (state.MeetingInfo.CreateHasDetail && isTitleSkipByDefault.GetValueOrDefault()) || state.MeetingInfo.RecreateState == RecreateEventState.Subject)
                {
                    if (string.IsNullOrEmpty(state.MeetingInfo.Title))
                    {
                        if (state.MeetingInfo.CreateHasDetail && isTitleSkipByDefault.GetValueOrDefault() && state.MeetingInfo.RecreateState != RecreateEventState.Subject)
                        {
                            state.MeetingInfo.Title = CreateEventWhiteList.GetDefaultTitle();
                        }
                        else
                        {
                            sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                            var title = content != null ? content.ToString() : sc.Context.Activity.Text;
                            if (CreateEventWhiteList.IsSkip(title))
                            {
                                state.MeetingInfo.Title = CreateEventWhiteList.GetDefaultTitle();
                            }
                            else
                            {
                                state.MeetingInfo.Title = title;
                            }
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

        private async Task<DialogTurnResult> CollectContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectContent, sc.Options);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectContentPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isContentSkipByDefault = false;
                isContentSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventContent")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (string.IsNullOrEmpty(state.MeetingInfo.Content) && (!(state.MeetingInfo.CreateHasDetail && isContentSkipByDefault.GetValueOrDefault()) || state.MeetingInfo.RecreateState == RecreateEventState.Content))
                {
                    var prompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.NoContent) as Activity;
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
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

        private async Task<DialogTurnResult> AfterCollectContentPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isContentSkipByDefault = false;
                isContentSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventContent")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (sc.Result != null && (!(state.MeetingInfo.CreateHasDetail && isContentSkipByDefault.GetValueOrDefault()) || state.MeetingInfo.RecreateState == RecreateEventState.Content))
                {
                    if (string.IsNullOrEmpty(state.MeetingInfo.Content))
                    {
                        sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                        var merged_content = content != null ? content.ToString() : sc.Context.Activity.Text;
                        if (!CreateEventWhiteList.IsSkip(merged_content))
                        {
                            state.MeetingInfo.Content = merged_content;
                        }
                    }
                }
                else if (state.MeetingInfo.CreateHasDetail && isContentSkipByDefault.GetValueOrDefault())
                {
                    state.MeetingInfo.Content = CalendarCommonStrings.DefaultContent;
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectAttendees(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.MeetingInfo.ContactInfor.Contacts.Count == 0 || state.MeetingInfo.RecreateState == RecreateEventState.Participants)
                {
                    return await sc.BeginDialogAsync(nameof(FindContactDialog), options: new FindContactDialogOptions(sc.Options), cancellationToken: cancellationToken);
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

        private async Task<DialogTurnResult> CollectLocation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfo.Location == null && state.MeetingInfo.MeetingRoom == null)
                {
                    return await sc.BeginDialogAsync(Actions.CollectLocation, sc.Options);
                }
                else
                {
                    state.MeetingInfo.Location = state.MeetingInfo.Location ?? state.MeetingInfo.MeetingRoom.DisplayName;
                    return await sc.NextAsync(cancellationToken: cancellationToken);
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

        private async Task<DialogTurnResult> CollectMeetingRoomPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfo.RecreateState == RecreateEventState.MeetingRoom || string.IsNullOrEmpty(Settings.AzureSearch?.SearchServiceName))
                {
                    return await sc.NextAsync();
                }
                else
                {
                    var prompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.NoMeetingRoom);
                    return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = prompt }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterCollectMeetingRoomPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfo.RecreateState == RecreateEventState.MeetingRoom || (sc.Result != null && (bool)sc.Result == true))
                {
                    return await sc.BeginDialogAsync(nameof(FindMeetingRoomDialog), options: sc.Options, cancellationToken: cancellationToken);
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectLocationPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isLocationSkipByDefault = false;
                isLocationSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventLocation")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfo.MeetingRoom != null)
                {
                    state.MeetingInfo.Location = state.MeetingInfo.MeetingRoom.DisplayName;
                    return await sc.EndDialogAsync();
                }
                else if (state.MeetingInfo.Location == null && (!(state.MeetingInfo.CreateHasDetail && isLocationSkipByDefault.GetValueOrDefault()) || state.MeetingInfo.RecreateState == RecreateEventState.Location))
                {
                    var prompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.NoLocation) as Activity;
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
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

        private async Task<DialogTurnResult> AfterCollectLocationPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isLocationSkipByDefault = false;
                isLocationSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventLocation")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfo.Location == null && sc.Result != null && (!(state.MeetingInfo.CreateHasDetail && isLocationSkipByDefault.GetValueOrDefault()) || state.MeetingInfo.RecreateState == RecreateEventState.Location))
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var luisResult = sc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);

                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                    var topIntent = luisResult?.TopIntent().intent.ToString();

                    var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);

                    // Enable the user to skip providing the location if they say something matching the Cancel intent, say something matching the ConfirmNo recognizer or something matching the NoLocation intent
                    if (CreateEventWhiteList.IsSkip(userInput))
                    {
                        state.MeetingInfo.Location = string.Empty;
                    }
                    else
                    {
                        state.MeetingInfo.Location = userInput;
                    }
                }
                else if (state.MeetingInfo.CreateHasDetail && isLocationSkipByDefault.GetValueOrDefault())
                {
                    state.MeetingInfo.Location = CalendarCommonStrings.DefaultLocation;
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> ShowEventInfo(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // show event information before create
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                var source = state.EventSource;
                var newEvent = new EventModel(source)
                {
                    Title = state.MeetingInfo.Title,
                    Content = state.MeetingInfo.Content,
                    Attendees = state.MeetingInfo.ContactInfor.Contacts,
                    StartTime = state.MeetingInfo.StartDateTime.Value,
                    EndTime = state.MeetingInfo.EndDateTime.Value,
                    TimeZone = TimeZoneInfo.Utc,
                    Location = state.MeetingInfo.Location,
                    ContentPreview = state.MeetingInfo.Content
                };

                var attendeeConfirmTextString = string.Empty;
                if (state.MeetingInfo.ContactInfor.Contacts.Count > 0)
                {
                    var attendeeConfirmResponse = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.ConfirmCreateAttendees, new
                    {
                        Attendees = DisplayHelper.ToDisplayParticipantsStringSummary(state.MeetingInfo.ContactInfor.Contacts, 5)
                    });
                    attendeeConfirmTextString = attendeeConfirmResponse.Text;
                }

                var subjectConfirmString = string.Empty;
                if (!string.IsNullOrEmpty(state.MeetingInfo.Title))
                {
                    var subjectConfirmResponse = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.ConfirmCreateSubject, new
                    {
                        Subject = string.IsNullOrEmpty(state.MeetingInfo.Title) ? CalendarCommonStrings.Empty : state.MeetingInfo.Title
                    });
                    subjectConfirmString = subjectConfirmResponse.Text;
                }

                var locationConfirmString = string.Empty;
                if (!string.IsNullOrEmpty(state.MeetingInfo.Location))
                {
                    var subjectConfirmResponse = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.ConfirmCreateLocation, new
                    {
                        Location = string.IsNullOrEmpty(state.MeetingInfo.Location) ? CalendarCommonStrings.Empty : state.MeetingInfo.Location
                    });
                    locationConfirmString = subjectConfirmResponse.Text;
                }

                var contentConfirmString = string.Empty;
                if (!string.IsNullOrEmpty(state.MeetingInfo.Content))
                {
                    var contentConfirmResponse = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.ConfirmCreateContent, new
                    {
                        Content = string.IsNullOrEmpty(state.MeetingInfo.Content) ? CalendarCommonStrings.Empty : state.MeetingInfo.Content
                    });
                    contentConfirmString = contentConfirmResponse.Text;
                }

                var startDateTimeInUserTimeZone = TimeConverter.ConvertUtcToUserTime(state.MeetingInfo.StartDateTime.Value, state.GetUserTimeZone());
                var endDateTimeInUserTimeZone = TimeConverter.ConvertUtcToUserTime(state.MeetingInfo.EndDateTime.Value, state.GetUserTimeZone());
                var tokens = new
                {
                    AttendeesConfirm = attendeeConfirmTextString,
                    Date = startDateTimeInUserTimeZone.ToSpeechDateString(false),
                    Time = startDateTimeInUserTimeZone.ToSpeechTimeString(false),
                    EndTime = endDateTimeInUserTimeZone.ToSpeechTimeString(false),
                    SubjectConfirm = subjectConfirmString,
                    LocationConfirm = locationConfirmString,
                    ContentConfirm = contentConfirmString
                };

                var prompt = await GetDetailMeetingResponseAsync(sc, newEvent, CreateEventResponses.ConfirmCreate, tokens);

                await sc.Context.SendActivityAsync(prompt);

                // show at most 5 user names, ask user show rest users
                if (state.MeetingInfo.ContactInfor.Contacts.Count > 5)
                {
                    return await sc.BeginDialogAsync(Actions.ShowRestParticipants);
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> ConfirmBeforeCreatePrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.ConfirmCreatePrompt) as Activity,
                    RetryPrompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.ConfirmCreateFailed) as Activity
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterConfirmBeforeCreatePrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    return await sc.NextAsync();
                }
                else
                {
                    // if user not create, ask if user want to change any field
                    return await sc.ReplaceDialogAsync(Actions.GetRecreateInfo, options: sc.Options, cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CreateEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var source = state.EventSource;

                if (state.MeetingInfo.MeetingRoom != null)
                {
                    state.MeetingInfo.ContactInfor.Contacts.Add(new EventModel.Attendee
                    {
                        DisplayName = state.MeetingInfo.MeetingRoom.DisplayName,
                        Address = state.MeetingInfo.MeetingRoom.EmailAddress,
                        AttendeeType = AttendeeType.Resource
                    });
                }

                var newEvent = new EventModel(source)
                {
                    Title = state.MeetingInfo.Title,
                    Content = state.MeetingInfo.Content,
                    Attendees = state.MeetingInfo.ContactInfor.Contacts,
                    StartTime = (DateTime)state.MeetingInfo.StartDateTime,
                    EndTime = (DateTime)state.MeetingInfo.EndDateTime,
                    TimeZone = TimeZoneInfo.Utc,
                    Location = state.MeetingInfo.MeetingRoom == null ? state.MeetingInfo.Location : null,
                };

                sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
                var calendarService = ServiceManager.InitCalendarService(token as string, state.EventSource);
                if (await calendarService.CreateEventAysnc(newEvent) != null)
                {
                    var activity = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.MeetingBooked);
                    await sc.Context.SendActivityAsync(activity);
                }
                else
                {
                    var prompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.EventCreationFailed) as Activity;
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
                }

                state.Clear();

                return await sc.EndDialogAsync(true, cancellationToken);
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

        private async Task<DialogTurnResult> GetRecreateInfo(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.GetRecreateInfoPrompt, new CalendarPromptOptions
                {
                    Prompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.GetRecreateInfo) as Activity,
                    RetryPrompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.GetRecreateInfoRetry) as Activity,
                    MaxReprompt = CalendarCommonUtil.MaxRepromptCount
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterGetRecreateInfo(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (sc.Result != null)
                {
                    var recreateState = sc.Result as RecreateEventState?;
                    switch (recreateState.Value)
                    {
                        case RecreateEventState.Cancel:
                            var activity = TemplateEngine.GenerateActivityForLocale(CalendarSharedResponses.ActionEnded);
                            await sc.Context.SendActivityAsync(activity);
                            state.Clear();
                            return await sc.EndDialogAsync(true, cancellationToken);
                        case RecreateEventState.Time:
                            state.MeetingInfo.ClearTimesForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Duration:
                            state.MeetingInfo.ClearEndTimesAndDurationForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Location:
                            state.MeetingInfo.ClearLocationForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.MeetingRoom:
                            state.MeetingInfo.ClearMeetingRoomForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Participants:
                            state.MeetingInfo.ClearParticipantsForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Subject:
                            state.MeetingInfo.ClearSubjectForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Content:
                            state.MeetingInfo.ClearContentForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        default:
                            // should not go to this part. place an error handling for save.
                            await HandleDialogExceptions(sc, new Exception("Get unexpect state in recreate."));
                            return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
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
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> ShowRestParticipantsPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.ShowRestParticipantsPrompt) as Activity,
                    RetryPrompt = TemplateEngine.GenerateActivityForLocale(CreateEventResponses.ShowRestParticipantsPrompt) as Activity,
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterShowRestParticipantsPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    await sc.Context.SendActivityAsync(state.MeetingInfo.ContactInfor.Contacts.GetRange(5, state.MeetingInfo.ContactInfor.Contacts.Count - 5).ToSpeechString(CommonStrings.And, li => li.DisplayName ?? li.Address));
                }

                return await sc.EndDialogAsync(true, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}