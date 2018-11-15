using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.ConfirmRecipient.Resources;
using EmailSkill.Dialogs.SendEmail.Resources;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkill.Dialogs.ShowEmail.Resources;
using EmailSkill.Extensions;
using EmailSkill.Util;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Graph;
using Microsoft.Recognizers.Text;
using Newtonsoft.Json.Linq;

namespace EmailSkill
{
    public class EmailSkillDialog : ComponentDialog
    {
        // Constants
        public const string SkillModeAuth = "SkillAuth";

        public EmailSkillDialog(
            string dialogId,
            ISkillConfiguration services,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IMailSkillServiceManager serviceManager)
            : base(dialogId)
        {
            Services = services;
            EmailStateAccessor = emailStateAccessor;
            DialogStateAccessor = dialogStateAccessor;
            ServiceManager = serviceManager;

            if (!Services.AuthenticationConnections.Any())
            {
                throw new Exception("You must configure an authentication connection in your bot file before using this component.");
            }

            AddDialog(new EventPrompt(SkillModeAuth, "tokens/response", TokenResponseValidator));
            AddDialog(new MultiProviderAuthDialog(services));
            AddDialog(new TextPrompt(Actions.Prompt));
            AddDialog(new ConfirmPrompt(Actions.TakeFurtherAction, null, Culture.English) { Style = ListStyle.SuggestedAction });
            AddDialog(new ChoicePrompt(Actions.Choice, ChoiceValidator, Culture.English) { Style = ListStyle.None });
        }

        protected ISkillConfiguration Services { get; set; }

        protected IStatePropertyAccessor<EmailSkillState> EmailStateAccessor { get; set; }

        protected IStatePropertyAccessor<DialogState> DialogStateAccessor { get; set; }

        protected IMailSkillServiceManager ServiceManager { get; set; }

