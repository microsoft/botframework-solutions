using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.Shared.Resources;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Graph;
using Microsoft.Recognizers.Text;
using Newtonsoft.Json.Linq;

namespace CalendarSkill
{
    public class CalendarSkillDialog : ComponentDialog
    {
        // Constants
        public const string AuthSkillMode = "SkillAuth";
        public const string AuthLocalMode = "LocalAuth";

        // Fields
        private CalendarBotResponseBuilder _responseBuilder = new CalendarBotResponseBuilder();
        private CalendarSkillServices _services;
        private CalendarSkillAccessors _accessors;
        private IServiceManager _serviceManager;

        public CalendarSkillDialog(string dialogId, CalendarSkillServices services, CalendarSkillAccessors accessors, IServiceManager serviceManager)
            : base(dialogId)
        {
            _services = services;
            _accessors = accessors;
            _serviceManager = serviceManager;

            var oauthSettings = new OAuthPromptSettings()
            {
                ConnectionName = this._services.AuthConnectionName,
                Text = $"Authentication",
                Title = "Signin",
                Timeout = 300000, // User has 5 minutes to login
            };

            AddDialog(new EventPrompt(AuthSkillMode, "tokens/response", TokenResponseValidator));
            AddDialog(new OAuthPrompt(AuthLocalMode, oauthSettings, AuthPromptValidator));
            AddDialog(new TextPrompt(Action.Prompt));
            AddDialog(new ConfirmPrompt(Action.TakeFurtherAction, null, Culture.English) { Style = ListStyle.SuggestedAction });
            AddDialog(new DateTimePrompt(Action.DateTimePrompt, null, Culture.English));
            AddDialog(new DateTimePrompt(Action.DateTimePromptForUpdateDelete, DateTimePromptValidator, Culture.English));
            AddDialog(new ChoicePrompt(Action.Choice, ChoiceValidator, Culture.English) { Style = ListStyle.None, });
            AddDialog(new ChoicePrompt(Action.EventChoice, null, Culture.English) { Style = ListStyle.Inline, ChoiceOptions = new ChoiceFactoryOptions { InlineSeparator = string.Empty, InlineOr = string.Empty, InlineOrMore = string.Empty, IncludeNumbers = false } });
        }

