using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Common;
using CalendarSkill.Dialogs.CreateEvent.Prompts;
using CalendarSkill.Dialogs.CreateEvent.Prompts.Options;
using CalendarSkill.Dialogs.CreateEvent.Resources;
using CalendarSkill.Dialogs.Shared;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Recognizers.Text.DateTime;
using static CalendarSkill.Models.CreateEventStateModel;

namespace CalendarSkill.Dialogs.CreateEvent
{
    public class CreateEventDialog : CalendarSkillDialog
    {
        public CreateEventDialog(
            SkillConfigurationBase services,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(CreateEventDialog), services, accessor, serviceManager, telemetryClient)
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
                ConfirmBeforeCreate,
                CreateEvent,
            };

            var updateAddress = new WaterfallStep[]
            {
                UpdateAddress,
                AfterUpdateAddress,
            };

            var confirmAttendee = new WaterfallStep[]
            {
                ConfirmAttendee,
                AfterConfirmAttendee,
            };

            var updateName = new WaterfallStep[]
            {
                UpdateUserName,
                AfterUpdateUserName,
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

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CreateEvent, createEvent) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateAddress, updateAddress) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ConfirmAttendee, confirmAttendee) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateName, updateName) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateStartDateForCreate, updateStartDate) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateStartTimeForCreate, updateStartTime) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateDurationForCreate, updateDuration) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.GetRecreateInfo, getRecreateInfo) { TelemetryClient = telemetryClient });
            AddDialog(new DatePrompt(Actions.DatePromptForCreate));
            AddDialog(new TimePrompt(Actions.TimePromptForCreate));
            AddDialog(new DurationPrompt(Actions.DurationPromptForCreate));
            AddDialog(new GetRecreateInfoPrompt(Actions.GetRecreateInfoPrompt));

            // Set starting dialog for component
            InitialDialogId = Actions.CreateEvent;
        }

        // Create Event waterfall steps
        public async Task<DialogTurnResult> CollectTitle(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.RecreateState == RecreateEventState.Subject)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoTitle_Short) }, cancellationToken);
                }
                else
                if (string.IsNullOrEmpty(state.Title) && !state.CreateHasDetail)
                {
                    var userNameString = state.Attendees.ToSpeechString(CommonStrings.And, li => li.DisplayName ?? li.Address);
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoTitle, ResponseBuilder, new StringDictionary() { { "UserName", userNameString } }) }, cancellationToken);
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

        public async Task<DialogTurnResult> CollectContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (sc.Result != null || state.CreateHasDetail || state.RecreateState == RecreateEventState.Subject)
                {
                    if (string.IsNullOrEmpty(state.Title))
                    {
                        if (state.CreateHasDetail && state.RecreateState != RecreateEventState.Subject)
                        {
                            state.Title = CreateEventWhiteList.GetDefaultTitle();
                        }
                        else
                        {
                            sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                            string title = content != null ? content.ToString() : sc.Context.Activity.Text;
                            if (CreateEventWhiteList.IsSkip(title))
                            {
                                state.Title = CreateEventWhiteList.GetDefaultTitle();
                            }
                            else
                            {
                                state.Title = title;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(state.Content) && (!state.CreateHasDetail || state.RecreateState == RecreateEventState.Content))
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoContent) }, cancellationToken);
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

        public async Task<DialogTurnResult> CollectAttendees(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (string.IsNullOrEmpty(state.APIToken))
                {
                    return await sc.EndDialogAsync(true, cancellationToken);
                }

                ServiceManager.InitCalendarService(state.APIToken, state.EventSource);

                if (state.Attendees.Count == 0 && (!state.CreateHasDetail || state.RecreateState == RecreateEventState.Participants))
                {
                    return await sc.BeginDialogAsync(Actions.UpdateAddress, cancellationToken: cancellationToken);
                }
                else
                {
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

        public async Task<DialogTurnResult> CollectStartDate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (sc.Result != null && (!state.CreateHasDetail || state.RecreateState == RecreateEventState.Content))
                {
                    if (string.IsNullOrEmpty(state.Content))
                    {
                        sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                        string merged_content = content != null ? content.ToString() : sc.Context.Activity.Text;
                        if (!CreateEventWhiteList.IsSkip(merged_content))
                        {
                            state.Content = merged_content;
                        }
                    }
                }

                if (!state.StartDate.Any())
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

        public async Task<DialogTurnResult> CollectStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.RecreateState == null || state.RecreateState == RecreateEventState.Time)
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

        public async Task<DialogTurnResult> CollectDuration(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.EndDateTime == null)
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

        public async Task<DialogTurnResult> CollectLocation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.Location == null && (!state.CreateHasDetail || state.RecreateState == RecreateEventState.Location))
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoLocation) }, cancellationToken);
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

        public async Task<DialogTurnResult> ConfirmBeforeCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.Location == null && sc.Result != null && (!state.CreateHasDetail || state.RecreateState == RecreateEventState.Location))
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var luisResult = state.LuisResult;

                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                    var topIntent = luisResult?.TopIntent().intent.ToString();

                    var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);

                    // Enable the user to skip providing the location if they say something matching the Cancel intent, say something matching the ConfirmNo recognizer or something matching the NoLocation intent
                    if (CreateEventWhiteList.IsSkip(userInput))
                    {
                        state.Location = string.Empty;
                    }
                    else
                    {
                        state.Location = userInput;
                    }
                }

                var source = state.EventSource;
                var newEvent = new EventModel(source)
                {
                    Title = state.Title,
                    Content = state.Content,
                    Attendees = state.Attendees,
                    StartTime = state.StartDateTime.Value,
                    EndTime = state.EndDateTime.Value,
                    TimeZone = TimeZoneInfo.Utc,
                    Location = state.Location,
                    ContentPreview = state.Content
                };

                var startDateTimeInUserTimeZone = TimeConverter.ConvertUtcToUserTime(state.StartDateTime.Value, state.GetUserTimeZone());
                var tokens = new StringDictionary
                {
                    { "Attendees", state.Attendees.ToSpeechString(CommonStrings.And, li => li.DisplayName ?? li.Address) },
                    { "Date", startDateTimeInUserTimeZone.ToSpeechDateString(true) },
                    { "Time", startDateTimeInUserTimeZone.ToSpeechTimeString(true) },
                };

                var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(CreateEventResponses.ConfirmCreate, newEvent.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", newEvent.ToAdaptiveCardData(state.GetUserTimeZone(), showContent: true), tokens: tokens);

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = replyMessage, RetryPrompt = sc.Context.Activity.CreateReply(CreateEventResponses.ConfirmCreateFailed, ResponseBuilder, tokens) }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> CreateEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var source = state.EventSource;
                    var newEvent = new EventModel(source)
                    {
                        Title = state.Title,
                        Content = state.Content,
                        Attendees = state.Attendees,
                        StartTime = (DateTime)state.StartDateTime,
                        EndTime = (DateTime)state.EndDateTime,
                        TimeZone = TimeZoneInfo.Utc,
                        Location = state.Location,
                    };

                    var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);
                    if (await calendarService.CreateEvent(newEvent) != null)
                    {
                        newEvent.ContentPreview = state.Content;
                        var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(CreateEventResponses.EventCreated, newEvent.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", newEvent.ToAdaptiveCardData(state.GetUserTimeZone(), showContent: true));
                        await sc.Context.SendActivityAsync(replyMessage, cancellationToken);
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CreateEventResponses.EventCreationFailed), cancellationToken);
                    }

                    state.Clear();
                }
                else
                {
                    return await sc.ReplaceDialogAsync(Actions.GetRecreateInfo, options: sc.Options, cancellationToken: cancellationToken);
                }

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

        // update address waterfall steps
        public async Task<DialogTurnResult> UpdateAddress(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.AttendeesNameList.Any())
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoAttendees) }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterUpdateAddress(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.AttendeesNameList.Any())
                {
                    return await sc.BeginDialogAsync(Actions.ConfirmAttendee, cancellationToken: cancellationToken);
                }

                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                    // TODO: can we do this somewhere else
                    if (IsEmail(userInput))
                    {
                        state.Attendees.Add(new EventModel.Attendee { Address = userInput });
                        return await sc.EndDialogAsync(true, cancellationToken);
                    }
                    else
                    {
                        if (state.EventSource != EventSource.Other)
                        {
                            if (userInput != null)
                            {
                                var nameList = userInput.Split(CreateEventWhiteList.GetContactNameSeparator(), StringSplitOptions.None)
                                    .Select(x => x.Trim())
                                    .Where(x => !string.IsNullOrWhiteSpace(x))
                                    .ToList();
                                state.AttendeesNameList = nameList;
                            }

                            return await sc.BeginDialogAsync(Actions.ConfirmAttendee, cancellationToken: cancellationToken);
                        }
                        else
                        {
                            return await sc.BeginDialogAsync(Actions.UpdateAddress, new UpdateAddressDialogOptions(UpdateAddressDialogOptions.UpdateReason.NotAnAddress), cancellationToken);
                        }
                    }
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

        // confirm attendee waterfall steps
        public async Task<DialogTurnResult> ConfirmAttendee(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var currentRecipientName = state.AttendeesNameList[state.ConfirmAttendeesNameIndex];
                if (IsEmail(currentRecipientName))
                {
                    var result =
                        new FoundChoice()
                        {
                            Value = $"{currentRecipientName}: {currentRecipientName}",
                        };

                    return await sc.NextAsync(result);
                }

                var originPersonList = await GetPeopleWorkWithAsync(sc, currentRecipientName);
                var originContactList = await GetContactsAsync(sc, currentRecipientName);
                originPersonList.AddRange(originContactList);

                var originUserList = new List<PersonModel>();
                try
                {
                    originUserList = await GetUserAsync(sc, currentRecipientName);
                }
                catch
                {
                    // do nothing when get user failed. because can not use token to ensure user use a work account.
                }

                (var personList, var userList) = FormatRecipientList(originPersonList, originUserList);

                // todo: should set updatename reason in sc.Result
                if (personList.Count > 10)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateName, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.TooMany), cancellationToken);
                }

                if (personList.Count < 1 && userList.Count < 1)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateName, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.NotFound), cancellationToken);
                }

                if (personList.Count == 1)
                {
                    var user = personList.FirstOrDefault();
                    if (user != null)
                    {
                        var result =
                            new FoundChoice()
                            {
                                Value = $"{user.DisplayName}: {user.Emails[0] ?? user.UserPrincipalName}",
                            };

                        return await sc.NextAsync(result, cancellationToken);
                    }
                }

                // TODO: should be simplify
                var selectOption = await GenerateOptions(personList, userList, sc);

                // If no more recipient to show, start update name flow and reset the recipient paging index.
                if (selectOption.Choices.Count == 0)
                {
                    state.ShowAttendeesIndex = 0;
                    return await sc.BeginDialogAsync(Actions.UpdateName, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.NotFound), cancellationToken);
                }

                // Update prompt string to include the choices because the list style is none;
                // TODO: should be removed if use adaptive card show choices.
                var choiceString = GetSelectPromptString(selectOption, true);
                selectOption.Prompt.Text = choiceString;
                return await sc.PromptAsync(Actions.Choice, selectOption, cancellationToken);
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

        public async Task<DialogTurnResult> AfterConfirmAttendee(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                // result is null when just update the recipient name. show recipients page should be reset.
                if (sc.Result == null)
                {
                    state.ShowAttendeesIndex = 0;
                    return await sc.BeginDialogAsync(Actions.ConfirmAttendee, cancellationToken: cancellationToken);
                }
                else if (sc.Result.ToString() == General.Intent.Next.ToString())
                {
                    state.ShowAttendeesIndex++;
                    return await sc.BeginDialogAsync(Actions.ConfirmAttendee, cancellationToken: cancellationToken);
                }
                else if (sc.Result.ToString() == General.Intent.Previous.ToString())
                {
                    if (state.ShowAttendeesIndex > 0)
                    {
                        state.ShowAttendeesIndex--;
                    }

                    return await sc.BeginDialogAsync(Actions.ConfirmAttendee, cancellationToken: cancellationToken);
                }
                else
                {
                    var user = (sc.Result as FoundChoice)?.Value.Trim('*');
                    if (user != null)
                    {
                        var attendee = new EventModel.Attendee
                        {
                            DisplayName = user.Split(": ")[0],
                            Address = user.Split(": ")[1],
                        };
                        if (state.Attendees.All(r => r.Address != attendee.Address))
                        {
                            state.Attendees.Add(attendee);
                        }
                    }

                    state.ConfirmAttendeesNameIndex++;
                    if (state.ConfirmAttendeesNameIndex < state.AttendeesNameList.Count)
                    {
                        return await sc.BeginDialogAsync(Actions.ConfirmAttendee, cancellationToken: cancellationToken);
                    }

                    return await sc.EndDialogAsync(true, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // update name waterfall steps
        public async Task<DialogTurnResult> UpdateUserName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var currentRecipientName = state.AttendeesNameList[state.ConfirmAttendeesNameIndex];

                if (((UpdateUserNameDialogOptions)sc.Options).Reason == UpdateUserNameDialogOptions.UpdateReason.TooMany)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.PromptTooManyPeople, ResponseBuilder, new StringDictionary() { { "UserName", currentRecipientName } }) }, cancellationToken);
                }
                else
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.PromptPersonNotFound, ResponseBuilder, new StringDictionary() { { "UserName", currentRecipientName } }) }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterUpdateUserName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                if (!string.IsNullOrEmpty(userInput))
                {
                    var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                    state.AttendeesNameList[state.ConfirmAttendeesNameIndex] = userInput;
                }

                return await sc.EndDialogAsync(null, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // update start date waterfall steps
        public async Task<DialogTurnResult> UpdateStartDateForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.CreateHasDetail && state.RecreateState != RecreateEventState.Time)
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                return await sc.PromptAsync(Actions.DatePromptForCreate, new PromptOptions
                {
                    Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoStartDate),
                    RetryPrompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoStartDate_Retry)
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterUpdateStartDateForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.CreateHasDetail && state.RecreateState != RecreateEventState.Time)
                {
                    DateTime datetime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, state.GetUserTimeZone());
                    state.StartDate.Add(datetime);
                }
                else
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
                                    var isRelativeTime = IsRelativeTime(sc.Context.Activity.Text, dateTimeValue, dateTimeConvertType);
                                    if (ContainsTime(dateTimeConvertType))
                                    {
                                        state.StartTime.Add(TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone()));
                                    }

                                    // Workaround as DateTimePrompt only return as local time
                                    if (isRelativeTime)
                                    {
                                        dateTime = new DateTime(
                                            dateTime.Year,
                                            dateTime.Month,
                                            dateTime.Day,
                                            DateTime.Now.Hour,
                                            DateTime.Now.Minute,
                                            DateTime.Now.Second);
                                    }

                                    state.StartDate.Add(isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : dateTime);
                                }
                            }
                            catch (FormatException ex)
                            {
                                await HandleExpectedDialogExceptions(sc, ex);
                            }
                        }
                    }
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
        public async Task<DialogTurnResult> UpdateStartTimeForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (!state.StartTime.Any())
                {
                    return await sc.PromptAsync(Actions.TimePromptForCreate, new NoSkipPromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoStartTime),
                        RetryPrompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoStartTime_Retry),
                        NoSkipPrompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoStartTime_NoSkip)
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

        public async Task<DialogTurnResult> AfterUpdateStartTimeForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (sc.Result != null && !state.StartTime.Any())
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
                                    var isRelativeTime = IsRelativeTime(sc.Context.Activity.Text, dateTimeValue, dateTimeConvertType);
                                    state.StartTime.Add(isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : dateTime);
                                }
                            }
                            catch (FormatException ex)
                            {
                                await HandleExpectedDialogExceptions(sc, ex);
                            }
                        }
                    }
                }

                var userNow = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone());
                var startDate = state.StartDate.Last();
                foreach (var startTime in state.StartTime)
                {
                    var startDateTime = new DateTime(
                        startDate.Year,
                        startDate.Month,
                        startDate.Day,
                        startTime.Hour,
                        startTime.Minute,
                        startTime.Second);
                    if (state.StartDateTime == null)
                    {
                        state.StartDateTime = startDateTime;
                    }

                    if (startDateTime >= userNow)
                    {
                        state.StartDateTime = startDateTime;
                        break;
                    }
                }

                state.StartDateTime = TimeZoneInfo.ConvertTimeToUtc(state.StartDateTime.Value, state.GetUserTimeZone());
                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // update duration waterfall steps
        public async Task<DialogTurnResult> UpdateDurationForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.Duration > 0 || state.EndTime.Any() || state.EndDate.Any() || (state.CreateHasDetail && state.RecreateState != RecreateEventState.Time && state.RecreateState != RecreateEventState.Duration))
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                return await sc.PromptAsync(Actions.DurationPromptForCreate, new PromptOptions
                {
                    Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoDuration),
                    RetryPrompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoDuration_Retry)
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterUpdateDurationForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.EndDate.Any() || state.EndTime.Any())
                {
                    var startDate = !state.StartDate.Any() ? TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone()) : state.StartDate.Last();
                    var endDate = startDate;
                    if (state.EndDate.Any())
                    {
                        endDate = state.EndDate.Last();
                    }

                    if (state.EndTime.Any())
                    {
                        foreach (var endtime in state.EndTime)
                        {
                            var endDateTime = new DateTime(
                                endDate.Year,
                                endDate.Month,
                                endDate.Day,
                                endtime.Hour,
                                endtime.Minute,
                                endtime.Second);
                            endDateTime = TimeZoneInfo.ConvertTimeToUtc(endDateTime, state.GetUserTimeZone());
                            if (state.EndDateTime == null || endDateTime >= state.StartDateTime)
                            {
                                state.EndDateTime = endDateTime;
                            }
                        }
                    }
                    else
                    {
                        state.EndDateTime = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59);
                        state.EndDateTime = TimeZoneInfo.ConvertTimeToUtc(state.EndDateTime.Value, state.GetUserTimeZone());
                    }

                    var ts = state.StartDateTime.Value.Subtract(state.EndDateTime.Value).Duration();
                    state.Duration = (int)ts.TotalSeconds;
                }

                if (state.Duration <= 0 && state.CreateHasDetail && state.RecreateState != RecreateEventState.Time && state.RecreateState != RecreateEventState.Duration)
                {
                    state.Duration = 1800;
                }

                if (state.Duration <= 0 && sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);

                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                    if (dateTimeResolutions.First().Value != null)
                    {
                        int.TryParse(dateTimeResolutions.First().Value, out var duration);
                        state.Duration = duration;
                    }
                }

                if (state.Duration > 0)
                {
                    state.EndDateTime = state.StartDateTime.Value.AddSeconds(state.Duration);
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

        public async Task<DialogTurnResult> GetRecreateInfo(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.GetRecreateInfoPrompt, new PromptOptions
                {
                    Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.GetRecreateInfo),
                    RetryPrompt = sc.Context.Activity.CreateReply(CreateEventResponses.GetRecreateInfo_Retry)
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterGetRecreateInfo(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (sc.Result != null)
                {
                    RecreateEventState? recreateState = sc.Result as RecreateEventState?;
                    switch (recreateState.Value)
                    {
                        case RecreateEventState.Cancel:
                            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarSharedResponses.ActionEnded), cancellationToken);
                            state.Clear();
                            return await sc.EndDialogAsync(true, cancellationToken);
                        case RecreateEventState.Time:
                            state.ClearTimes();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Duration:
                            state.ClearTimesExceptStartTime();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Location:
                            state.ClearLocation();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Participants:
                            state.ClearParticipants();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Subject:
                            state.ClearSubject();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Content:
                            state.ClearContent();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        default:
                            // should not go to this part. place an error handling for save.
                            await HandleDialogExceptions(sc, new Exception("Get unexpect state in recreate."));
                            return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                    }
                }
                else
                {
                    // should not go to this part. place an error handling for save.
                    await HandleDialogExceptions(sc, new Exception("Get unexpect result in recreate."));
                    return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected static (List<PersonModel> formattedPersonList, List<PersonModel> formattedUserList) FormatRecipientList(List<PersonModel> personList, List<PersonModel> userList)
        {
            // Remove dup items
            List<PersonModel> formattedPersonList = new List<PersonModel>();
            List<PersonModel> formattedUserList = new List<PersonModel>();

            foreach (var person in personList)
            {
                var mailAddress = person.Emails[0] ?? person.UserPrincipalName;

                bool isDup = false;
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

                bool isDup = false;
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

        protected async Task<PromptOptions> GenerateOptions(List<PersonModel> personList, List<PersonModel> userList, DialogContext dc)
        {
            var state = await Accessor.GetAsync(dc.Context);
            var pageIndex = state.ShowAttendeesIndex;
            var pageSize = 5;
            var skip = pageSize * pageIndex;
            var options = new PromptOptions
            {
                Choices = new List<Choice>(),
                Prompt = dc.Context.Activity.CreateReply(CreateEventResponses.ConfirmRecipient),
            };
            for (var i = 0; i < personList.Count; i++)
            {
                var user = personList[i];
                var mailAddress = user.Emails[0] ?? user.UserPrincipalName;

                var choice = new Choice()
                {
                    Value = $"**{user.DisplayName}: {mailAddress}**",
                    Synonyms = new List<string> { (options.Choices.Count + 1).ToString(), user.DisplayName, user.DisplayName.ToLower(), mailAddress },
                };

                var userName = user.UserPrincipalName?.Split("@").FirstOrDefault() ?? user.UserPrincipalName;
                if (!string.IsNullOrEmpty(userName))
                {
                    choice.Synonyms.Add(userName);
                    choice.Synonyms.Add(userName.ToLower());
                }

                if (skip <= 0)
                {
                    if (options.Choices.Count >= pageSize)
                    {
                        return options;
                    }

                    options.Choices.Add(choice);
                }
                else
                {
                    skip--;
                }
            }

            if (options.Choices.Count == 0)
            {
                pageSize = 10;
            }

            for (var i = 0; i < userList.Count; i++)
            {
                var user = userList[i];
                var mailAddress = user.Emails[0] ?? user.UserPrincipalName;
                var choice = new Choice()
                {
                    Value = $"{user.DisplayName}: {mailAddress}",
                    Synonyms = new List<string> { (options.Choices.Count + 1).ToString(), user.DisplayName, user.DisplayName.ToLower(), mailAddress },
                };

                var userName = user.UserPrincipalName?.Split("@").FirstOrDefault() ?? user.UserPrincipalName;
                if (!string.IsNullOrEmpty(userName))
                {
                    choice.Synonyms.Add(userName);
                    choice.Synonyms.Add(userName.ToLower());
                }

                if (skip <= 0)
                {
                    if (options.Choices.Count >= pageSize)
                    {
                        return options;
                    }

                    options.Choices.Add(choice);
                }
                else if (skip >= 10)
                {
                    return options;
                }
                else
                {
                    skip--;
                }
            }

            return options;
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

        private string GetSelectPromptString(PromptOptions selectOption, bool containNumbers)
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

        private bool IsEmail(string emailString)
        {
            return Regex.IsMatch(emailString, @"\w[-\w.+]*@([A-Za-z0-9][-A-Za-z0-9]+\.)+[A-Za-z]{2,14}");
        }
    }
}