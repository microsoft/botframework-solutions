using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.FindContact;
using EmailSkill.Dialogs.FindContact.Resources;
using EmailSkill.Dialogs.ForwardEmail;
using EmailSkill.Dialogs.Shared.DialogOptions;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkill.Dialogs.Shared.Resources.Cards;
using EmailSkill.Dialogs.Shared.Resources.Strings;
using EmailSkill.Extensions;
using EmailSkill.Model;
using EmailSkill.ServiceClients;
using EmailSkill.Util;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Data;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Prompts;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Graph;
using Microsoft.Recognizers.Text;
using Newtonsoft.Json.Linq;

namespace EmailSkill.Dialogs.Shared
{
    public class EmailSkillDialog : ComponentDialog
    {
        // Constants
        public const string SkillModeAuth = "SkillAuth";

        public EmailSkillDialog(
            string dialogId,
            SkillConfigurationBase services,
            ResponseManager responseManager,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(dialogId)
        {
            Services = services;
            ResponseManager = responseManager;
            EmailStateAccessor = emailStateAccessor;
            DialogStateAccessor = dialogStateAccessor;
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
        }

        protected EmailSkillDialog(string dialogId)
            : base(dialogId)
        {
        }

        protected SkillConfigurationBase Services { get; set; }

        protected IStatePropertyAccessor<EmailSkillState> EmailStateAccessor { get; set; }

        protected IStatePropertyAccessor<DialogState> DialogStateAccessor { get; set; }

        protected IServiceManager ServiceManager { get; set; }

        protected ResponseManager ResponseManager { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await EmailStateAccessor.GetAsync(dc.Context);
            await DigestEmailLuisResult(dc, state.LuisResult, true);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await EmailStateAccessor.GetAsync(dc.Context);
            await DigestEmailLuisResult(dc, state.LuisResult, false);
            return await base.OnContinueDialogAsync(dc, cancellationToken);
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

        // Shared steps
        protected virtual async Task<DialogTurnResult> IfClearContextStep(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                var luisResult = state.LuisResult;
                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalLuisResult = state.GeneralLuisResult;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (skillOptions == null || !skillOptions.SubFlowMode)
                {
                    // Clear email state data
                    await ClearConversationState(sc);
                    await DigestEmailLuisResult(sc, luisResult, true);
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected virtual async Task<DialogTurnResult> SetDisplayConfig(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                if (skillOptions == null || !skillOptions.SubFlowMode)
                {
                    // For forward/reply/display email, display all emails by default.
                    state.IsUnreadOnly = false;
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }


        protected virtual async Task<DialogTurnResult> PagingStep(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                var luisResult = state.LuisResult;
                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalLuisResult = state.GeneralLuisResult;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (skillLuisResult == EmailLU.Intent.ShowNext || generalTopIntent == General.Intent.Next)
                {
                    state.ShowEmailIndex++;
                    state.ReadEmailIndex = 0;
                }
                else if ((skillLuisResult == EmailLU.Intent.ShowPrevious || generalTopIntent == General.Intent.Previous) && state.ShowEmailIndex >= 0)
                {
                    state.ShowEmailIndex--;
                    state.ReadEmailIndex = 0;
                }
                else if (IsReadMoreIntent(generalTopIntent, sc.Context.Activity.Text))
                {
                    if (state.MessageList.Count <= ConfigData.GetInstance().MaxReadSize)
                    {
                        state.ShowEmailIndex++;
                        state.ReadEmailIndex = 0;
                    }
                    else
                    {
                        state.ReadEmailIndex++;
                    }
                }

                await DigestFocusEmailAsync(sc);

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

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
                else
                {
                    var retry = ResponseManager.GetResponse(EmailSharedResponses.NoAuth);
                    return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions() { RetryPrompt = retry });
                }
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
                    state.Token = providerTokenResponse.TokenResponse.Token;

                    var provider = providerTokenResponse.AuthenticationProvider;

                    if (provider == OAuthProvider.AzureAD)
                    {
                        state.MailSourceType = MailSource.Microsoft;
                    }
                    else if (provider == OAuthProvider.Google)
                    {
                        state.MailSourceType = MailSource.Google;
                    }
                    else
                    {
                        throw new Exception($"The authentication provider \"{provider.ToString()}\" is not support by the Email Skill.");
                    }
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> UpdateMessage(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                return await sc.BeginDialogAsync(Actions.Show, skillOptions);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> PromptUpdateMessage(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                if (state.MessageList.Count == 0)
                {
                    return await sc.EndDialogAsync(true);
                }

                return await sc.PromptAsync(
                    Actions.Prompt,
                    new PromptOptions() { Prompt = ResponseManager.GetResponse(EmailSharedResponses.NoFocusMessage) });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterUpdateMessage(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var luisResult = state.LuisResult;

                await DigestFocusEmailAsync(sc);
                var focusedMessage = state.Message.FirstOrDefault();

                // todo: should set updatename reason in stepContext.Result
                if (focusedMessage == null)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateSelectMessage, skillOptions);
                }
                else
                {
                    return await sc.EndDialogAsync(true);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CollectSelectedEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;

                if (state.Message == null || state.Message.Count() == 0)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateSelectMessage, skillOptions);
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterCollectSelectedEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                // End the dialog when there is no focused email
                if (state.Message.Count == 0)
                {
                    return await sc.EndDialogAsync(true);
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ConfirmBeforeSending(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (sc.Result != null && sc.Result is bool)
                {
                    var checkLastStepSuccess = (bool)sc.Result;
                    if (!checkLastStepSuccess)
                    {
                        return await sc.EndDialogAsync(true, cancellationToken);
                    }
                }

                var state = await EmailStateAccessor.GetAsync(sc.Context);
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
                    Subject = state.Subject.Equals(EmailCommonStrings.EmptySubject) ? null : string.Format(EmailCommonStrings.SubjectFormat, state.Subject),
                    NameList = string.Format(EmailCommonStrings.ToFormat, nameListString),
                    EmailContent = state.Content.Equals(EmailCommonStrings.EmptyContent) ? null : string.Format(EmailCommonStrings.ContentFormat, state.Content),
                };

                var speech = SpeakHelper.ToSpeechEmailSendDetailString(state.Subject, nameListString, state.Content);
                var tokens = new StringDictionary
                {
                    { "EmailDetails", speech },
                };

                var prompt = ResponseManager.GetCardResponse(
                    EmailSharedResponses.ConfirmSend,
                    new Card("EmailWithOutButtonCard", emailCard),
                    tokens);

                var retry = ResponseManager.GetResponse(EmailSharedResponses.ConfirmSendFailed);

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = prompt, RetryPrompt = retry });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CollectAdditionalText(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                if (string.IsNullOrEmpty(state.Content))
                {
                    var noEmailContentMessage = ResponseManager.GetResponse(EmailSharedResponses.NoEmailContent);
                    if (sc.ActiveDialog.Id == nameof(ForwardEmailDialog))
                    {
                        if (state.Recipients.Count == 0 || state.Recipients == null)
                        {
                            state.FirstRetryInFindContact = true;
                            return await sc.EndDialogAsync();
                        }

                        var recipientConfirmedMessage = ResponseManager.GetResponse(EmailSharedResponses.RecipientConfirmed, new StringDictionary() { { "UserName", await GetNameListStringAsync(sc) } });
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
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CollectRecipient(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                if (state.Recipients.Count() == 0)
                {
                    return await sc.BeginDialogAsync(Actions.CollectRecipient, skillOptions);
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> PromptRecipientCollection(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                if (state.IsNoRecipientAvailable())
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(EmailSharedResponses.NoRecipients) });
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

        protected async Task<DialogTurnResult> GetRecipients(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                // ensure state.nameList is not null or empty
                if (state.IsNoRecipientAvailable())
                {
                    var userInput = sc.Result.ToString();
                    if (string.IsNullOrWhiteSpace(userInput))
                    {
                        return await sc.BeginDialogAsync(Actions.CollectRecipient, skillOptions);
                    }

                    if (IsEmail(userInput))
                    {
                        state.EmailList.Add(userInput);
                    }
                    else
                    {
                        var nameList = userInput.Split(new[] { ",", CommonStrings.And }, options: StringSplitOptions.None)
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();
                        state.NameList = nameList;
                    }
                }

                state.FirstEnterFindContact = true;
                return await sc.BeginDialogAsync(nameof(FindContactDialog), skillOptions);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterCollectAdditionalText(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var contentInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                    if (!EmailCommonPhrase.GetIsSkip(contentInput))
                    {
                        state.Content = contentInput;
                    }
                    else
                    {
                        state.Content = EmailCommonStrings.EmptyContent;
                    }
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ShowEmails(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                var (messages, totalCount) = await GetMessagesAsync(sc);

                // Get display messages
                var displayMessages = new List<Message>();
                var startIndex = ConfigData.GetInstance().MaxReadSize * state.ReadEmailIndex;
                for (var i = startIndex; i < messages.Count(); i++)
                {
                    displayMessages.Add(messages[i]);
                }

                if (displayMessages.Count > 0)
                {
                    state.MessageList = displayMessages;
                    state.Message.Clear();
                    state.Message.Add(displayMessages[0]);

                    await ShowMailList(sc, displayMessages, totalCount, cancellationToken);
                    return await sc.NextAsync();
                }
                else
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(EmailSharedResponses.EmailNotFound));
                }

                return await sc.EndDialogAsync(true);
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

        protected async Task<DialogTurnResult> SearchEmailsFromList(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                var messages = state.MessageList;

                if (((state.LuisResult.Entities.ordinal != null) && (state.LuisResult.Entities.ordinal.Count() > 0))
                    || ((state.LuisResult.Entities.number != null) && (state.LuisResult.Entities.number.Count() > 0)))
                {
                    // Search by ordinal and number
                    if (state.MessageList.Count > state.UserSelectIndex)
                    {
                        state.Message.Clear();
                        state.Message.Add(state.MessageList[state.UserSelectIndex]);
                    }
                }
                else
                {
                    // Search by condition
                    var searchSender = state.SenderName?.ToLowerInvariant();
                    var searchSubject = state.SearchTexts?.ToLowerInvariant();
                    var searchUserInput = userInput?.ToLowerInvariant();

                    // Get display messages
                    var displayMessages = new List<Message>();
                    for (int i = 0; i < messages.Count(); i++)
                    {
                        var messageSender = messages[i].Sender?.EmailAddress?.Name?.ToLowerInvariant();
                        var messageSubject = messages[i].Subject?.ToLowerInvariant();

                        if (messageSender != null
                            && (((searchSender != null) && messageSender.Contains(searchSender))
                            || ((searchUserInput != null) && messageSender.Contains(searchUserInput))))
                        {
                            displayMessages.Add(messages[i]);
                        }
                        else if (messageSubject != null
                            && (((searchSubject != null) && messageSubject.Contains(searchSubject))
                            || ((searchUserInput != null) && messageSubject.Contains(searchUserInput))))
                        {
                            displayMessages.Add(messages[i]);
                        }
                    }

                    state.MessageList = displayMessages;
                    state.Message.Clear();
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

        // Helpers
        protected async Task<string> GetNameListStringAsync(WaterfallStepContext sc)
        {
            var state = await EmailStateAccessor.GetAsync(sc?.Context);
            var recipients = state.Recipients;

            if (recipients == null || recipients.Count == 0)
            {
                throw new NoRecipientsException();
            }
            else if (recipients.Count == 1)
            {
                return recipients.FirstOrDefault()?.EmailAddress.Name + ": " + recipients.FirstOrDefault()?.EmailAddress.Address;
            }

            string result = recipients.FirstOrDefault()?.EmailAddress.Name + ": " + recipients.FirstOrDefault()?.EmailAddress.Address;
            for (int i = 1; i < recipients.Count; i++)
            {
                if (i == recipients.Count - 1)
                {
                    result += string.Format(CommonStrings.SeparatorFormat, CommonStrings.And) + recipients[i].EmailAddress.Name + ": " + recipients[i].EmailAddress.Address;
                }
                else
                {
                    result += ", " + recipients[i].EmailAddress.Name + ": " + recipients[i].EmailAddress.Address;
                }
            }

            return result;
        }

        protected string GetSelectPromptString(PromptOptions selectOption, bool containNumbers)
        {
            var result = string.Empty;
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
            var nameListString = string.Empty;

            switch (actionType)
            {
                case Actions.Send:
                    nameListString = DisplayHelper.ToDisplayRecipientsString(state.Recipients);
                    break;
                case Actions.Reply:
                case Actions.Forward:
                case Actions.Delete:
                default:
                    nameListString = DisplayHelper.ToDisplayRecipientsString_Summay(state.Recipients);
                    break;
            }

            return nameListString;
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
                        state.Subject = focusedMessage.Subject.ToLower().StartsWith(EmailCommonStrings.Reply) ? focusedMessage.Subject : string.Format(EmailCommonStrings.ReplyReplyFormat, focusedMessage?.Subject);
                        state.Recipients = focusedMessage.ToRecipients.ToList();
                        break;
                    case Actions.Forward:
                        state.Subject = focusedMessage.Subject.ToLower().StartsWith(EmailCommonStrings.Forward) ? focusedMessage.Subject : string.Format(EmailCommonStrings.ForwardReplyFormat, focusedMessage?.Subject);
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

        protected async Task<(List<Message>, int)> GetMessagesAsync(WaterfallStepContext sc)
        {
            var result = new List<Message>();

            var pageSize = ConfigData.GetInstance().MaxDisplaySize;
            var state = await EmailStateAccessor.GetAsync(sc.Context);
            var token = state.Token;
            var serivce = ServiceManager.InitMailService(token, state.GetUserTimeZone(), state.MailSourceType);

            var isUnreadOnly = state.IsUnreadOnly;
            var isImportant = state.IsImportant;
            var startDateTime = state.StartDateTime;
            var endDateTime = state.EndDateTime;
            var directlyToMe = state.DirectlyToMe;
            var skip = state.ShowEmailIndex * pageSize;
            string mailAddress = null;
            if (!string.IsNullOrEmpty(state.SenderName))
            {
                var searchResult = await GetPeopleWorkWithAsync(sc.Context, state.SenderName);
                var user = searchResult.FirstOrDefault();
                if (user != null)
                {
                    // maybe we should only show unread email from somebody
                    // isRead = true;
                    mailAddress = user.ScoredEmailAddresses.FirstOrDefault()?.Address ?? user.UserPrincipalName;
                }
            }

            // Get user message.
            result = await serivce.GetMyMessagesAsync(startDateTime, endDateTime, isUnreadOnly, isImportant, directlyToMe, mailAddress);

            // Go back to last page if next page didn't get anything
            if (skip >= result.Count)
            {
                skip = (state.ShowEmailIndex - 1) * pageSize;
            }

            // get messages for current page
            var filteredResult = new List<Message>();
            for (var i = 0; i < result.Count; i++)
            {
                if (skip > 0)
                {
                    skip--;
                }
                else
                {
                    if (filteredResult.Count < pageSize)
                    {
                        filteredResult.Add(result[i]);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return (filteredResult, result.Count);
        }

        protected async Task ShowMailList(WaterfallStepContext sc, List<Message> messages, int totalCount, CancellationToken cancellationToken = default(CancellationToken))
        {
            var updatedMessages = new List<Message>();
            var state = await EmailStateAccessor.GetAsync(sc.Context);

            var cards = new List<Card>();
            foreach (var message in messages)
            {
                var nameListString = DisplayHelper.ToDisplayRecipientsString_Summay(message.ToRecipients);

                var emailCard = new EmailCardData
                {
                    Subject = message.Subject,
                    Sender = message.Sender.EmailAddress.Name,
                    NameList = string.Format(EmailCommonStrings.ToFormat, nameListString),
                    EmailContent = message.BodyPreview,
                    EmailLink = message.WebLink,
                    ReceivedDateTime = message.ReceivedDateTime == null
                    ? CommonStrings.NotAvailable
                    : message.ReceivedDateTime.Value.UtcDateTime.ToRelativeString(state.GetUserTimeZone()),
                    Speak = SpeakHelper.ToSpeechEmailDetailOverallString(message, state.GetUserTimeZone()),
                };
                cards.Add(new Card("EmailCard", emailCard));
                updatedMessages.Add(message);
            }

            var searchType = EmailCommonStrings.Relevant;
            if (state.IsUnreadOnly)
            {
                searchType = string.Format(EmailCommonStrings.RelevantFormat, EmailCommonStrings.Unread);
            }
            else if (state.IsImportant)
            {
                searchType = string.Format(EmailCommonStrings.RelevantFormat, EmailCommonStrings.Important);
            }

            var tokens = new StringDictionary
            {
                { "TotalCount", totalCount.ToString() },
                { "EmailListDetails", SpeakHelper.ToSpeechEmailListString(updatedMessages, state.GetUserTimeZone(), ConfigData.GetInstance().MaxReadSize) },
            };

            var reply = ResponseManager.GetCardResponse(EmailSharedResponses.ShowEmailPrompt, cards, tokens);

            if (state.ShowEmailIndex == 0)
            {
                if (updatedMessages.Count == 1)
                {
                    reply = ResponseManager.GetCardResponse(EmailSharedResponses.ShowOneEmailPrompt, cards, tokens);
                }
            }
            else
            {
                reply = ResponseManager.GetCardResponse(EmailSharedResponses.ShowEmailPrompt_OtherPage, cards, tokens);
                if (updatedMessages.Count == 1)
                {
                    reply = ResponseManager.GetCardResponse(EmailSharedResponses.ShowOneEmailPrompt_OtherPage, cards, tokens);
                }
            }

            int maxPage = (totalCount / ConfigData.GetInstance().MaxDisplaySize) + (totalCount % ConfigData.GetInstance().MaxDisplaySize > 0 ? 1 : 0) - 1;
            if (state.ShowEmailIndex < 0)
            {
                var pagingInfo = ResponseManager.GetResponse(EmailSharedResponses.FirstPageAlready);
                reply.Text = pagingInfo.Text + reply.Text;
                reply.Speak = pagingInfo.Speak + reply.Speak;
                state.ShowEmailIndex = 0;
            }
            else if (state.ShowEmailIndex > maxPage)
            {
                var pagingInfo = ResponseManager.GetResponse(EmailSharedResponses.LastPageAlready);
                reply.Text = pagingInfo.Text + reply.Text;
                reply.Speak = pagingInfo.Speak + reply.Speak;
                state.ShowEmailIndex--;
            }

            await sc.Context.SendActivityAsync(reply);
            return;
        }

        protected async Task<List<Person>> GetPeopleWorkWithAsync(ITurnContext context, string name)
        {
            var result = new List<Person>();
            var state = await EmailStateAccessor.GetAsync(context);
            var token = state.Token;
            var service = ServiceManager.InitUserService(token, state.GetUserTimeZone(), state.MailSourceType);

            // Get users.
            return await service.GetPeopleAsync(name);
        }

        protected async Task<List<Person>> GetUserAsync(ITurnContext context, string name)
        {
            var result = new List<Person>();
            try
            {
                var state = await EmailStateAccessor.GetAsync(context);
                var token = state.Token;
                var service = ServiceManager.InitUserService(token, state.GetUserTimeZone(), state.MailSourceType);

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

        protected async Task<List<Person>> GetContactsAsync(ITurnContext context, string name)
        {
            var result = new List<Person>();
            var state = await EmailStateAccessor.GetAsync(context);
            var token = state.Token;
            var service = ServiceManager.InitUserService(token, state.GetUserTimeZone(), state.MailSourceType);

            // Get users.
            var contactsList = await service.GetContactsAsync(name);
            foreach (var contact in contactsList)
            {
                result.Add(contact.ToPerson());
            }

            return result;
        }

        protected async Task ClearConversationState(WaterfallStepContext sc)
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                // Keep email display and focus data when in sub flow mode
                if (skillOptions == null || !skillOptions.SubFlowMode)
                {
                    state.Message.Clear();
                    state.ShowEmailIndex = 0;
                    state.IsUnreadOnly = true;
                    state.IsImportant = false;
                    state.StartDateTime = DateTime.UtcNow.Add(new TimeSpan(-7, 0, 0, 0));
                    state.EndDateTime = DateTime.UtcNow;
                    state.DirectlyToMe = false;
                    state.UserSelectIndex = -1;
                }

                state.NameList.Clear();
                state.Content = null;
                state.Subject = null;
                state.Recipients.Clear();
                state.ConfirmRecipientIndex = 0;
                state.SenderName = null;
                state.EmailList = new List<string>();
                state.ShowRecipientIndex = 0;
                state.LuisResultPassedFromSkill = null;
                state.ReadEmailIndex = 0;
                state.ReadRecipientIndex = 0;
                state.RecipientChoiceList.Clear();
                state.SearchTexts = null;
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

        protected async Task DigestFocusEmailAsync(WaterfallStepContext sc)
        {
            var state = await EmailStateAccessor.GetAsync(sc.Context);

            // Get focus message if any
            if (state.MessageList != null && state.UserSelectIndex >= 0 && state.UserSelectIndex < state.MessageList.Count())
            {
                state.Message.Clear();
                state.Message.Add(state.MessageList[state.UserSelectIndex]);
            }
        }

        protected async Task DigestEmailLuisResult(DialogContext dc, EmailLU luisResult, bool isBeginDialog)
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(dc.Context);

                var intent = luisResult.TopIntent().intent;

                var entity = luisResult.Entities;

                if (entity != null)
                {
                    if (entity.ordinal != null)
                    {
                        try
                        {
                            var emailList = state.MessageList;
                            var value = entity.ordinal[0];
                            if (Math.Abs(value - (int)value) < double.Epsilon)
                            {
                                state.UserSelectIndex = (int)value - 1;
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    if (entity.number != null && (entity.ordinal == null || entity.ordinal.Length == 0))
                    {
                        try
                        {
                            var emailList = state.MessageList;
                            var value = entity.number[0];
                            if (Math.Abs(value - (int)value) < double.Epsilon)
                            {
                                state.UserSelectIndex = (int)value - 1;
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
                        case EmailLU.Intent.CheckMessages:
                        case EmailLU.Intent.SearchMessages:
                        case EmailLU.Intent.ReadAloud:
                            {
                                // Get email search type
                                if (dc.Context.Activity.Text != null)
                                {
                                    var words = dc.Context.Activity.Text.Split(' ');
                                    {
                                        foreach (var word in words)
                                        {
                                            var lowerInput = word.ToLower();

                                            if (lowerInput.Contains(EmailCommonStrings.High) || lowerInput.Contains(EmailCommonStrings.Important))
                                            {
                                                state.IsImportant = true;
                                            }
                                            else if (lowerInput.Contains(EmailCommonStrings.Unread))
                                            {
                                                state.IsUnreadOnly = true;
                                            }
                                            else if (lowerInput.Contains(EmailCommonStrings.All))
                                            {
                                                state.IsUnreadOnly = false;
                                            }
                                        }
                                    }
                                }

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

                                if (entity.email != null)
                                {
                                    // As luis result for email address often contains extra spaces for word breaking
                                    // (e.g. send email to test@test.com, email address entity will be test @ test . com)
                                    // So use original user input as email address.
                                    var rawEntity = luisResult.Entities._instance.email;
                                    foreach (var emailAddress in rawEntity)
                                    {
                                        var email = luisResult.Text.Substring(emailAddress.StartIndex, emailAddress.EndIndex - emailAddress.StartIndex);
                                        if (IsEmail(email) && !state.EmailList.Contains(email))
                                        {
                                            state.EmailList.Add(email);
                                        }
                                    }
                                }

                                if (entity.SenderNamePattern != null)
                                {
                                    state.SenderName = entity.SenderNamePattern[0];
                                }
                                else if (entity.SenderName != null)
                                {
                                    state.SenderName = entity.SenderName[0];
                                }

                                if (entity.EmailSubjectPattern != null)
                                {
                                    state.SearchTexts = entity.EmailSubjectPattern[0];
                                }
                                else if (entity.SearchTexts != null)
                                {
                                    state.SearchTexts = entity.SearchTexts[0];
                                }
                                else if (entity.EmailSubject != null)
                                {
                                    state.SearchTexts = entity.EmailSubject[0];
                                }

                                break;
                            }

                        case EmailLU.Intent.SendEmail:
                        case EmailLU.Intent.Forward:
                        case EmailLU.Intent.Reply:
                            {
                                if (entity.EmailSubject != null)
                                {
                                    state.Subject = entity.EmailSubject[0];
                                }

                                if (entity.Message != null)
                                {
                                    state.Content = entity.Message[0];
                                }

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

                                if (entity.email != null)
                                {
                                    // As luis result for email address often contains extra spaces for word breaking
                                    // (e.g. send email to test@test.com, email address entity will be test @ test . com)
                                    // So use original user input as email address.
                                    var rawEntity = luisResult.Entities._instance.email;
                                    foreach (var emailAddress in rawEntity)
                                    {
                                        var email = luisResult.Text.Substring(emailAddress.StartIndex, emailAddress.EndIndex - emailAddress.StartIndex);
                                        if (IsEmail(email) && !state.EmailList.Contains(email))
                                        {
                                            state.EmailList.Add(email);
                                        }
                                    }
                                }

                                if (entity.SenderName != null)
                                {
                                    state.SenderName = entity.SenderName[0];

                                    // Clear focus email if there is any.
                                    state.Message.Clear();
                                }

                                break;
                            }

                        default:
                            break;
                    }
                }
            }
            catch
            {
                // put log here
            }
        }

        protected bool IsEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            return Regex.IsMatch(email, @"^\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$");
        }

        protected bool IsReadMoreIntent(General.Intent? topIntent, string userInput)
        {
            var isReadMoreUserInput = userInput == null ? false : userInput.ToLowerInvariant().Contains(CommonStrings.More);
            return topIntent == General.Intent.ReadMore && isReadMoreUserInput;
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
            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(EmailSharedResponses.EmailErrorMessage));

            // clear state
            await ClearAllState(sc);
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
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(EmailSharedResponses.EmailErrorMessage_BotProblem));
            }
            else
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(EmailSharedResponses.EmailErrorMessage));
            }

            // clear state
            await ClearAllState(sc);
        }

        [Serializable]
        protected class NoRecipientsException : Exception
        {
            public NoRecipientsException()
            {
            }

            public NoRecipientsException(string message)
                : base(message)
            {
            }

            public NoRecipientsException(string message, Exception innerException)
                : base(message, innerException)
            {
            }

            protected NoRecipientsException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
        }
    }
}