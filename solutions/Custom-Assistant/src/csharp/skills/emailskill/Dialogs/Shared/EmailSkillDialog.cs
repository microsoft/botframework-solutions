// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.Shared;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkill.Extensions;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Graph;
using Microsoft.Recognizers.Text;
using Newtonsoft.Json.Linq;

namespace EmailSkill
{
    /// <summary>
    /// Email skill base dialog.
    /// </summary>
    public class EmailSkillDialog : ComponentDialog
    {
        /// <summary>
        /// Auth skill mode key.
        /// </summary>
        public const string AuthSkillmode = "SkillAuth";

        /// <summary>
        /// Local auth skill mode key.
        /// </summary>
        public const string AuthLocalmode = "LocalAuth";

        private EmailSkillServices _services;
        private IMailSkillServiceManager _serviceManager;
        private EmailSkillAccessors _accessors;
        private EmailBotResponseBuilder emailBotResponseBuilder = new EmailBotResponseBuilder();

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailSkillDialog"/> class.
        /// </summary>
        /// <param name="dialogId">Dialog Id.</param>
        /// <param name="services">EmailSkillServices.</param>
        /// <param name="serviceManager">Service manager for steps.</param>
        /// <param name="accessors">State accessor.</param>
        public EmailSkillDialog(string dialogId, EmailSkillServices services, EmailSkillAccessors accessors, IMailSkillServiceManager serviceManager)
            : base(dialogId)
        {
            this._services = services;
            this._serviceManager = serviceManager;
            this._accessors = accessors;
            var authPromptSettings = new OAuthPromptSettings()
            {
                ConnectionName = "msgraph",
                Text = "Authentication",
                Title = "Signin",
                Timeout = 300000, // User has 5 minutes to login.
            };

            // todo : should set culture
            this.AddDialog(new TextPrompt(Action.Prompt));
            this.AddDialog(new EventPrompt(AuthSkillmode, "tokens/response", (PromptValidator<Activity>)this.TokenResponseValidator));
            this.AddDialog(new OAuthPrompt(AuthLocalmode, authPromptSettings, (PromptValidator<TokenResponse>)this.AuthPromptValidator));
            this.AddDialog(new ConfirmPrompt(Action.TakeFurtherAction, null, Culture.English) { Style = ListStyle.SuggestedAction });
            this.AddDialog(new ChoicePrompt(Action.Choice, this.ChoiceValidator, Culture.English) { Style = ListStyle.None });
        }

        /// <summary>
        /// Make Recipient name list as a string to inject to the message when all recipient is confirmed by user.
        /// </summary>
        /// <param name="sc">dialog context.</param>
        /// <param name="accessors">Email bot accessors.</param>
        /// <returns>Recipients name as a string.</returns>
        public async Task<string> GetNameListString(WaterfallStepContext sc, EmailSkillAccessors accessors)
        {
            var state = await accessors.EmailSkillState.GetAsync(sc.Context);
            var recipients = state.Recipients;
            if (recipients.Count == 1)
            {
                return recipients.FirstOrDefault()?.EmailAddress.Name;
            }

            string result = string.Empty;
            for (int i = 0; i < recipients.Count; i++)
            {
                if (i == recipients.Count - 1)
                {
                    result += " and " + recipients[i].EmailAddress.Name;
                }
                else
                {
                    result += ", " + recipients[i].EmailAddress.Name;
                }
            }

            return result;
        }