        protected EmailSkillResponseBuilder ResponseBuilder { get; set; } = new EmailSkillResponseBuilder();

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await EmailStateAccessor.GetAsync(dc.Context);
            await DigestEmailLuisResult(dc, state.LuisResult);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await EmailStateAccessor.GetAsync(dc.Context);
            await DigestEmailLuisResult(dc, state.LuisResult);
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        // Shared steps
        protected async Task<DialogTurnResult> GetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;

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
                else if (sc.Context.Activity.ChannelId == "test")
                {
                    return await sc.NextAsync();
                }
                else
                {
                    return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions() { RetryPrompt = sc.Context.Activity.CreateReply(EmailSharedResponses.NoAuth, ResponseBuilder), });
                }
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        protected async Task<DialogTurnResult> AfterGetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // When the user authenticates interactively we pass on the tokens/Response event which surfaces as a JObject
                // When the token is cached we get a TokenResponse object.
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
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
                    var state = await EmailStateAccessor.GetAsync(sc.Context);
                    state.MsGraphToken = providerTokenResponse.TokenResponse.Token;
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        protected async Task<DialogTurnResult> UpdateMessage(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.Show);
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        protected async Task<DialogTurnResult> PromptUpdateMessage(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(
                Actions.Prompt,
                new PromptOptions() { Prompt = sc.Context.Activity.CreateReply(EmailSharedResponses.NoFocusMessage, ResponseBuilder), });
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        protected async Task<DialogTurnResult> AfterUpdateMessage(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var luisResult = state.LuisResult;

                var focusedMessage = state.Message.FirstOrDefault();

                // todo: should set updatename reason in stepContext.Result
                if (focusedMessage == null)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateSelectMessage);
                }
                else
                {
                    return await sc.EndDialogAsync(true);
                }
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        protected async Task<DialogTurnResult> CollectSelectedEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                if (state.Message.Count == 0)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateSelectMessage);
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        protected async Task<DialogTurnResult> ConfirmBeforeSending(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    if (string.IsNullOrEmpty(state.Content))
                    {
                        sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                        state.Content = content != null ? content.ToString() : sc.Context.Activity.Text;
                    }
                }

                string nameListString;

                // this means reply confirm
                if (state.Recipients.FirstOrDefault() == null)
                {
                    await GetPreviewSubject(sc, Actions.Reply);
                    nameListString = await GetPreviewNameListString(sc, Actions.Reply);
                }
                else if (state.Subject == null)
                {
                    // this mean forward confirm
                    await GetPreviewSubject(sc, Actions.Forward);
                    nameListString = await GetPreviewNameListString(sc, Actions.Forward);
                }
                else
                {
                    nameListString = await GetPreviewNameListString(sc, Actions.Send);
                }

                var emailCard = new EmailCardData
                {
                    Subject = "Subject: " + state.Subject,
                    NameList = nameListString,
                    EmailContent = "Content: " + state.Content,
                };
                var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(EmailSharedResponses.ConfirmSend, "Dialogs/Shared/Resources/Cards/EmailWithOutButtonCard.json", emailCard, ResponseBuilder);

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = replyMessage, RetryPrompt = sc.Context.Activity.CreateReply(EmailSharedResponses.ConfirmSendFailed, ResponseBuilder), });
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        protected async Task<DialogTurnResult> CollectAdditionalText(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                if (string.IsNullOrEmpty(state.Content))
                {
                    var noEmailContentMessage = sc.Context.Activity.CreateReply(EmailSharedResponses.NoEmailContent, ResponseBuilder);
                    if (sc.ActiveDialog.Id == nameof(ForwardEmailDialog))
                    {
                        var recipientConfirmedMessage =
                            sc.Context.Activity.CreateReply(EmailSharedResponses.RecipientConfirmed, null, new StringDictionary() { { "UserName", await GetNameListString(sc) } });
                        noEmailContentMessage.Text = recipientConfirmedMessage.Text + " " + noEmailContentMessage.Text;
                        noEmailContentMessage.Speak = recipientConfirmedMessage.Speak + " " + noEmailContentMessage.Speak;
                    }

                    return await sc.PromptAsync(
                        Actions.Prompt,
                        new PromptOptions { Prompt = noEmailContentMessage, });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        protected async Task<DialogTurnResult> CollectNameList(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                if (state.NameList.Count == 0)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(EmailSharedResponses.NoRecipients, ResponseBuilder), });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        protected async Task<DialogTurnResult> CollectRecipients(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                // ensure state.nameList is not null or empty
                if (state.NameList.Count == 0)
                {
                    var userInput = sc.Result.ToString();
                    if (userInput == null)
                    {
                        return await sc.BeginDialogAsync(nameof(ConfirmRecipientDialog));
                    }

                    var nameList = userInput.Split(new[] { ",", "and" }, options: StringSplitOptions.None)
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();
                    state.NameList = nameList;
                }

                return await sc.BeginDialogAsync(nameof(ConfirmRecipientDialog));
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        protected async Task<DialogTurnResult> ShowEmails(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                var messages = await GetMessagesAsync(sc);
                if (messages.Count > 0)
                {
                    await ShowMailList(sc, messages);
                    state.MessageList = messages;
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailSharedResponses.EmailNotFound, ResponseBuilder));
                }

                return await sc.EndDialogAsync(true);
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
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
            var state = await EmailStateAccessor.GetAsync(pc.Context);
            var luisResult = state.LuisResult;
            var topIntent = luisResult?.TopIntent().intent;
            var generlLuisResult = state.GeneralLuisResult;
            var generalTopIntent = generlLuisResult?.TopIntent().intent;

            // If user want to show more recipient end current choice dialog and return the intent to next step.
            if (generalTopIntent == General.Intent.Next || generalTopIntent == General.Intent.Previous)
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

                return true;
            }
            else
            {
                if (!pc.Recognized.Succeeded || pc.Recognized == null)
                {
                    // do nothing when not recognized.
                    return false;
                }
                else
                {
                    // prompt.End(prompt.Recognized.Value);
                    return true;
                }
            }
        }

        // Helpers
        protected async Task<string> GetNameListString(WaterfallStepContext sc)
        {
            var state = await EmailStateAccessor.GetAsync(sc.Context);
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

        protected async Task<PromptOptions> GenerateOptions(List<Person> personList, List<Person> userList, DialogContext sc)
        {
            var state = await EmailStateAccessor.GetAsync(sc.Context);
            var pageIndex = state.ShowRecipientIndex;
            var pageSize = 5;
            var skip = pageSize * pageIndex;

            var options = new PromptOptions
            {
                Choices = new List<Choice>(),
                Prompt = sc.Context.Activity.CreateReply(ConfirmRecipientResponses.ConfirmRecipient),
            };

            if (pageIndex > 0)
            {
                options.Prompt = sc.Context.Activity.CreateReply(ConfirmRecipientResponses.ConfirmRecipientNotFirstPage);
            }

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
                options.Prompt = sc.Context.Activity.CreateReply(ConfirmRecipientResponses.ConfirmRecipientLastPage);
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
                    result += i + 1 + "-";
                }

                result += choice.Value + "\r\n";
            }

            return result;
        }

        protected async Task<string> GetPreviewNameListString(WaterfallStepContext sc, string actionType)
        {
            var state = await EmailStateAccessor.GetAsync(sc.Context);
            var nameListString = "To: ";

            switch (actionType)
            {
                case Actions.Send:
                    nameListString += string.Join(";", state.Recipients.Select(r => r.EmailAddress.Name));
                    return nameListString;
                case Actions.Reply:
                case Actions.Forward:
                default:
                    nameListString += state.Recipients.FirstOrDefault()?.EmailAddress.Name;
                    if (state.Recipients.Count > 1)
                    {
                        nameListString += $" + {state.Recipients.Count - 1} more";
                    }

                    return nameListString;
            }
        }

        protected async Task<bool> GetPreviewSubject(WaterfallStepContext sc, string actionType)
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                var focusedMessage = state.Message.FirstOrDefault();

                switch (actionType)
                {
                    case Actions.Reply:
                        state.Subject = focusedMessage.Subject.ToLower().StartsWith("re:") ? focusedMessage.Subject : "RE: " + focusedMessage?.Subject;
                        state.Recipients = focusedMessage.ToRecipients.ToList();
                        break;
                    case Actions.Forward:
                        state.Subject = focusedMessage.Subject.ToLower().StartsWith("fw:") ? focusedMessage.Subject : "FW: " + focusedMessage?.Subject;
                        break;
                    case Actions.Send:
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

        protected async Task<List<Message>> GetMessagesAsync(WaterfallStepContext sc)
        {
            var result = new List<Message>();
            try
            {
                const int pageSize = 5;
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var token = state.MsGraphToken;
                var serivce = ServiceManager.InitMailService(token, state.GetUserTimeZone());

                var isUnreadOnly = state.IsUnreadOnly;
                var isImportant = state.IsImportant;
                var startDateTime = state.StartDateTime;
                var endDateTime = state.EndDateTime;
                var directlyToMe = state.DirectlyToMe;
                var skip = state.ShowEmailIndex * pageSize;
                string mailAddress = null;
                if (!string.IsNullOrEmpty(state.SenderName))
                {
                    var searchResult = await GetPeopleWorkWithAsync(sc, state.SenderName);
                    var user = searchResult.FirstOrDefault();
                    if (user != null)
                    {
                        // maybe we should only show unread email from somebody
                        // isRead = true;
                        mailAddress = user.ScoredEmailAddresses.FirstOrDefault()?.Address ?? user.UserPrincipalName;
                    }
                }

                // Get user message.
                result = await serivce.GetMyMessagesAsync(startDateTime, endDateTime, isUnreadOnly, isImportant, directlyToMe, mailAddress, skip);
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }

            return result;
        }

        protected async Task ShowMailList(WaterfallStepContext sc, List<Message> messages)
        {
            var state = await EmailStateAccessor.GetAsync(sc.Context);
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
            if (state.IsUnreadOnly)
            {
                searchType += " unread";
            }
            else if (state.IsImportant)
            {
                searchType += " important";
            }

            var stringToken = new StringDictionary
            {
                { "SearchType", searchType },
                { "EmailListDetails", SpeakHelper.ToSpeechEmailListString(sc, messages) },
            };

            var reply = sc.Context.Activity.CreateAdaptiveCardGroupReply(EmailSharedResponses.ShowEmailPrompt, "Dialogs/Shared/Resources/Cards/EmailCard.json", AttachmentLayoutTypes.Carousel, cardsData, ResponseBuilder, stringToken);
            await sc.Context.SendActivityAsync(reply);
        }

        protected async Task<List<Person>> GetPeopleWorkWithAsync(WaterfallStepContext sc, string name)
        {
            var result = new List<Person>();
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var token = state.MsGraphToken;
                var service = ServiceManager.InitUserService(token, state.GetUserTimeZone());

                // Get users.
                result = await service.GetPeopleAsync(name);
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }

            return result;
        }

        protected async Task<List<Person>> GetUserAsync(WaterfallStepContext sc, string name)
        {
            var result = new List<Person>();
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var token = state.MsGraphToken;
                var service = ServiceManager.InitUserService(token, state.GetUserTimeZone());

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

        protected async Task ClearConversationState(WaterfallStepContext sc)
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                state.NameList.Clear();
                state.Message.Clear();
                state.Content = null;
                state.Subject = null;
                state.Recipients.Clear();
                state.ConfirmRecipientIndex = 0;
                state.ShowEmailIndex = 0;
                state.IsUnreadOnly = true;
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
                throw;
            }
        }

        protected async Task ClearAllState(WaterfallStepContext sc)
        {
            try
            {
                await DialogStateAccessor.DeleteAsync(sc.Context);
                await EmailStateAccessor.SetAsync(sc.Context, new EmailSkillState());
            }
            catch (Exception)
            {
                // todo : should log error here.
                throw;
            }
        }

        protected async Task DigestEmailLuisResult(DialogContext dc, Email luisResult)
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(dc.Context);

                if (dc.Context.Activity.Text != null)
                {
                    var words = dc.Context.Activity.Text.Split(' ');
                    foreach (var word in words)
                    {
                        switch (word)
                        {
                            case "high":
                            case "important":
                                state.IsImportant = true;
                                break;
                            case "unread":
                                state.IsUnreadOnly = true;
                                break;
                        }
                    }
                }

                var entity = luisResult.Entities;
                if (entity.ContactName != null)
                {
                    foreach (var name in entity.ContactName)
                    {
                        if (!state.NameList.Contains(name))
                        {
                            state.NameList.Add(name);
                        }
                    }
                }

                if (entity.EmailSubject != null)
                {
                    state.Subject = entity.EmailSubject[0];
                }

                if (entity.Message != null)
                {
                    state.Content = entity.Message[0];
                }

                if (entity.SenderName != null)
                {
                    state.SenderName = entity.SenderName[0];
                }

                if (entity.datetime != null)
                {
                    // todo: enable date time
                    // case "builtin.datetimeV2.date":
                    // case "builtin.datetimeV2.datetime":
                    // foreach (dynamic value in resolution["values"])
                    // {
                    //    var start = value["value"].ToString();
                    //    var dateTime = DateTime.Parse(start);
                    //    state.StartDateTime = dateTime;
                    //    state.EndDateTime = DateTime.UtcNow;
                    // }

                    // break;
                    // case "builtin.datetimeV2.datetimerange":
                    // foreach (dynamic value in resolution["values"])
                    // {
                    //    var start = value["start"].ToString();
                    //    var end = value["end"].ToString();
                    //    state.StartDateTime = DateTime.Parse(start);
                    //    state.EndDateTime = DateTime.Parse(end);
                    // }

                    // break;
                }

                if (entity.ordinal != null)
                {
                    try
                    {
                        var emailList = state.MessageList;
                        var value = entity.ordinal[0];
                        if (Math.Abs(value - (int)value) < double.Epsilon)
                        {
                            var num = (int)value;
                            if (emailList != null && num > 0 && num <= emailList.Count)
                            {
                                state.Message.Clear();
                                state.Message.Add(emailList[num - 1]);
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }

                if (entity.number != null && entity.ordinal == null)
                {
                    try
                    {
                        var emailList = state.MessageList;
                        var value = entity.number[0];
                        if (Math.Abs(value - (int)value) < double.Epsilon)
                        {
                            var num = (int)value;
                            if (emailList != null && num > 0 && num <= emailList.Count)
                            {
                                state.Message.Clear();
                                state.Message.Add(emailList[num - 1]);
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
            catch
            {
                // put log here
            }
        }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        protected async Task<Exception> HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailSharedResponses.EmailErrorMessage));
            await ClearAllState(sc);
            await sc.CancelAllDialogsAsync();
            return ex;
        }
    }
}