        // Shared Steps
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
                    return await sc.PromptAsync(AuthSkillMode, new PromptOptions());
                }
                else
                {
                    return await sc.PromptAsync(AuthLocalMode, new PromptOptions());
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.AuthFailed));
                await _accessors.CalendarSkillState.SetAsync(sc.Context, new CalendarSkillState());
                return await sc.CancelAllDialogsAsync();
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
                    var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                    state.APIToken = tokenResponse.Token;
                }

                return await sc.NextAsync();
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.AuthFailed, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> Greeting(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.GreetingMessage, _responseBuilder));
                return await sc.EndDialogAsync(true);
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> CollectTitle(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);

                if (string.IsNullOrEmpty(state.Title))
                {
                    string userNameString = string.Empty;
                    foreach (var attendee in state.Attendees)
                    {
                        if (userNameString != string.Empty)
                        {
                            userNameString += ", ";
                        }

                        userNameString += attendee.DisplayName ?? attendee.Address;
                    }

                    return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoTitle, _responseBuilder, new StringDictionary() { { "UserName", userNameString } }) });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> CollectContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    if (string.IsNullOrEmpty(state.Title))
                    {
                        sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                        state.Title = content != null ? content.ToString() : sc.Context.Activity.Text;
                    }
                }

                if (string.IsNullOrEmpty(state.Content))
                {
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoContent),
                    });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> CollectAttendees(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.APIToken))
                {
                    return await sc.EndDialogAsync(true);
                }

                var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource, state.GetUserTimeZone());
                if (state.Attendees.Count == 0)
                {
                    return await sc.BeginDialogAsync(Action.UpdateAddress);
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> CollectStartDate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    if (string.IsNullOrEmpty(state.Content))
                    {
                        sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                        state.Content = content != null ? content.ToString() : sc.Context.Activity.Text;
                    }
                }

                if (state.StartDate == null)
                {
                    return await sc.BeginDialogAsync(Action.UpdateStartDateForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> CollectStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                if (state.StartTime == null)
                {
                    return await sc.BeginDialogAsync(Action.UpdateStartTimeForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> CollectDuration(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);

                if (state.EndDateTime == null)
                {
                    return await sc.BeginDialogAsync(Action.UpdateDurationForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> CollectLocation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);

                if (state.Location == null)
                {
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoLocation) });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> ConfirmBeforeCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var luisResult = CalendarSkillHelper.GetLuisResult(sc.Context, this._accessors, this._services, cancellationToken).Result;
                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                    var topIntent = luisResult?.TopIntent().intent;
                    if (topIntent == Luis.Calendar.Intent.Reject || topIntent == Luis.Calendar.Intent.ConfirmNo || topIntent == Luis.Calendar.Intent.NoLocation)
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
                    StartTime = (DateTime)state.StartDateTime,
                    EndTime = (DateTime)state.EndDateTime,
                    TimeZone = state.GetUserTimeZone(),
                    Location = state.Location,
                };

                var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(CalendarBotResponses.ConfirmCreate, newEvent.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", newEvent.ToAdaptiveCardData());

                return await sc.PromptAsync(Action.TakeFurtherAction, new PromptOptions
                {
                    Prompt = replyMessage,
                    RetryPrompt = sc.Context.Activity.CreateReply(CalendarBotResponses.ConfirmCreateFailed, _responseBuilder),
                });
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> CreateEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
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
                        TimeZone = state.GetUserTimeZone(),
                        Location = state.Location,
                    };
                    var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource, state.GetUserTimeZone());
                    if (await calendarService.CreateEvent(newEvent) != null)
                    {
                        var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(CalendarBotResponses.EventCreated, newEvent.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", newEvent.ToAdaptiveCardData());
                        await sc.Context.SendActivityAsync(replyMessage);
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.EventCreationFailed));
                    }

                    state.Clear();
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.ActionEnded));
                    state.Clear();
                }

                return await sc.EndDialogAsync(true);
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> UpdateAddress(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                if (state.EventSource == EventSource.Microsoft)
                {
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoAttendeesMS),
                    });
                }
                else
                {
                    if (sc.Result != null)
                    {
                        return await sc.PromptAsync(Action.Prompt, new PromptOptions
                        {
                            Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.WrongAddress),
                        });
                    }
                    else
                    {
                        return await sc.PromptAsync(Action.Prompt, new PromptOptions
                        {
                            Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoAttendees),
                        });
                    }
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> AfterUpdateAddress(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                    // TODO: can we do this somewhere else
                    if (IsEmail(userInput))
                    {
                        state.Attendees.Add(new EventModel.Attendee { Address = userInput });
                        return await sc.EndDialogAsync(true);
                    }
                    else
                    {
                        if (state.EventSource == EventSource.Microsoft)
                        {
                            if (userInput != null)
                            {
                                var nameList = userInput.Split(new string[] { ",", "and", ";" }, StringSplitOptions.None)
                                    .Select(x => x.Trim())
                                    .Where(x => !string.IsNullOrWhiteSpace(x))
                                    .ToList();
                                state.AttendeesNameList = nameList;
                            }

                            return await sc.BeginDialogAsync(Action.ConfirmAttendee);
                        }
                        else
                        {
                            return await sc.BeginDialogAsync(Action.UpdateAddress, new UpdateAddressDialogOptions(UpdateAddressDialogOptions.UpdateReason.NotAnAddress));
                        }
                    }
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> ConfirmAttendee(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                var currentRecipientName = state.AttendeesNameList[state.ConfirmAttendeesNameIndex];
                var personList = await GetPeopleWorkWithAsync(sc, currentRecipientName);
                var userList = await GetUserAsync(sc, currentRecipientName);

                // todo: should set updatename reason in sc.Result
                if (personList.Count > 10)
                {
                    return await sc.BeginDialogAsync(Action.UpdateName, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.TooMany));
                }

                if (personList.Count < 1 && userList.Count < 1)
                {
                    return await sc.BeginDialogAsync(Action.UpdateName, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.NotFound));
                }

                if (personList.Count == 1)
                {
                    var user = personList.FirstOrDefault();
                    if (user != null)
                    {
                        var result =
                            new FoundChoice()
                            {
                                Value = $"{user.DisplayName}: {user.ScoredEmailAddresses.FirstOrDefault()?.Address ?? user.UserPrincipalName}",
                            };

                        return await sc.NextAsync(result);
                    }
                }

                // TODO: should be simplify
                var selectOption = await GenerateOptions(personList, userList, sc);

                // If no more recipient to show, start update name flow and reset the recipient paging index.
                if (selectOption.Choices.Count == 0)
                {
                    state.ShowAttendeesIndex = 0;
                    return await sc.BeginDialogAsync(Action.UpdateName, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.TooMany));
                }

                // Update prompt string to include the choices because the list style is none;
                // TODO: should be removed if use adaptive card show choices.
                var choiceString = GetSelectPromptString(selectOption, true);
                selectOption.Prompt.Text = choiceString;
                return await sc.PromptAsync(Action.Choice, selectOption);
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> AfterConfirmAttendee(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);

                // result is null when just update the recipient name. show recipients page should be reset.
                if (sc.Result == null)
                {
                    state.ShowAttendeesIndex = 0;
                    return await sc.BeginDialogAsync(Action.ConfirmAttendee);
                }
                else if (sc.Result.ToString() == Luis.Calendar.Intent.ShowNext.ToString())
                {
                    state.ShowAttendeesIndex++;
                    return await sc.BeginDialogAsync(Action.ConfirmAttendee);
                }
                else if (sc.Result.ToString() == Luis.Calendar.Intent.ShowPrevious.ToString())
                {
                    if (state.ShowAttendeesIndex > 0)
                    {
                        state.ShowAttendeesIndex--;
                    }

                    return await sc.BeginDialogAsync(Action.ConfirmAttendee);
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
                        return await sc.BeginDialogAsync(Action.ConfirmAttendee);
                    }
                    else
                    {
                        return await sc.EndDialogAsync(true);
                    }
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> UpdateUserName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                var currentRecipientName = state.AttendeesNameList[state.ConfirmAttendeesNameIndex];

                if (((UpdateUserNameDialogOptions)sc.Options).Reason == UpdateUserNameDialogOptions.UpdateReason.TooMany)
                {
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.PromptTooManyPeople, _responseBuilder, new StringDictionary() { { "UserName", currentRecipientName } }),
                    });
                }
                else
                {
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.PromptPersonNotFound, _responseBuilder),
                    });
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
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
                    var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                    state.AttendeesNameList[state.ConfirmAttendeesNameIndex] = userInput;
                }

                return await sc.EndDialogAsync(true);
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> UpdateStartDateForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (((UpdateDateTimeDialogOptions)sc.Options).Reason == UpdateDateTimeDialogOptions.UpdateReason.NotFound)
                {
                    return await sc.PromptAsync(Action.DateTimePrompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoStartDate),
                    });
                }
                else
                {
                    return await sc.PromptAsync(Action.DateTimePrompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.DidntUnderstandMessage),
                    });
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> AfterUpdateStartDateForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                    var dateTime = DateTime.Parse(dateTimeResolutions.First().Value);
                    var dateTimeConvertType = dateTimeResolutions.First().Timex;

                    if (dateTime != null)
                    {
                        bool isRelativeTime = CalendarSkillHelper.IsRelativeTime(sc.Context.Activity.Text, dateTimeResolutions.First().Value, dateTimeResolutions.First().Timex);

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

                        state.StartDate = isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : dateTime;
                        return await sc.EndDialogAsync();
                    }
                }

                return await sc.BeginDialogAsync(Action.UpdateStartDateForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime));
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> UpdateStartTimeForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (((UpdateDateTimeDialogOptions)sc.Options).Reason == UpdateDateTimeDialogOptions.UpdateReason.NotFound)
                {
                    return await sc.PromptAsync(Action.DateTimePrompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoStartTime),
                    });
                }
                else
                {
                    return await sc.PromptAsync(Action.DateTimePrompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.DidntUnderstandMessage),
                    });
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> AfterUpdateStartTimeForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                    var dateTime = DateTime.Parse(dateTimeResolutions.First().Value);
                    var dateTimeConvertType = dateTimeResolutions.First().Timex;

                    if (dateTime != null)
                    {
                        bool isRelativeTime = CalendarSkillHelper.IsRelativeTime(sc.Context.Activity.Text, dateTimeResolutions.First().Value, dateTimeResolutions.First().Timex);
                        state.StartTime = isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : dateTime;
                        state.StartDateTime = new DateTime(
                            state.StartDate.Value.Year,
                            state.StartDate.Value.Month,
                            state.StartDate.Value.Day,
                            state.StartTime.Value.Hour,
                            state.StartTime.Value.Minute,
                            state.StartTime.Value.Second);
                        return await sc.EndDialogAsync();
                    }
                }

                return await sc.BeginDialogAsync(Action.UpdateStartTimeForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime));
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> UpdateDurationForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (((UpdateDateTimeDialogOptions)sc.Options).Reason == UpdateDateTimeDialogOptions.UpdateReason.NotFound)
                {
                    return await sc.PromptAsync(Action.DateTimePrompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoDuration),
                    });
                }
                else
                {
                    return await sc.PromptAsync(Action.DateTimePrompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.DidntUnderstandMessage),
                    });
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> AfterUpdateDurationForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);

                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                    int.TryParse(dateTimeResolutions.First().Value, out int duration);

                    if (duration > 0)
                    {
                        state.EndDateTime = state.StartDateTime.Value.AddSeconds(duration);
                        return await sc.EndDialogAsync();
                    }
                    else
                    {
                        // TODO: Handle improper duration
                    }
                }

                return await sc.BeginDialogAsync(Action.UpdateDurationForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime));
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> ConfirmBeforeDelete(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                if (sc.Result != null && state.Events.Count > 1)
                {
                    var events = state.Events;
                    state.Events = new List<EventModel>
                {
                    events[(sc.Result as FoundChoice).Index],
                };
                }

                var deleteEvent = state.Events[0];
                var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(CalendarBotResponses.ConfirmDelete, deleteEvent.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", deleteEvent.ToAdaptiveCardData());

                return await sc.PromptAsync(Action.TakeFurtherAction, new PromptOptions
                {
                    Prompt = replyMessage,
                    RetryPrompt = sc.Context.Activity.CreateReply(CalendarBotResponses.ConfirmDeleteFailed, _responseBuilder),
                });
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> DeleteEventByStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource, state.GetUserTimeZone());
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var deleteEvent = state.Events[0];
                    await calendarService.DeleteEventById(deleteEvent.Id);

                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.EventDeleted));
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.ActionEnded));
                }

                state.Clear();
                return await sc.EndDialogAsync(true);
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> UpdateStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);

                if (((UpdateDateTimeDialogOptions)sc.Options).Reason == UpdateDateTimeDialogOptions.UpdateReason.NoEvent)
                {
                    return await sc.PromptAsync(Action.DateTimePromptForUpdateDelete, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.EventWithStartTimeNotFound),
                    });
                }
                else
                {
                    if (state.DialogName == "DeleteEvent")
                    {
                        return await sc.PromptAsync(Action.DateTimePromptForUpdateDelete, new PromptOptions
                        {
                            Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoDeleteStartTime),
                        });
                    }
                    else
                    {
                        return await sc.PromptAsync(Action.DateTimePromptForUpdateDelete, new PromptOptions
                        {
                            Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoUpdateStartTime),
                        });
                    }
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> AfterUpdateStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                DateTime? startTime = null;
                try
                {
                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                    if (dateTimeResolutions.Count > 0)
                    {
                        startTime = DateTime.Parse(dateTimeResolutions.First().Value);
                        var dateTimeConvertType = dateTimeResolutions.First().Timex;
                        bool isRelativeTime = CalendarSkillHelper.IsRelativeTime(sc.Context.Activity.Text, dateTimeResolutions.First().Value, dateTimeResolutions.First().Timex);
                        startTime = isRelativeTime ? TimeZoneInfo.ConvertTime(startTime.Value, TimeZoneInfo.Local, state.GetUserTimeZone()) : startTime;
                    }
                }
                catch
                {
                }

                var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource, state.GetUserTimeZone());

                var events = new List<EventModel>();
                if (startTime != null)
                {
                    state.StartDateTime = startTime;
                    startTime = DateTime.SpecifyKind(startTime.Value, DateTimeKind.Local);
                    events = await calendarService.GetEventsByStartTime(startTime.Value);
                }
                else
                {
                    state.Title = userInput;
                    events = await calendarService.GetEventsByTitle(userInput);
                }

                state.Events = events;

                if (events.Count <= 0)
                {
                    return await sc.BeginDialogAsync(Action.UpdateStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NoEvent));
                }
                else if (events.Count > 1)
                {
                    var options = new PromptOptions()
                    {
                        Choices = new List<Choice>(),
                    };

                    for (var i = 0; i < events.Count; i++)
                    {
                        var item = events[i];
                        var choice = new Choice()
                        {
                            Value = string.Empty,
                            Synonyms = new List<string> { (i + 1).ToString(), item.Title },
                        };
                        options.Choices.Add(choice);
                    }

                    var replyToConversation = sc.Context.Activity.CreateReply(CalendarBotResponses.MultipleEventsStartAtSameTime);
                    replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyToConversation.Attachments = new List<Microsoft.Bot.Schema.Attachment>();

                    var cardsData = new List<CalendarCardData>();
                    foreach (var item in events)
                    {
                        var meetingCard = item.ToAdaptiveCardData();
                        var replyTemp = sc.Context.Activity.CreateAdaptiveCardReply(CalendarBotResponses.GreetingMessage, item.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", meetingCard);
                        replyToConversation.Attachments.Add(replyTemp.Attachments[0]);
                    }

                    options.Prompt = replyToConversation;

                    return await sc.PromptAsync(Action.EventChoice, options);
                }
                else
                {
                    return await sc.EndDialogAsync(true);
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> ShowNextEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.APIToken))
                {
                    return await sc.EndDialogAsync(true);
                }

                var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource, state.GetUserTimeZone());

                var eventList = await calendarService.GetUpcomingEvents();
                var nextEventList = new List<EventModel>();
                foreach (var item in eventList)
                {
                    if (item.IsCancelled != true && (nextEventList.Count == 0 || nextEventList[0].StartTime == item.StartTime))
                    {
                        nextEventList.Add(item);
                    }
                }

                if (nextEventList.Count == 0)
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.ShowNoMeetingMessage));
                }
                else
                {
                    if (nextEventList.Count == 1)
                    {
                        var speakParams = new StringDictionary()
                        {
                            { "EventName", nextEventList[0].Title },
                            { "EventTime", nextEventList[0].StartTime.ToString("h:mm tt") },
                            { "PeopleCount", nextEventList[0].Attendees.Count.ToString() },
                        };
                        if (string.IsNullOrEmpty(nextEventList[0].Location))
                        {
                            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.ShowNextMeetingNoLocationMessage, _responseBuilder, speakParams));
                        }
                        else
                        {
                            speakParams.Add("Location", nextEventList[0].Location);
                            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.ShowNextMeetingMessage, _responseBuilder, speakParams));
                        }
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.ShowMultipleNextMeetingMessage));
                    }

                    await ShowMeetingList(sc, nextEventList, true);
                }

                state.Clear();
                return await sc.EndDialogAsync(true);
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage, _responseBuilder));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> IfClearContextStep(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // clear context before show emails, and extract it from luis result again.
                var state = await this._accessors.CalendarSkillState.GetAsync(sc.Context);

                var luisResult = CalendarSkillHelper.GetLuisResult(sc.Context, this._accessors, this._services, cancellationToken).Result;

                var topIntent = luisResult?.TopIntent().intent;

                if (topIntent == Luis.Calendar.Intent.Summary)
                {
                    state.Clear();
                }

                if (topIntent == Luis.Calendar.Intent.ShowNext)
                {
                    if ((state.ShowEventIndex + 1) * CalendarSkillState.PageSize < state.SummaryEvents.Count)
                    {
                        state.ShowEventIndex++;
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarNoMoreEvent));
                        return await sc.CancelAllDialogsAsync();
                    }
                }

                if (topIntent == Luis.Calendar.Intent.ShowPrevious)
                {
                    if (state.ShowEventIndex > 0)
                    {
                        state.ShowEventIndex--;
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarNoPreviousEvent));
                        return await sc.CancelAllDialogsAsync();
                    }
                }

                return await sc.NextAsync();
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> ShowEventsSummary(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var tokenResponse = sc.Result as TokenResponse;

                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                if (state.SummaryEvents == null)
                {
                    if (string.IsNullOrEmpty(state.APIToken))
                    {
                        return await sc.EndDialogAsync(true);
                    }

                    var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource, state.GetUserTimeZone());

                    var searchDate = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, state.GetUserTimeZone());

                    var startTime = new DateTime(searchDate.Year, searchDate.Month, searchDate.Day);
                    var endTime = new DateTime(searchDate.Year, searchDate.Month, searchDate.Day, 23, 59, 59);
                    var startTimeUtc = TimeZoneInfo.ConvertTimeToUtc(startTime, state.GetUserTimeZone());
                    var endTimeUtc = TimeZoneInfo.ConvertTimeToUtc(endTime, state.GetUserTimeZone());
                    var rawEvents = await calendarService.GetEventsByTime(startTimeUtc, endTimeUtc);
                    var todayEvents = new List<EventModel>();
                    foreach (var item in rawEvents)
                    {
                        if (item.StartTime > searchDate && item.StartTime >= startTime && item.IsCancelled != true)
                        {
                            todayEvents.Add(item);
                        }
                    }

                    if (todayEvents.Count == 0)
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.ShowNoMeetingMessage));
                        return await sc.EndDialogAsync(true);
                    }
                    else
                    {
                        var speakParams = new StringDictionary()
                        {
                            { "Count", todayEvents.Count.ToString() },
                            { "EventName1", todayEvents[0].Title },
                            { "EventDuration", todayEvents[0].ToDurationString() },
                        };

                        if (todayEvents.Count == 1)
                        {
                            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.ShowOneMeetingSummaryMessage, _responseBuilder, speakParams));
                        }
                        else
                        {
                            speakParams.Add("EventName2", todayEvents[todayEvents.Count - 1].Title);
                            speakParams.Add("EventTime", todayEvents[todayEvents.Count - 1].StartTime.ToString("h:mm tt"));
                            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.ShowOneMeetingSummaryMessage, _responseBuilder, speakParams));
                        }
                    }

                    await this.ShowMeetingList(sc, todayEvents.GetRange(0, Math.Min(CalendarSkillState.PageSize, todayEvents.Count)), false);
                    state.SummaryEvents = todayEvents;
                }
                else
                {
                    await this.ShowMeetingList(sc, state.SummaryEvents.GetRange(state.ShowEventIndex * CalendarSkillState.PageSize, Math.Min(CalendarSkillState.PageSize, state.SummaryEvents.Count - (state.ShowEventIndex * CalendarSkillState.PageSize))), false);
                }

                return await sc.NextAsync();
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> FromTokenToStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.APIToken))
                {
                    return await sc.EndDialogAsync(true);
                }

                var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource, state.GetUserTimeZone());

                if (state.StartDateTime == null)
                {
                    return await sc.BeginDialogAsync(Action.UpdateStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> FromEventsToNewDate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                if (sc.Result != null && state.Events.Count > 1)
                {
                    var events = state.Events;
                    state.Events = new List<EventModel>
                {
                    events[(sc.Result as FoundChoice).Index],
                };
                }

                var origin = state.Events[0];
                if (!origin.IsOrganizer)
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.NotEventOrganizer));
                    state.Clear();
                    return await sc.EndDialogAsync(true);
                }
                else if (state.NewStartDateTime == null)
                {
                    return await sc.BeginDialogAsync(Action.UpdateNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> ConfirmBeforeUpdate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);

                var newStartTime = (DateTime)state.NewStartDateTime;
                newStartTime = DateTime.SpecifyKind(newStartTime, DateTimeKind.Local);

                // DateTime newStartTime = DateTime.Parse((string)state.NewStartDateTime);
                var origin = state.Events[0];
                var last = origin.EndTime - origin.StartTime;
                origin.StartTime = newStartTime;
                origin.EndTime = (newStartTime + last).AddSeconds(1);
                var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(CalendarBotResponses.ConfirmUpdate, origin.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", origin.ToAdaptiveCardData());

                return await sc.PromptAsync(Action.TakeFurtherAction, new PromptOptions
                {
                    Prompt = replyMessage,
                    RetryPrompt = sc.Context.Activity.CreateReply(CalendarBotResponses.ConfirmUpdateFailed, _responseBuilder),
                });
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> UpdateEventTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);

                    var newStartTime = (DateTime)state.NewStartDateTime;

                    var origin = state.Events[0];
                    var updateEvent = new EventModel(origin.Source);
                    var last = origin.EndTime - origin.StartTime;
                    updateEvent.StartTime = TimeZoneInfo.ConvertTimeToUtc(newStartTime, state.GetUserTimeZone());
                    updateEvent.EndTime = TimeZoneInfo.ConvertTimeToUtc((newStartTime + last).AddSeconds(1), state.GetUserTimeZone());
                    updateEvent.TimeZone = TimeZoneInfo.Utc;
                    updateEvent.Id = origin.Id;
                    var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource, state.GetUserTimeZone());
                    var newEvent = await calendarService.UpdateEventById(updateEvent);

                    newEvent.StartTime = TimeZoneInfo.ConvertTimeFromUtc(newEvent.StartTime, state.GetUserTimeZone());
                    newEvent.EndTime = TimeZoneInfo.ConvertTimeFromUtc(newEvent.EndTime, state.GetUserTimeZone());
                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(CalendarBotResponses.EventUpdated, newEvent.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", newEvent.ToAdaptiveCardData());
                    await sc.Context.SendActivityAsync(replyMessage);
                    state.Clear();
                }
            else
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.ActionEnded));
            }

                return await sc.EndDialogAsync(true);
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> GetNewEventTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (((UpdateDateTimeDialogOptions)sc.Options).Reason == UpdateDateTimeDialogOptions.UpdateReason.NotFound)
                {
                    return await sc.PromptAsync(Action.DateTimePrompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoNewTime) });
                }
                else
                {
                    return await sc.PromptAsync(Action.DateTimePrompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.DidntUnderstandMessage) });
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> AfterGetNewEventTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                    var newStartTime = DateTime.Parse(dateTimeResolutions.First().Value);
                    var dateTimeConvertType = dateTimeResolutions.First().Timex;

                    if (newStartTime != null)
                    {
                        bool isRelativeTime = CalendarSkillHelper.IsRelativeTime(sc.Context.Activity.Text, dateTimeResolutions.First().Value, dateTimeResolutions.First().Timex);
                        state.NewStartDateTime = isRelativeTime ? TimeZoneInfo.ConvertTime(newStartTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : newStartTime;
                        return await sc.ContinueDialogAsync();
                    }
                    else
                    {
                        return await sc.BeginDialogAsync(Action.UpdateNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime));
                    }
                }
                else
                {
                    return await sc.BeginDialogAsync(Action.UpdateNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime));
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
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

        public Task<bool> ChoiceValidator(PromptValidatorContext<FoundChoice> pc, CancellationToken cancellationToken)
        {
            var luisResult = CalendarSkillHelper.GetLuisResult(pc.Context, this._accessors, this._services, cancellationToken).Result;

            var topIntent = luisResult?.TopIntent().intent;

            // TODO: The signature for validators has changed to return bool -- Need new way to handle this logic
            // If user want to show more recipient end current choice dialog and return the intent to next step.
            if (topIntent == Luis.Calendar.Intent.ShowNext || topIntent == Luis.Calendar.Intent.ShowPrevious)
            {
                // pc.End(topIntent);
                return Task.FromResult(true);
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
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        public Task<bool> DateTimePromptValidator(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        // Helpers
        public async Task<List<Person>> GetUserAsync(DialogContext dc, string name)
        {
            var result = new List<Person>();
            var state = await _accessors.CalendarSkillState.GetAsync(dc.Context);
            try
            {
                var token = state.APIToken;
                var service = _serviceManager.InitUserService(token, state.GetUserTimeZone());

                // Get users.
                result = await service.GetPeopleAsync(name);
            }
            catch (ServiceException)
            {
                await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(CalendarBotResponses.FindUserErrorMessage, _responseBuilder));
                state.Clear();
                await dc.EndDialogAsync(true);
            }

            return result;
        }

        public async Task<List<Person>> GetPeopleWorkWithAsync(DialogContext dc, string name)
        {
            var result = new List<Person>();
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(dc.Context);
                var token = state.APIToken;
                var service = _serviceManager.InitUserService(token, state.GetUserTimeZone());

                // Get users.
                result = await service.GetPeopleAsync(name);
            }
            catch (ServiceException)
            {
                await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(CalendarBotResponses.FindUserErrorMessage, _responseBuilder));

                var state = await _accessors.CalendarSkillState.GetAsync(dc.Context);
                state.Clear();
                await _accessors.CalendarSkillState.SetAsync(dc.Context, state);
                await dc.EndDialogAsync(true); // todo: should be sc.EndAll();
            }

            return result;
        }

        public async Task<PromptOptions> GenerateOptions(List<Person> personList, List<Person> userList, DialogContext dc)
        {
            var state = await _accessors.CalendarSkillState.GetAsync(dc.Context);
            var pageIndex = state.ShowAttendeesIndex;
            var pageSize = 5;
            var skip = pageSize * pageIndex;
            var options = new PromptOptions
            {
                Choices = new List<Choice>(),
                Prompt = dc.Context.Activity.CreateReply(CalendarBotResponses.ConfirmRecipient),
            };
            for (var i = 0; i < personList.Count; i++)
            {
                var user = personList[i];
                var mailAddress = user.ScoredEmailAddresses.FirstOrDefault()?.Address ?? user.UserPrincipalName;

                var choice = new Choice()
                {
                    Value = $"**{user.DisplayName}: {mailAddress}**",
                    Synonyms = new List<string> { (i + 1).ToString(), user.DisplayName, user.DisplayName.ToLower(), mailAddress },
                };
                var userName = user.UserPrincipalName.Split("@").FirstOrDefault() ?? user.UserPrincipalName;
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
                var mailAddress = user.ScoredEmailAddresses.FirstOrDefault()?.Address ?? user.UserPrincipalName;
                var choice = new Choice()
                {
                    Value = $"{user.DisplayName}: {mailAddress}",
                    Synonyms = new List<string> { (i + 1).ToString(), user.DisplayName, user.DisplayName.ToLower(), mailAddress },
                };
                var userName = user.UserPrincipalName.Split("@").FirstOrDefault() ?? user.UserPrincipalName;
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

        public string GetSelectPromptString(PromptOptions selectOption, bool containNumbers)
        {
            var result = string.Empty;
            result += selectOption.Prompt.Text + "\r\n";
            for (var i = 0; i < selectOption.Choices.Count; i++)
            {
                var choice = selectOption.Choices[i];
                result += "  ";
                if (containNumbers)
                {
                    result += i + 1 + "-";
                }

                result += choice.Value + "\r\n";
            }

            return result;
        }

        public bool IsEmail(string emailString)
        {
            return Regex.IsMatch(emailString, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
        }

        public async Task ShowMeetingList(DialogContext dc, List<EventModel> events, bool showDate = true)
        {
            var replyToConversation = dc.Context.Activity.CreateReply();
            replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            replyToConversation.Attachments = new List<Microsoft.Bot.Schema.Attachment>();

            var cardsData = new List<CalendarCardData>();
            foreach (var item in events)
            {
                var meetingCard = item.ToAdaptiveCardData(showDate);
                var replyTemp = dc.Context.Activity.CreateAdaptiveCardReply(CalendarBotResponses.GreetingMessage, item.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", meetingCard);
                replyToConversation.Attachments.Add(replyTemp.Attachments[0]);
            }

            await dc.Context.SendActivityAsync(replyToConversation);
        }

        public async Task<DialogTurnResult> PromptToRead(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.ReadOutPrompt) });
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> CallReadEventDialog(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Action.Read);
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> ReadEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                var luisResult = CalendarSkillHelper.GetLuisResult(sc.Context, this._accessors, this._services, cancellationToken).Result;

                var topIntent = luisResult?.TopIntent().intent;
                if (topIntent == null)
                {
                    return await sc.EndDialogAsync(true);
                }

                var eventItem = state.ReadOutEvents.FirstOrDefault();
                if (topIntent == Luis.Calendar.Intent.ConfirmNo || topIntent == Luis.Calendar.Intent.Reject)
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CancellingMessage));
                    return await sc.EndDialogAsync(true);
                }
                else if (topIntent == Luis.Calendar.Intent.ReadAloud && eventItem == null)
                {
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.ReadOutPrompt), });
                }
                else if (eventItem != null)
                {
                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(CalendarBotResponses.ReadOutMessage, eventItem.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", eventItem.ToAdaptiveCardData());
                    await sc.Context.SendActivityAsync(replyMessage);

                    return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.ReadOutMorePrompt) });

                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch (Exception)
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> AfterReadOutEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var luisResult = CalendarSkillHelper.GetLuisResult(sc.Context, this._accessors, this._services, cancellationToken).Result;

                var topIntent = luisResult?.TopIntent().intent;
                if (topIntent == null)
                {
                    return await sc.EndDialogAsync(true);
                }

                if (topIntent == Luis.Calendar.Intent.ReadAloud)
                {
                    return await sc.BeginDialogAsync(Action.Read);
                }
                else
                {
                    return await sc.EndDialogAsync("true");
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.CalendarErrorMessage));
                var state = await _accessors.CalendarSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }
    }
}