        /// <summary>
        /// Show email list again before user select a message.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Representing the asynchronous operation.</returns>
        public async Task<DialogTurnResult> GetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (SkillDialogOptions)sc.Options;

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
                    return await sc.PromptAsync(AuthSkillmode, new PromptOptions());
                }
                else
                {
                    return await sc.PromptAsync(AuthLocalmode, new PromptOptions());
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage, this.emailBotResponseBuilder));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Check auth token when OAuth prompt dialog ended.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Representing the asynchronous operation.</returns>
        public async Task<DialogTurnResult> AfterGetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // When the user authenticates interactively we pass on the tokens/Response event which surfaces as a JObject
                // When the token is cached we get a TokenResponse object.
                var skillOptions = (SkillDialogOptions)sc.Options;
                TokenResponse tokenResponse;
                if (skillOptions != null && skillOptions.SkillMode)
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
                    var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
                    state.MsGraphToken = tokenResponse.Token;
                }

                return await sc.NextAsync();
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage, this.emailBotResponseBuilder));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Choice validator when choose a person.
        /// </summary>
        /// <param name="pc">The PromptValidatorContext.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Completed Task.</returns>
        public Task<bool> ChoiceValidator(PromptValidatorContext<FoundChoice> pc, CancellationToken cancellationToken)
        {
            var luisResult = EmailSkillHelper.GetLuisResult(pc.Context, this._accessors, this._services, cancellationToken).Result;

            var topIntent = luisResult?.TopIntent().intent;

            // If user want to show more recipient end current choice dialog and return the intent to next step.
            if (topIntent == Email.Intent.ShowNext || topIntent == Email.Intent.ShowPrevious)
            {
                // TODO: The signature of validators has been changed per the sdk team, meaning this logic will need to be executed in a different way
                if (pc.Options.Choices.Count > 5)
                {
                    // prompt.End(UpdateUserDialogOptions.UpdateReason.TooMany);
                    pc.Recognized.Succeeded = true;
                    pc.Recognized.Value = new FoundChoice() { Value = UpdateUserDialogOptions.UpdateReason.TooMany.ToString() };
                }
                else
                {
                    // prompt.End(topIntent);
                    pc.Recognized.Succeeded = true;
                    pc.Recognized.Value = new FoundChoice() { Value = topIntent.ToString() };
                }

                return Task.FromResult(true);
            }
            else
            {
                if (!pc.Recognized.Succeeded || pc.Recognized == null)
                {
                    // do nothing when not recognized.
                    return Task.FromResult(false);
                }
                else
                {
                    // prompt.End(prompt.Recognized.Value);
                    return Task.FromResult(true);
                }
            }
        }

        /// <summary>
        /// Update focused message.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>representing the asynchronous operation.</returns>
        public async Task<DialogTurnResult> UpdateMessage(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Action.Show);
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage, this.emailBotResponseBuilder));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Prompt user to select a message from the list.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>representing the asynchronous operation.</returns>
        public async Task<DialogTurnResult> PromptUpdateMessage(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(
                Action.Prompt,
                new PromptOptions() { Prompt = sc.Context.Activity.CreateReply(EmailBotResponses.NoFocusMessage, this.emailBotResponseBuilder), });
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage, this.emailBotResponseBuilder));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Get user selected message. if failed to get it, reprompt.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>representing the asynchronous operation.</returns>
        public async Task<DialogTurnResult> AfterUpdateMessage(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var luisResult = await EmailSkillHelper.GetLuisResult(sc.Context, this._accessors, this._services, cancellationToken);
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
                var focusedMessage = state.Message.FirstOrDefault();

                // todo: should set updatename reason in stepContext.Result
                if (focusedMessage == null)
                {
                    return await sc.BeginDialogAsync(Action.UpdateSelectMessage);
                }
                else
                {
                    return await sc.EndDialogAsync(true);
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage, this.emailBotResponseBuilder));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Check if user select a message, if not, start update message dialog.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>representing the asynchronous operation.</returns>
        public async Task<DialogTurnResult> CollectSelectedEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
                if (state.Message.Count == 0)
                {
                    return await sc.BeginDialogAsync(Action.UpdateSelectMessage);
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage, this.emailBotResponseBuilder));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Ask user to confirm before sending the email. Take TextResult as email content if stepContext.Result is not null.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Representing the asynchronous operation.</returns>
        public async Task<DialogTurnResult> ConfirmBeforeSending(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    if (string.IsNullOrEmpty(state.Content))
                    {
                        sc.Context.Activity.Properties.TryGetValue("OriginText", out JToken content);
                        state.Content = content != null ? content.ToString() : sc.Context.Activity.Text;
                    }
                }

                string nameListString;

                // this means reply confirm
                if (state.Recipients.FirstOrDefault() == null)
                {
                    await this.GetPreviewSubject(sc, Action.Reply);
                    nameListString = await this.GetPreviewNameListString(sc, Action.Reply);
                }
                else if (state.Subject == null)
                {
                    // this mean forward confirm
                    await this.GetPreviewSubject(sc, Action.Forward);
                    nameListString = await this.GetPreviewNameListString(sc, Action.Forward);
                }
                else
                {
                    nameListString = await this.GetPreviewNameListString(sc, Action.Send);
                }

                var emailCard = new EmailCardData
                {
                    Subject = "Subject: " + state.Subject,
                    NameList = nameListString,
                    EmailContent = "Content: " + state.Content,
                };
                var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(EmailBotResponses.ConfirmSend, "Dialogs/Shared/Resources/Cards/EmailWithOutButtonCard.json", emailCard, this.emailBotResponseBuilder);

                return await sc.PromptAsync(Action.TakeFurtherAction, new PromptOptions { Prompt = replyMessage, RetryPrompt = sc.Context.Activity.CreateReply(EmailBotResponses.ConfirmSendFailed, this.emailBotResponseBuilder), });
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage, this.emailBotResponseBuilder));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Prompt user to input email content.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Representing the asynchronous operation.</returns>
        public async Task<DialogTurnResult> CollectAdditionalText(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);

                if (string.IsNullOrEmpty(state.Content))
                {
                    var noEmailContentMessage = sc.Context.Activity.CreateReply(EmailBotResponses.NoEmailContent, this.emailBotResponseBuilder);
                    if (sc.ActiveDialog.Id == ForwardEmailDialog.Name)
                    {
                        var recipientConfirmedMessage =
                            sc.Context.Activity.CreateReply(EmailBotResponses.RecipientConfirmed, null, new StringDictionary() { { "UserName", await this.GetNameListString(sc, this._accessors) } });
                        noEmailContentMessage.Text = recipientConfirmedMessage.Text + " " + noEmailContentMessage.Text;
                        noEmailContentMessage.Speak = recipientConfirmedMessage.Speak + " " + noEmailContentMessage.Speak;
                    }

                    return await sc.PromptAsync(
                        Action.Prompt,
                        new PromptOptions { Prompt = noEmailContentMessage, });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage, this.emailBotResponseBuilder));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Prompt user input recipients name.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Representing the asynchronous operation.</returns>
        public async Task<DialogTurnResult> CollectNameList(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
                if (state.NameList.Count == 0)
                {
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(EmailBotResponses.NoRecipients, this.emailBotResponseBuilder), });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage, this.emailBotResponseBuilder));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Try find real user from the name user just input.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>representing the asynchronous operation.</returns>
        public async Task<DialogTurnResult> CollectRecipients(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);

                // ensure state.nameList is not null or empty
                if (state.NameList.Count == 0)
                {
                    var userInput = sc.Result.ToString();
                    if (userInput == null)
                    {
                        return await sc.BeginDialogAsync(ConfirmRecipientDialog.Name);
                    }

                    var nameList = userInput.Split(new[] { ",", "and" }, options: StringSplitOptions.None)
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();
                    state.NameList = nameList;
                }

                return await sc.BeginDialogAsync(ConfirmRecipientDialog.Name);
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage, this.emailBotResponseBuilder));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Get user list by name.
        /// </summary>
        /// <param name="sc">dialog context.</param>
        /// <param name="name">user's name.</param>
        /// <returns>List of Person.</returns>
        public async Task<List<Person>> GetPeopleWorkWithAsync(WaterfallStepContext sc, string name)
        {
            var result = new List<Person>();
            try
            {
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
                var token = state.MsGraphToken;
                IUserService service = this._serviceManager.InitUserService(token, state.GetUserTimeZone());

                // Get users.
                result = await service.GetPeopleAsync(name);
            }
            catch (ServiceException)
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.FindUserErrorMessage, this.emailBotResponseBuilder));
                await this.ClearConversationState(sc, this._accessors);
                await sc.CancelAllDialogsAsync();
            }

            return result;
        }

        /// <summary>
        /// Get User from org and change its type to person.
        /// </summary>
        /// <param name="sc">dialog context.</param>
        /// <param name="name">user's name.</param>
        /// <returns>List of Person.</returns>
        public async Task<List<Person>> GetUserAsync(WaterfallStepContext sc, string name)
        {
            var result = new List<Person>();
            try
            {
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
                var token = state.MsGraphToken;
                IUserService service = this._serviceManager.InitUserService(token, state.GetUserTimeZone());

                // Get users.
                var userList = await service.GetUserAsync(name);
                foreach (var user in userList)
                {
                    result.Add(user.ToPerson());
                }
            }
            catch (ServiceException)
            {
                // won't clear conversation state hear, because sometime use api is not available, like user msa account.
            }

            return result;
        }

        /// <summary>
        /// Show email step.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> ShowEmails(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);

                var messages = await this.GetMessagesAsync(sc);
                if (messages.Count > 0)
                {
                    await this.ShowMailList(sc, messages);
                    state.MessageList = messages;
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailNotFound, this.emailBotResponseBuilder));
                }

                return await sc.EndDialogAsync(true);
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage, this.emailBotResponseBuilder));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Show email but not end the dialog directly.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> ShowEmailsWithOutEnd(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);

                var messages = await this.GetMessagesAsync(sc);
                if (messages.Count > 0)
                {
                    await this.ShowMailList(sc, messages);
                    state.MessageList = messages;
                    return await sc.NextAsync();
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailNotFound, this.emailBotResponseBuilder));
                    return await sc.EndDialogAsync(true);
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage, this.emailBotResponseBuilder));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Search email by context.
        /// </summary>
        /// <param name="sc">dialog context.</param>
        /// <returns>List of Messages.</returns>
        public async Task<List<Message>> GetMessagesAsync(WaterfallStepContext sc)
        {
            var result = new List<Message>();
            try
            {
                const int pageSize = 5;
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
                var token = state.MsGraphToken;
                IMailService serivce = this._serviceManager.InitMailService(token, state.GetUserTimeZone());

                var isRead = state.IsRead;
                var isImportant = state.IsImportant;
                var startDateTime = state.StartDateTime;
                var endDateTime = state.EndDateTime;
                var directlyToMe = state.DirectlyToMe;
                var skip = state.ShowEmailIndex * pageSize;
                string mailAddress = null;
                if (!string.IsNullOrEmpty(state.SenderName))
                {
                    var searchResult = await this.GetPeopleWorkWithAsync(sc, state.SenderName);
                    var user = searchResult.FirstOrDefault();
                    if (user != null)
                    {
                        // maybe we should only show unread email from somebody
                        // isRead = true;
                        mailAddress = user.ScoredEmailAddresses.FirstOrDefault()?.Address ?? user.UserPrincipalName;
                    }
                }

                // Get user message.
                result = await serivce.GetMyMessages(startDateTime, endDateTime, isRead, isImportant, directlyToMe, mailAddress, skip);
            }
            catch (ServiceException se)
            {
                await sc.Context.SendActivityAsync(string.Format(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage, this.emailBotResponseBuilder).Text, se.Error.Code, se.Error.Message));
                await this.ClearConversationState(sc, this._accessors);
                await sc.CancelAllDialogsAsync();
            }

            return result;
        }

        /// <summary>
        /// Make the adaptive card and post it to user.
        /// </summary>
        /// <param name="sc">dialog context.</param>
        /// <param name="messages">MsGraph Message List.</param>
        /// <returns>completed task.</returns>
        public async Task ShowMailList(WaterfallStepContext sc, List<Message> messages)
        {
            var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
            var cardsData = new List<EmailCardData>();
            foreach (var message in messages)
            {
                var nameListString = $"To: {message.ToRecipients.FirstOrDefault()?.EmailAddress.Name}";
                if (message.ToRecipients != null && message.ToRecipients.Count() > 1)
                {
                    nameListString += $" + {message.ToRecipients.Count() - 1} more";
                }

                var emailCard = new EmailCardData
                {
                    Subject = message.Subject,
                    Sender = message.Sender.EmailAddress.Name,
                    NameList = nameListString,
                    EmailContent = message.BodyPreview,
                    EmailLink = message.WebLink,
                    ReceivedDateTime = message.ReceivedDateTime == null
                    ? "Not available"
                    : message.ReceivedDateTime.Value.UtcDateTime.ToRelativeString(state.GetUserTimeZone()),
                    Speak = message?.Subject + " From " + message.Sender.EmailAddress.Name,
                };
                cardsData.Add(emailCard);
            }

            var searchType = "relevant";
            if (state.IsRead)
            {
                searchType = " unread";
            }
            else if (state.IsImportant)
            {
                searchType += " important";
            }

            var reply = sc.Context.Activity.CreateAdaptiveCardGroupReply(EmailBotResponses.ShowEmailPrompt, "Dialogs/Shared/Resources/Cards/EmailCard.json", AttachmentLayoutTypes.Carousel, cardsData, this.emailBotResponseBuilder, new StringDictionary { { "SearchType", searchType } });
            await sc.Context.SendActivityAsync(reply);
        }

        /// <summary>
        /// Try clear current conversation context without token and message list.
        /// </summary>
        /// <param name="sc">The dialog context.</param>
        /// <param name="accessors">Email bot accessors.</param>
        /// <returns>bool, try result.</returns>
        public async Task ClearConversationState(WaterfallStepContext sc, EmailSkillAccessors accessors)
        {
            try
            {
                var state = await accessors.EmailSkillState.GetAsync(sc.Context);
                state.NameList.Clear();
                state.Message.Clear();
                state.Content = null;
                state.Subject = null;
                state.Recipients.Clear();
                state.ConfirmRecipientIndex = 0;
                state.ShowEmailIndex = 0;
                state.IsRead = false;
                state.IsImportant = false;
                state.StartDateTime = DateTime.UtcNow.Add(new TimeSpan(-7, 0, 0, 0));
                state.EndDateTime = DateTime.UtcNow;
                state.DirectlyToMe = false;
                state.SenderName = null;
                state.ShowRecipientIndex = 0;
                state.LuisResultPassedFromSkill = null;
            }
            catch (Exception)
            {
                // todo : should log error here.
            }
        }

        /// <summary>
        /// Set a new conversation state.
        /// </summary>
        /// <param name="sc">dialog context.</param>
        /// <param name="accessors">Email bot accessors.</param>
        /// <returns>bool, try clear the conversation dialog state and email skill state.</returns>
        public async Task ClearAllState(WaterfallStepContext sc, EmailSkillAccessors accessors)
        {
            try
            {
                await accessors.ConversationDialogState.DeleteAsync(sc.Context);
                await accessors.EmailSkillState.SetAsync(sc.Context, new EmailSkillState());
            }
            catch (Exception)
            {
                // todo : should log error here.
            }
        }

        /// <summary>
        /// Ask user to confirm the real people he/her just talk about.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Representing the asynchronous operation.</returns>
        public async Task<DialogTurnResult> UpdateUserName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
                var currentRecipientName = state.NameList[state.ConfirmRecipientIndex];

                // todo: should make a reason enum
                if (((UpdateUserDialogOptions)sc.Options).Reason == UpdateUserDialogOptions.UpdateReason.TooMany)
                {
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(EmailBotResponses.PromptTooManyPeople, null, new StringDictionary() { { "UserName", currentRecipientName } }), });
                }

                return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(EmailBotResponses.PromptPersonNotFound, null, new StringDictionary() { { "UserName", currentRecipientName } }), });
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Check if user input the recipients name.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Representing the asynchronous operation.</returns>
        public async Task<DialogTurnResult> AfterUpdateUserName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var userInput = sc.Result as string;
                if (string.IsNullOrEmpty(userInput))
                {
                    return await sc.EndDialogAsync();
                }

                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
                state.NameList[state.ConfirmRecipientIndex] = userInput;

                // should not return with value, next step use the return value for confirmation.
                return await sc.EndDialogAsync();
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage, null));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Prompt user to confirm people, if there are too many or failed to find name, restart update name.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>DialogTurnResult.</returns>
        public async Task<DialogTurnResult> ConfirmRecipient(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
                var currentRecipientName = state.NameList[state.ConfirmRecipientIndex];
                var personList = await this.GetPeopleWorkWithAsync(sc, currentRecipientName);

                // msa account can not get user from your org. and token type is not jwt.
                // TODO: find a way to check the account is msa or aad.
                var handler = new JwtSecurityTokenHandler();
                var userList = new List<Person>();
                try
                {
                    userList = await this.GetUserAsync(sc, currentRecipientName);
                }
                catch
                {
                    // do nothing when get user failed. because can not use token to ensure user use a work account.
                }

                // todo: should set updatename reason in stepContext.Result
                if (personList.Count > 10)
                {
                    return await sc.BeginDialogAsync(Action.UpdateRecipientName, new UpdateUserDialogOptions(UpdateUserDialogOptions.UpdateReason.TooMany));
                }

                if (personList.Count < 1 && userList.Count < 1)
                {
                    return await sc.BeginDialogAsync(Action.UpdateRecipientName, new UpdateUserDialogOptions(UpdateUserDialogOptions.UpdateReason.NotFound));
                }

                if (personList.Count == 1)
                {
                    var user = personList.FirstOrDefault();
                    if (user != null)
                    {
                        var result =
                            new FoundChoice()
                            {
                                Value =
                                    $"{user.DisplayName}: {user.ScoredEmailAddresses.FirstOrDefault()?.Address ?? user.UserPrincipalName}",
                            };

                        return await sc.NextAsync(result);
                    }
                }

                if (sc.Options is UpdateUserDialogOptions updateUserDialogOptions)
                {
                    state.ShowRecipientIndex = 0;
                    return await sc.BeginDialogAsync(Action.UpdateRecipientName, updateUserDialogOptions);
                }

                // TODO: should be simplify
                var selectOption = await this.GenerateOptions(personList, userList, sc);

                // If no more recipient to show, start update name flow and reset the recipient paging index.
                if (selectOption.Choices.Count == 0)
                {
                    state.ShowRecipientIndex = 0;
                    return await sc.BeginDialogAsync(Action.UpdateRecipientName, new UpdateUserDialogOptions(UpdateUserDialogOptions.UpdateReason.TooMany));
                }

                // Update prompt string to include the choices because the list style is none;
                // TODO: should be removed if use adaptive card show choices.
                var choiceString = this.GetSelectPromptString(selectOption, true);
                selectOption.Prompt.Text = choiceString;
                return await sc.PromptAsync(Action.Choice, selectOption);
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage, null));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Check if user confirm the people mentioned.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> AfterConfirmRecipient(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);

                // result is null when just update the recipient name. show recipients page should be reset.
                if (sc.Result == null)
                {
                    state.ShowRecipientIndex = 0;
                    return await sc.BeginDialogAsync(Action.ConfirmRecipient);
                }

                var choiceResult = (sc.Result as FoundChoice)?.Value.Trim('*');
                if (choiceResult != null)
                {
                    if (choiceResult == Email.Intent.ShowNext.ToString())
                    {
                        state.ShowRecipientIndex++;
                        return await sc.BeginDialogAsync(Action.ConfirmRecipient);
                    }

                    if (choiceResult == UpdateUserDialogOptions.UpdateReason.TooMany.ToString())
                    {
                        state.ShowRecipientIndex++;
                        return await sc.BeginDialogAsync(Action.ConfirmRecipient, new UpdateUserDialogOptions(UpdateUserDialogOptions.UpdateReason.TooMany));
                    }

                    if (choiceResult == Email.Intent.ShowPrevious.ToString())
                    {
                        if (state.ShowRecipientIndex > 0)
                        {
                            state.ShowRecipientIndex--;
                        }

                        return await sc.BeginDialogAsync(Action.ConfirmRecipient);
                    }

                    var recipient = new Recipient();
                    var emailAddress = new EmailAddress
                    {
                        Name = choiceResult.Split(": ")[0],
                        Address = choiceResult.Split(": ")[1],
                    };
                    recipient.EmailAddress = emailAddress;
                    if (state.Recipients.All(r => r.EmailAddress.Address != emailAddress.Address))
                    {
                        state.Recipients.Add(recipient);
                    }

                    state.ConfirmRecipientIndex++;
                }

                if (state.ConfirmRecipientIndex < state.NameList.Count)
                {
                    return await sc.BeginDialogAsync(Action.ConfirmRecipient);
                }

                return await sc.EndDialogAsync(true);
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage, null));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Generate choices for confirm user dialog.
        /// </summary>
        /// <param name="personList">people work with list search by name.</param>
        /// <param name="userList">User list search from org.</param>
        /// <param name="sc">Current dialog context.</param>
        /// <returns>ChoicePromptOptions.</returns>
        public async Task<PromptOptions> GenerateOptions(List<Person> personList, List<Person> userList, DialogContext sc)
        {
            var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
            var pageIndex = state.ShowRecipientIndex;
            var pageSize = 5;
            var skip = pageSize * pageIndex;
            var options = new PromptOptions
            {
                Choices = new List<Choice>(),
                Prompt = sc.Context.Activity.CreateReply(EmailBotResponses.ConfirmRecipient),
            };
            if (pageIndex > 0)
            {
                options.Prompt = sc.Context.Activity.CreateReply(EmailBotResponses.ConfirmRecipientNotFirstPage);
            }

            for (int i = 0; i < personList.Count; i++)
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
                options.Prompt = sc.Context.Activity.CreateReply(EmailBotResponses.ConfirmRecipientLastPage);
            }

            for (int i = 0; i < userList.Count; i++)
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

        /// <summary>
        /// Forward email step.
        /// </summary>
        /// <param name="sc">dialog context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> ForwardEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);

                    var token = state.MsGraphToken;
                    var message = state.Message;
                    var id = message.FirstOrDefault()?.Id;
                    var content = state.Content;
                    var recipients = state.Recipients;

                    IMailService service = this._serviceManager.InitMailService(token, state.GetUserTimeZone());

                    // send user message.
                    await service.ForwardMessage(id, content, recipients);

                    var nameListString = $"To: {state.Recipients.FirstOrDefault()?.EmailAddress.Name}";
                    if (state.Recipients.Count > 1)
                    {
                        nameListString += $" + {state.Recipients.Count - 1} more";
                    }

                    var emailCard = new EmailCardData
                    {
                        Subject = "Subject: FW: " + message.FirstOrDefault()?.Subject,
                        NameList = nameListString,
                        EmailContent = "Content: " + state.Content,
                    };
                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(EmailBotResponses.SentSuccessfully, "Dialogs/Shared/Resources/Cards/EmailWithOutButtonCard.json", emailCard);

                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.CancellingMessage));
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }

            await this.ClearConversationState(sc, this._accessors);
            return await sc.EndDialogAsync(true);
        }

        /// <summary>
        /// Reply email step.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> ReplyEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
                    var token = state.MsGraphToken;
                    var message = state.Message.FirstOrDefault();
                    var content = state.Content;

                    IMailService service = this._serviceManager.InitMailService(token, state.GetUserTimeZone());

                    // reply user message.
                    if (message != null)
                    {
                        await service.ReplyToMessage(message.Id, content);
                    }

                    var nameListString = $"To: {message?.From.EmailAddress.Name}";
                    if (message?.ToRecipients.Count() > 1)
                    {
                        nameListString += $" + {message.ToRecipients.Count() - 1} more";
                    }

                    var emailCard = new EmailCardData
                    {
                        Subject = "RE: " + message?.Subject,
                        NameList = nameListString,
                        EmailContent = state.Content,
                    };
                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(EmailBotResponses.SentSuccessfully, "Dialogs/Shared/Resources/Cards/EmailWithOutButtonCard.json", emailCard);

                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.CancellingMessage));
                }
            }
            catch (ServiceException se)
            {
                await sc.Context.SendActivityAsync(string.Format(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage).Text, se.Error.Code, se.Error.Message));
            }

            await this.ClearConversationState(sc, this._accessors);
            return await sc.EndDialogAsync(true);
        }

        /// <summary>
        /// Prompt user to input email subject.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Representing the asynchronous operation.</returns>
        public async Task<DialogTurnResult> CollectSubject(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
                if (state.Subject != null)
                {
                    return await sc.NextAsync();
                }

                var recipientConfirmedMessage = sc.Context.Activity.CreateReply(EmailBotResponses.RecipientConfirmed, null, new StringDictionary() { { "UserName", await this.GetNameListString(sc, this._accessors) } });
                var noSubjectMessage =
                    sc.Context.Activity.CreateReply(EmailBotResponses.NoSubject);
                noSubjectMessage.Text = recipientConfirmedMessage.Text + " " + noSubjectMessage.Text;
                noSubjectMessage.Speak += recipientConfirmedMessage.Speak + " " + noSubjectMessage.Speak;

                return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = noSubjectMessage, });
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Prompt user to input email content. get and set last user input as subject if stepContext.Result is not null.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>representing the asynchronous operation.</returns>
        public async Task<DialogTurnResult> CollectText(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out JToken subject);
                    state.Subject = subject != null ? subject.ToString() : sc.Context.Activity.Text;
                }

                if (string.IsNullOrEmpty(state.Content))
                {
                    var noMessageBodyMessage = sc.Context.Activity.CreateReply(EmailBotResponses.NoMessageBody);
                    if (sc.Result == null)
                    {
                        var recipientConfirmedMessage = sc.Context.Activity.CreateReply(EmailBotResponses.RecipientConfirmed, null, new StringDictionary() { { "UserName", await this.GetNameListString(sc, this._accessors) } });
                        noMessageBodyMessage.Text = recipientConfirmedMessage.Text + " " + noMessageBodyMessage.Text;
                        noMessageBodyMessage.Speak += recipientConfirmedMessage.Speak + " " + noMessageBodyMessage.Speak;
                    }

                    return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = noMessageBodyMessage });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Send email step.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> SendEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
                    var token = state.MsGraphToken;

                    var service = this._serviceManager.InitMailService(token, state.GetUserTimeZone());

                    // send user message.
                    await service.SendMessage(state.Content, state.Subject, state.Recipients);

                    var nameListString = $"To: {state.Recipients.FirstOrDefault()?.EmailAddress.Name}";
                    if (state.Recipients.Count > 1)
                    {
                        nameListString += $" + {state.Recipients.Count - 1} more";
                    }

                    var emailCard = new EmailCardData
                    {
                        Subject = "Subject: " + state.Subject,
                        NameList = nameListString,
                        EmailContent = "Content: " + state.Content,
                    };
                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(EmailBotResponses.SentSuccessfully, "Dialogs/Shared/Resources/Cards/EmailWithOutButtonCard.json", emailCard);

                    await sc.Context.SendActivityAsync(replyMessage);
                }

                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.ActionEnded));
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }

            await this.ClearConversationState(sc, this._accessors);
            return await sc.EndDialogAsync(true);
        }

        /// <summary>
        /// Determine if clear the context when user say 'next', 'previous', 'show emails'.
        /// </summary>
        /// <param name="sc">Current step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>representing the asynchronous operation.</returns>
        public async Task<DialogTurnResult> IfClearContextStep(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // clear context before show emails, and extract it from luis result again.
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
                var luisResult = EmailSkillHelper.GetLuisResult(sc.Context, this._accessors, this._services, cancellationToken).Result;

                var topIntent = luisResult?.TopIntent().intent;
                if (topIntent == Email.Intent.CheckMessages)
                {
                    await this.ClearConversationState(sc, this._accessors);
                    await EmailSkillHelper.DigestEmailLuisResult(sc.Context, this._accessors, luisResult);
                }

                if (topIntent == Email.Intent.ShowNext)
                {
                    state.ShowEmailIndex++;
                }

                if (topIntent == Email.Intent.ShowPrevious && state.ShowEmailIndex > 0)
                {
                    state.ShowEmailIndex--;
                }

                return await sc.NextAsync();
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Prompt user to select an email to read.
        /// </summary>
        /// <param name="sc">dialog context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> PromptToRead(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(EmailBotResponses.ReadOutPrompt) });
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Call Read Email Dialog.
        /// </summary>
        /// <param name="sc">dialog context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> CallReadEmailDialog(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Action.Read);
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Prompt user if email need to been read.
        /// </summary>
        /// <param name="sc">dialog context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> ReadEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
                var luisResult = EmailSkillHelper.GetLuisResult(sc.Context, this._accessors, this._services, cancellationToken).Result;

                var topIntent = luisResult?.TopIntent().intent;
                if (topIntent == null)
                {
                    return await sc.EndDialogAsync(true);
                }

                var message = state.Message.FirstOrDefault();
                if (topIntent == Email.Intent.ConfirmNo)
                {
                    await sc.Context.SendActivityAsync(
                        sc.Context.Activity.CreateReply(EmailBotResponses.CancellingMessage));
                    return await sc.EndDialogAsync(true);
                }
                else if (topIntent == Email.Intent.ReadAloud && message == null)
                {
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(EmailBotResponses.ReadOutPrompt), });
                }
                else if (topIntent == Email.Intent.SelectItem || (topIntent == Email.Intent.ReadAloud && message != null))
                {
                    var nameListString = $"To: {message?.ToRecipients.FirstOrDefault()?.EmailAddress.Name}";
                    if (message?.ToRecipients.Count() > 1)
                    {
                        nameListString += $" + {message.ToRecipients.Count() - 1} more";
                    }

                    var emailCard = new EmailCardData
                    {
                        Subject = message?.Subject,
                        Sender = message?.Sender.EmailAddress.Name,
                        NameList = nameListString,
                        EmailContent = message?.BodyPreview,
                        EmailLink = message?.WebLink,
                        ReceivedDateTime = message?.ReceivedDateTime == null
                            ? "Not available"
                            : message.ReceivedDateTime.Value.UtcDateTime.ToRelativeString(state.GetUserTimeZone()),
                        Speak = message?.Subject + " From " + message?.Sender.EmailAddress.Name + " " + message?.BodyPreview,
                    };
                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(EmailBotResponses.ReadOutMessage, "Dialogs/Shared/Resources/Cards/EmailDetailCard.json", emailCard);
                    await sc.Context.SendActivityAsync(replyMessage);

                    return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(EmailBotResponses.ReadOutMorePrompt) });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Determine to read the next email or end the dialog.
        /// </summary>
        /// <param name="sc">dialog context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> AfterReadOutEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
                var luisResult = EmailSkillHelper.GetLuisResult(sc.Context, this._accessors, this._services, cancellationToken).Result;

                var topIntent = luisResult?.TopIntent().intent;
                if (topIntent == null)
                {
                    return await sc.EndDialogAsync(true);
                }

                if (topIntent == Email.Intent.ReadAloud || topIntent == Email.Intent.SelectItem)
                {
                    return await sc.BeginDialogAsync(Action.Read);
                }
                else
                {
                    // return a signal for main flow need to start a new ComponentDialog.
                    return await sc.EndDialogAsync(true);
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailErrorMessage));
                await this.ClearAllState(sc, this._accessors);
                return await sc.CancelAllDialogsAsync();
            }
        }

        private string GetSelectPromptString(PromptOptions selectOption, bool containNumbers)
        {
            string result = string.Empty;
            result += selectOption.Prompt.Text + "\r\n";
            for (int i = 0; i < selectOption.Choices.Count; i++)
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

        /// <summary>
        /// Used for skill/event signin scenarios.
        /// </summary>
        /// <param name="pc">The prompt validator context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Boolean flag if validator passed.</returns>
        private Task<bool> TokenResponseValidator(PromptValidatorContext<Activity> pc, CancellationToken cancellationToken)
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

        /// <summary>
        /// Used for local signin scenarios.
        /// </summary>
        /// <param name="pc">The prompt validator context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Boolean flag if validator passed.</returns>
        private Task<bool> AuthPromptValidator(PromptValidatorContext<TokenResponse> pc, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Make Recipient name list as a string to show on confirm card before sending the email.
        /// </summary>
        /// <param name="sc">dialog context.</param>
        /// <param name="actionType">current flow type.</param>
        /// <returns>Recipients name as a string.</returns>
        private async Task<string> GetPreviewNameListString(WaterfallStepContext sc, string actionType)
        {
            var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);
            var nameListString = "To: ";

            switch (actionType)
            {
                case Action.Send:
                    nameListString += string.Join(";", state.Recipients.Select(r => r.EmailAddress.Name));
                    return nameListString;
                case Action.Reply:
                case Action.Forward:
                default:
                    nameListString += state.Recipients.FirstOrDefault()?.EmailAddress.Name;
                    if (state.Recipients.Count > 1)
                    {
                        nameListString += $" + {state.Recipients.Count - 1} more";
                    }

                    return nameListString;
            }
        }

        /// <summary>
        /// Set email subject shown on confirm card before sending the email.
        /// </summary>
        /// <param name="sc">Current dialog context.</param>
        /// <param name="actionType">Current flow type.</param>
        /// <returns>Recipients name as a string.</returns>
        private async Task<bool> GetPreviewSubject(WaterfallStepContext sc, string actionType)
        {
            try
            {
                var state = await this._accessors.EmailSkillState.GetAsync(sc.Context);

                var focusedMessage = state.Message.FirstOrDefault();

                switch (actionType)
                {
                    case Action.Reply:
                        state.Subject = focusedMessage.Subject.ToLower().StartsWith("re:") ? focusedMessage.Subject : "RE: " + focusedMessage?.Subject;
                        state.Recipients = focusedMessage.ToRecipients.ToList();
                        break;
                    case Action.Forward:
                        state.Subject = focusedMessage.Subject.ToLower().StartsWith("fw:") ? focusedMessage.Subject : "FW: " + focusedMessage?.Subject;
                        break;
                    case Action.Send:
                    default:
                        break;
                }

                return true;
            }
            catch
            {
                // todo: should log the exception.
                return false;
            }
        }
    }
}
