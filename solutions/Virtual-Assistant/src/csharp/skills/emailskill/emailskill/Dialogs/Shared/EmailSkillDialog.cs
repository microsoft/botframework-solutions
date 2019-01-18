using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.ConfirmRecipient;
using EmailSkill.Dialogs.ConfirmRecipient.Resources;
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
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Prompts;
using Microsoft.Bot.Solutions.Resources;
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
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(dialogId)
        {
            Services = services;
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

        protected EmailSkillResponseBuilder ResponseBuilder { get; set; } = new EmailSkillResponseBuilder();

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

        protected virtual async Task<DialogTurnResult> PagingStep(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                var luisResult = state.LuisResult;
                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalLuisResult = state.GeneralLuisResult;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (skillLuisResult == Email.Intent.None && generalTopIntent == General.Intent.Next)
                {
                    state.ShowEmailIndex++;
                    state.ReadEmailIndex = 0;
                }
                else if (skillLuisResult == Email.Intent.None && generalTopIntent == General.Intent.Previous && state.ShowEmailIndex > 0)
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
                    return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions() { RetryPrompt = sc.Context.Activity.CreateReply(EmailSharedResponses.NoAuth, ResponseBuilder), });
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
                    new PromptOptions() { Prompt = sc.Context.Activity.CreateReply(EmailSharedResponses.NoFocusMessage, ResponseBuilder) });
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
                    Subject = string.Format(EmailCommonStrings.SubjectFormat, state.Subject),
                    NameList = string.Format(EmailCommonStrings.ToFormat, nameListString),
                    EmailContent = string.Format(EmailCommonStrings.ContentFormat, state.Content),
                };

                var speech = SpeakHelper.ToSpeechEmailSendDetailString(state.Subject, nameListString, state.Content);
                var stringToken = new StringDictionary
                {
                    { "EmailDetails", speech },
                };
                var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(EmailSharedResponses.ConfirmSend, "Dialogs/Shared/Resources/Cards/EmailWithOutButtonCard.json", emailCard, ResponseBuilder, stringToken);

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = replyMessage, RetryPrompt = sc.Context.Activity.CreateReply(EmailSharedResponses.ConfirmSendFailed, ResponseBuilder), });
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
                    var noEmailContentMessage = sc.Context.Activity.CreateReply(EmailSharedResponses.NoEmailContent, ResponseBuilder);
                    if (sc.ActiveDialog.Id == nameof(ForwardEmailDialog))
                    {
                        var recipientConfirmedMessage =
                            sc.Context.Activity.CreateReply(EmailSharedResponses.RecipientConfirmed, null, new StringDictionary() { { "UserName", await GetNameListStringAsync(sc) } });
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
                return await sc.BeginDialogAsync(Actions.CollectRecipient, skillOptions);
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
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(EmailSharedResponses.NoRecipients, ResponseBuilder), });
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

                return await sc.BeginDialogAsync(nameof(ConfirmRecipientDialog), skillOptions);
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
                for (int i = startIndex; i < messages.Count(); i++)
                {
                    displayMessages.Add(messages[i]);
                }

                if (displayMessages.Count > 0)
                {
                    state.MessageList = displayMessages;

                    if (displayMessages.Count == 1)
                    {
                        state.Message.Add(displayMessages[0]);
                    }

                    await ShowMailList(sc, displayMessages, totalCount, cancellationToken);
                    return await sc.NextAsync();
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailSharedResponses.EmailNotFound, ResponseBuilder));
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
                throw new Exception("No recipient!");
            }
            else if (recipients.Count == 1)
            {
                return recipients.FirstOrDefault()?.EmailAddress.Name;
            }

            string result = recipients.FirstOrDefault()?.EmailAddress.Name;
            for (int i = 1; i < recipients.Count; i++)
            {
                if (i == recipients.Count - 1)
                {
                    result += string.Format(CommonStrings.SeparatorFormat, CommonStrings.And) + recipients[i].EmailAddress.Name;
                }
                else
                {
                    result += ", " + recipients[i].EmailAddress.Name;
                }
            }

            return result;
        }

        protected async Task<PromptOptions> GenerateOptions(List<Person> personList, List<Person> userList, ITurnContext context)
        {
            var state = await EmailStateAccessor.GetAsync(context);
            var pageIndex = state.ShowRecipientIndex;
            var pageSize = ConfigData.GetInstance().MaxDisplaySize;
            var skip = pageSize * pageIndex;

            // Go back to the last page when reaching the end.
            if (skip >= personList.Count + userList.Count && pageIndex > 0)
            {
                state.ShowRecipientIndex--;
                state.ReadRecipientIndex = 0;
                pageIndex = state.ShowRecipientIndex;
                skip = pageSize * pageIndex;
            }

            var options = new PromptOptions
            {
                Choices = new List<Choice>(),
                Prompt = context.Activity.CreateReply(ConfirmRecipientResponses.ConfirmRecipient),
            };

            if (pageIndex > 0)
            {
                options.Prompt = context.Activity.CreateReply(ConfirmRecipientResponses.ConfirmRecipientNotFirstPage);
            }

            for (var i = 0; i < personList.Count; i++)
            {
                var user = personList[i];
                var mailAddress = user.ScoredEmailAddresses.FirstOrDefault()?.Address ?? user.UserPrincipalName;

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
                pageSize = ConfigData.GetInstance().MaxDisplaySize;
                options.Prompt = context.Activity.CreateReply(ConfirmRecipientResponses.ConfirmRecipientLastPage);
            }

            for (var i = 0; i < userList.Count; i++)
            {
                var user = userList[i];
                var mailAddress = user.ScoredEmailAddresses.FirstOrDefault()?.Address ?? user.UserPrincipalName;
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

            int pageSize = ConfigData.GetInstance().MaxDisplaySize;
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
                state.ShowEmailIndex--;
                skip = state.ShowEmailIndex * pageSize;
            }

            // get messages for current page
            var filteredResult = new List<Message>();
            for (int i = 0; i < result.Count; i++)
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
            var cardsData = new List<EmailCardData>();

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
                cardsData.Add(emailCard);
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

            var stringToken = new StringDictionary
            {
                { "TotalCount", totalCount.ToString() },
                { "EmailListDetails", SpeakHelper.ToSpeechEmailListString(updatedMessages, state.GetUserTimeZone(), ConfigData.GetInstance().MaxReadSize) },
            };

            var reply = sc.Context.Activity.CreateAdaptiveCardGroupReply(EmailSharedResponses.ShowEmailPrompt, "Dialogs/Shared/Resources/Cards/EmailCard.json", AttachmentLayoutTypes.Carousel, cardsData, ResponseBuilder, stringToken);
            if (updatedMessages.Count == 1)
            {
                reply = sc.Context.Activity.CreateAdaptiveCardGroupReply(EmailSharedResponses.ShowOneEmailPrompt, "Dialogs/Shared/Resources/Cards/EmailCard.json", AttachmentLayoutTypes.Carousel, cardsData, ResponseBuilder, stringToken);
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

        protected async Task DigestEmailLuisResult(DialogContext dc, Email luisResult, bool isBeginDialog)
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(dc.Context);

                var intent = luisResult.TopIntent().intent;

                var entity = luisResult.Entities;

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
                    case Email.Intent.CheckMessages:
                    case Email.Intent.SearchMessages:
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

                            if (entity.EmailAddress != null)
                            {
                                // As luis result for email address often contains extra spaces for word breaking
                                // (e.g. send email to test@test.com, email address entity will be test @ test . com)
                                // So use original user input as email address.
                                var rawEntity = luisResult.Entities._instance.EmailAddress;
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
                                state.IsUnreadOnly = false;
                            }

                            break;
                        }

                    case Email.Intent.SendEmail:
                    case Email.Intent.Forward:
                    case Email.Intent.Reply:
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

                            if (entity.EmailAddress != null)
                            {
                                // As luis result for email address often contains extra spaces for word breaking
                                // (e.g. send email to test@test.com, email address entity will be test @ test . com)
                                // So use original user input as email address.
                                var rawEntity = luisResult.Entities._instance.EmailAddress;
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
                                state.IsUnreadOnly = false;

                                // Clear focus email if there is any.
                                state.Message.Clear();
                            }

                            break;
                        }

                    default:
                        break;
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
            bool isReadMoreUserInput = userInput == null ? false : userInput.ToLowerInvariant().Contains(CommonStrings.More);
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
            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailSharedResponses.EmailErrorMessage));

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
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailSharedResponses.EmailErrorMessage_BotProblem));
            }
            else
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailSharedResponses.EmailErrorMessage));
            }

            // clear state
            await ClearAllState(sc);
        }
    }
}