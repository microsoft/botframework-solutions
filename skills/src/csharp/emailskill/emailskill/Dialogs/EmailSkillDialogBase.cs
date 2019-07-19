﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Extensions;
using EmailSkill.Models;
using EmailSkill.Models.DialogModel;
using EmailSkill.Responses.Shared;
using EmailSkill.Services;
using EmailSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Graph;
using Microsoft.Recognizers.Text;
using Newtonsoft.Json.Linq;

namespace EmailSkill.Dialogs
{
    public class EmailSkillDialogBase : ComponentDialog
    {
        private ResourceMultiLanguageGenerator _lgMultiLangEngine;
        protected const string EmailStateKey = "EmailState";

        public EmailSkillDialogBase(
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
            EmailStateAccessor = conversationState.CreateProperty<EmailSkillState>(nameof(EmailSkillState));
            DialogStateAccessor = conversationState.CreateProperty<DialogState>(nameof(DialogState));
            ServiceManager = serviceManager;
            TelemetryClient = telemetryClient;

            // combine path for cross platform support
            _lgMultiLangEngine = new ResourceMultiLanguageGenerator("Shared.lg");

            AddDialog(new MultiProviderAuthDialog(settings.OAuthConnections, appCredentials));
            AddDialog(new TextPrompt(Actions.Prompt));
            AddDialog(new ConfirmPrompt(Actions.TakeFurtherAction, null, Culture.English) { Style = ListStyle.SuggestedAction });
        }

        protected EmailSkillDialogBase(string dialogId)
            : base(dialogId)
        {
        }

        protected BotServices Services { get; set; }

        protected BotSettings Settings { get; set; }

        protected IStatePropertyAccessor<EmailSkillState> EmailStateAccessor { get; set; }

        protected IStatePropertyAccessor<DialogState> DialogStateAccessor { get; set; }

        protected IServiceManager ServiceManager { get; set; }

        protected ResponseManager ResponseManager { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var skillOptions = ((JObject)options).ToObject<EmailSkillDialogOptions>();

            return await base.OnBeginDialogAsync(dc, skillOptions, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
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
        protected virtual async Task<DialogTurnResult> InitEmailSendDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                var userState = await EmailStateAccessor.GetAsync(sc.Context);
                var dialogState = new SendEmailDialogState();

                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = Services.CognitiveModelSets[locale];

                // Update state with email luis result and entities --- todo: use luis result in adaptive dialog
                var luisResult = await localeConfig.LuisServices["email"].RecognizeAsync<emailLuis>(sc.Context);
                userState.LuisResult = luisResult;
                localeConfig.LuisServices.TryGetValue("general", out var luisService);
                var generalLuisResult = await luisService.RecognizeAsync<General>(sc.Context);
                userState.GeneralLuisResult = generalLuisResult;

                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (skillOptions != null && skillOptions.SubFlowMode)
                {
                    dialogState = userState?.CacheModel != null ? new SendEmailDialogState(userState?.CacheModel) : dialogState;
                }

                var newState = DigestSendEmailLuisResult(sc, userState.LuisResult, userState.GeneralLuisResult, dialogState, true);
                sc.State.Dialog.Add(EmailStateKey, newState);

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected virtual async Task<DialogTurnResult> SaveEmailSendDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                var dialogState = new SendEmailDialogState();
                if (skillOptions != null && skillOptions.DialogState != null)
                {
                    if (skillOptions.DialogState is SendEmailDialogState)
                    {
                        dialogState = (SendEmailDialogState)skillOptions.DialogState;
                    }
                    else
                    {
                        dialogState = skillOptions.DialogState != null ? new SendEmailDialogState(skillOptions.DialogState) : dialogState;
                    }
                }

                var userState = await EmailStateAccessor.GetAsync(sc.Context);

                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = Services.CognitiveModelSets[locale];

                // Update state with email luis result and entities --- todo: use luis result in adaptive dialog
                var luisResult = await localeConfig.LuisServices["email"].RecognizeAsync<emailLuis>(sc.Context);
                userState.LuisResult = luisResult;
                localeConfig.LuisServices.TryGetValue("general", out var luisService);
                var generalLuisResult = await luisService.RecognizeAsync<General>(sc.Context);
                userState.GeneralLuisResult = generalLuisResult;

                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                var newState = DigestSendEmailLuisResult(sc, userState.LuisResult, userState.GeneralLuisResult, dialogState, true);
                sc.State.Dialog.Add(EmailStateKey, newState);

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
                var state = (EmailStateBase)sc.State.Dialog[EmailStateKey];

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
                var userState = await EmailStateAccessor.GetAsync(sc.Context);
                var state = (EmailStateBase)sc.State.Dialog[EmailStateKey];

                var luisResult = userState.LuisResult;
                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalLuisResult = userState.GeneralLuisResult;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (skillLuisResult == emailLuis.Intent.ShowNext || generalTopIntent == General.Intent.ShowNext)
                {
                    state.ShowEmailIndex++;
                }
                else if ((skillLuisResult == emailLuis.Intent.ShowPrevious || generalTopIntent == General.Intent.ShowPrevious) && state.ShowEmailIndex >= 0)
                {
                    state.ShowEmailIndex--;
                }

                if (state.MessageList != null && state.UserSelectIndex >= 0 && state.UserSelectIndex < state.MessageList.Count())
                {
                    state.Message.Clear();
                    state.Message.Add(state.MessageList[state.UserSelectIndex]);
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // Shared steps
        protected async Task<DialogTurnResult> GetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[NoAuth]", null);
                return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions() { RetryPrompt = activity as Activity });
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
                if (sc.Result is ProviderTokenResponse providerTokenResponse)
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
                var state = (EmailStateBase)sc.State.Dialog[EmailStateKey];
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.DialogState = state;
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
                var state = (EmailStateBase)sc.State.Dialog[EmailStateKey];
                if (state.MessageList.Count == 0)
                {
                    return await sc.EndDialogAsync(true);
                }

                var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[NoFocusMessage]", null);
                return await sc.PromptAsync(
                    Actions.Prompt,
                    new PromptOptions() { Prompt = activity as Activity });
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

                var state = (EmailStateBase)sc.State.Dialog[EmailStateKey];
                skillOptions.DialogState = state;

                var userState = await EmailStateAccessor.GetAsync(sc.Context);
                var luisResult = userState.LuisResult;

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
                var state = (EmailStateBase)sc.State.Dialog[EmailStateKey];

                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;
                skillOptions.DialogState = state;

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
                var state = (EmailStateBase)sc.State.Dialog[EmailStateKey];

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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                string nameListString;

                var action = Actions.Send;
                if (state.FindContactInfor.Contacts.FirstOrDefault() == null)
                {
                    // this means reply confirm
                    action = Actions.Reply;
                    await GetPreviewSubject(sc, action);
                }
                else if (state.Subject == null)
                {
                    // this mean forward confirm
                    action = Actions.Forward;
                    await GetPreviewSubject(sc, action);
                }
                else
                {
                    action = Actions.Send;
                }

                nameListString = DisplayHelper.ToDisplayRecipientsString_Summay(state.FindContactInfor.Contacts);

                var emailCard = new EmailCardData
                {
                    Subject = state.Subject.Equals(EmailCommonStrings.EmptySubject) ? null : state.Subject,
                    EmailContent = state.Content.Equals(EmailCommonStrings.EmptyContent) ? null : state.Content,
                    RecipientsCount = state.FindContactInfor.Contacts.Count()
                };
                emailCard = await ProcessRecipientPhotoUrl(sc.Context, emailCard, state.FindContactInfor.Contacts);

                var speech = SpeakHelper.ToSpeechEmailSendDetailString(state.Subject, nameListString, state.Content);

                if (state.FindContactInfor.Contacts.Count > DisplayHelper.MaxReadoutNumber && (action == Actions.Send || action == Actions.Forward))
                {
                    var prompt = await LGHelper.GenerateAdaptiveCardAsync(
                        _lgMultiLangEngine,
                        sc.Context,
                        "[ConfirmSendWithRecipients]",
                        new { emailDetails = state.Subject },
                        "[EmailWithOutButtonCard(emailDetails)]",
                        new { emailDetails = emailCard });

                    var retry = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ConfirmSendRecipientsFailed]", null);

                    return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = prompt as Activity, RetryPrompt = retry as Activity });
                }
                else
                {
                    var prompt = await LGHelper.GenerateAdaptiveCardAsync(
                        _lgMultiLangEngine,
                        sc.Context,
                        "[ConfirmSendWithoutRecipients]",
                        new { emailDetails = state.Subject },
                        "[EmailWithOutButtonCard(emailDetails)]",
                        new { emailDetails = emailCard });

                    var retry = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ConfirmSendFailed]", null);

                    return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = prompt as Activity, RetryPrompt = retry as Activity});
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ConfirmAllRecipient(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];

                if (state.FindContactInfor.Contacts.Count > DisplayHelper.MaxReadoutNumber)
                {
                    var nameListString = DisplayHelper.ToDisplayRecipientsString(state.FindContactInfor.Contacts);
                    var confirmRecipients = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ConfirmSendAfterConfirmRecipients]", new { recipientsList = nameListString });
                    var retry = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ConfirmSendFailed]", null);

                    return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = confirmRecipients as Activity, RetryPrompt = retry as Activity});
                }
                else
                {
                    return await sc.NextAsync(sc.Result);
                }
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
                if (sc.Result != null && sc.Result is FindContactDialogOptions)
                {
                    var result = (FindContactDialogOptions)sc.Result;
                    sc.State.Dialog[EmailStateKey] = result.DialogState;
                }

                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];

                if (string.IsNullOrEmpty(state.Content))
                {
                    var noEmailContentMessage = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[NoEmailContent]", null);
                    if (sc.ActiveDialog.Id == nameof(ForwardEmailDialog))
                    {
                        if (state.FindContactInfor.Contacts.Count == 0 || state.FindContactInfor.Contacts == null)
                        {
                            state.FindContactInfor.FirstRetryInFindContact = true;
                            return await sc.EndDialogAsync();
                        }

                        noEmailContentMessage = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[NoEmailContentWithRecipientConfirmed]", new { userName = await GetNameListStringAsync(sc) });
                    }

                    return await sc.PromptAsync(
                        Actions.Prompt,
                        new PromptOptions { Prompt = noEmailContentMessage as Activity});
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                skillOptions.DialogState = state;
                if (!state.FindContactInfor.Contacts.Any())
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                if (state.FindContactInfor.ContactsNameList.Any())
                {
                    return await sc.NextAsync();
                }
                else
                {
                    var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[NoRecipients]", null);
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = activity as Activity });
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];

                // ensure state.nameList is not null or empty
                //if (!state.FindContactInfor.ContactsNameList.Any())
                //{
                //    var userInput = sc.Result.ToString();
                //    if (string.IsNullOrWhiteSpace(userInput))
                //    {
                //        skillOptions.DialogState = state;
                //        return await sc.BeginDialogAsync(Actions.CollectRecipient, skillOptions);
                //    }

                //    var nameList = userInput.Split(EmailCommonPhrase.GetContactNameSeparator(), options: StringSplitOptions.None)
                //        .Select(x => x.Trim())
                //        .Where(x => !string.IsNullOrWhiteSpace(x))
                //        .ToList();
                //    state.FindContactInfor.ContactsNameList = nameList;
                //}

                skillOptions.DialogState = state;
                return await sc.BeginDialogAsync(nameof(FindContactDialog), options: new FindContactDialogOptions(sc.Options), cancellationToken: cancellationToken);
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
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
                var state = (EmailStateBase)sc.State.Dialog[EmailStateKey];
                var userState = await EmailStateAccessor.GetAsync(sc.Context);

                var (messages, totalCount, importantCount) = await GetMessagesAsync(sc);

                // Get display messages
                var displayMessages = new List<Message>();
                var startIndex = 0;
                for (var i = startIndex; i < messages.Count(); i++)
                {
                    displayMessages.Add(messages[i]);
                }

                if (displayMessages.Count > 0)
                {
                    state.MessageList = displayMessages;
                    state.Message.Clear();
                    state.Message.Add(displayMessages[0]);

                    await ShowMailList(sc, displayMessages, totalCount, importantCount, cancellationToken);
                    return await sc.NextAsync();
                }
                else
                {
                    state.MessageList.Clear();
                    state.Message.Clear();

                    var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[EmailNotFound]", null);
                    await sc.Context.SendActivityAsync(activity);
                }

                return await sc.EndDialogAsync(false);
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
                var state = (EmailStateBase)sc.State.Dialog[EmailStateKey];
                var userState = await EmailStateAccessor.GetAsync(sc.Context);
                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                var messages = state.MessageList;

                if (((userState.LuisResult.Entities.ordinal != null) && (userState.LuisResult.Entities.ordinal.Count() > 0))
                    || ((userState.GeneralLuisResult?.Entities?.number != null) && (userState.GeneralLuisResult.Entities.number.Count() > 0)))
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

                    var searchType = EmailSearchType.None;
                    (messages, searchType) = FilterMessages(messages, searchSender, searchSubject, searchUserInput);

                    if (searchType == EmailSearchType.SearchByContact)
                    {
                        state.SearchTexts = null;
                        if (state.SenderName == null)
                        {
                            state.SenderName = userInput;
                        }
                    }
                    else if (searchType == EmailSearchType.SearchBySubject)
                    {
                        state.SenderName = null;
                        if (state.SearchTexts == null)
                        {
                            state.SearchTexts = userInput;
                        }
                    }

                    state.MessageList = messages;
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
            var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
            var recipients = state.FindContactInfor.Contacts;

            if (recipients == null || recipients.Count == 0)
            {
                throw new NoRecipientsException();
            }
            else if (recipients.Count == 1)
            {
                return recipients.FirstOrDefault()?.EmailAddress.Name + ": " + recipients.FirstOrDefault()?.EmailAddress.Address;
            }

            var result = recipients.FirstOrDefault()?.EmailAddress.Name + ": " + recipients.FirstOrDefault()?.EmailAddress.Address;
            for (var i = 1; i < recipients.Count; i++)
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

        protected async Task<bool> GetPreviewSubject(WaterfallStepContext sc, string actionType)
        {
            try
            {
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];

                var focusedMessage = state.Message.FirstOrDefault();

                switch (actionType)
                {
                    case Actions.Reply:
                        state.Subject = focusedMessage.Subject.ToLower().StartsWith(EmailCommonStrings.Reply) ? focusedMessage.Subject : string.Format(EmailCommonStrings.ReplyReplyFormat, focusedMessage?.Subject);
                        state.FindContactInfor.Contacts = focusedMessage.ToRecipients.ToList();
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

        protected (List<Message>, EmailSearchType) FilterMessages(List<Message> messages, string searchSender, string searchSubject, string searchUserInput)
        {
            if ((searchSender == null) && (searchSubject == null) && (searchUserInput == null))
            {
                return (messages, EmailSearchType.None);
            }

            var searchType = EmailSearchType.None;

            // Get display messages
            var displayMessages = new List<Message>();
            for (var i = 0; i < messages.Count(); i++)
            {
                var messageSender = messages[i].Sender?.EmailAddress?.Name?.ToLowerInvariant();
                var messageSubject = messages[i].Subject?.ToLowerInvariant();

                if (messageSender != null)
                {
                    if ((searchType == EmailSearchType.None) || (searchType == EmailSearchType.SearchByContact))
                    {
                        if (((searchSender != null) && messageSender.Contains(searchSender))
                        || ((searchUserInput != null) && messageSender.Contains(searchUserInput)))
                        {
                            displayMessages.Add(messages[i]);

                            searchType = EmailSearchType.SearchByContact;
                            if (searchSender == null)
                            {
                                searchSender = searchUserInput;
                            }

                            searchSubject = null;
                            continue;
                        }
                    }
                }

                if (messageSubject != null)
                {
                    if ((searchType == EmailSearchType.None) || (searchType == EmailSearchType.SearchBySubject))
                    {
                        if (((searchSubject != null) && messageSubject.Contains(searchSubject))
                        || ((searchUserInput != null) && messageSubject.Contains(searchUserInput)))
                        {
                            displayMessages.Add(messages[i]);

                            searchType = EmailSearchType.SearchBySubject;
                            if (searchSubject == null)
                            {
                                searchSubject = searchUserInput;
                            }

                            searchSender = null;
                            continue;
                        }
                    }
                }
            }

            return (displayMessages, searchType);
        }

        protected async Task<(List<Message>, int, int)> GetMessagesAsync(WaterfallStepContext sc)
        {
            var result = new List<Message>();

            var pageSize = ConfigData.GetInstance().MaxDisplaySize;
            var state = (EmailStateBase)sc.State.Dialog[EmailStateKey]; //await EmailStateAccessor.GetAsync(sc.Context);
            var userState = await EmailStateAccessor.GetAsync(sc.Context);
            var token = userState.Token;
            var serivce = ServiceManager.InitMailService(token, userState.GetUserTimeZone(), userState.MailSourceType);

            var isUnreadOnly = state.IsUnreadOnly;
            var isImportant = state.IsImportant;
            var startDateTime = state.StartDateTime;
            var endDateTime = state.EndDateTime;
            var directlyToMe = state.DirectlyToMe;
            var skip = state.ShowEmailIndex * pageSize;
            string mailAddress = null;

            // Get user message.
            result = await serivce.GetMyMessagesAsync(startDateTime, endDateTime, isUnreadOnly, isImportant, directlyToMe, mailAddress);

            // Go back to last page if next page didn't get anything
            if (skip >= result.Count)
            {
                skip = (state.ShowEmailIndex - 1) * pageSize;
            }

            // get messages for current page
            var filteredResult = new List<Message>();
            var importantEmailCount = 0;
            for (var i = 0; i < result.Count; i++)
            {
                if (result[i].Importance.HasValue && result[i].Importance.Value == Importance.High)
                {
                    importantEmailCount++;
                }

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
                }
            }

            return (filteredResult, result.Count, importantEmailCount);
        }

        protected async Task ShowMailList(WaterfallStepContext sc, List<Message> messages, int totalCount, int importantCount, CancellationToken cancellationToken = default(CancellationToken))
        {
            var updatedMessages = new List<Message>();
            var state = (EmailStateBase)sc.State.Dialog[EmailStateKey];
            var userState = await EmailStateAccessor.GetAsync(sc.Context);

            var cards = new List<Card>();
            var emailList = new List<EmailCardData>();
            foreach (var message in messages)
            {
                var nameListString = DisplayHelper.ToDisplayRecipientsString_Summay(message.ToRecipients);

                var senderIcon = await GetUserPhotoUrlAsync(sc.Context, message.Sender.EmailAddress);
                var emailCard = new EmailCardData
                {
                    Subject = message.Subject,
                    Sender = message.Sender.EmailAddress.Name,
                    NameList = string.Format(EmailCommonStrings.ToFormat, nameListString),
                    EmailContent = message.BodyPreview,
                    EmailLink = message.WebLink,
                    ReceivedDateTime = message.ReceivedDateTime == null
                    ? CommonStrings.NotAvailable
                    : message.ReceivedDateTime.Value.UtcDateTime.ToOverallRelativeString(userState.GetUserTimeZone()),
                    Speak = SpeakHelper.ToSpeechEmailDetailOverallString(message, userState.GetUserTimeZone()),
                    SenderIcon = senderIcon
                };

                var isImportant = message.Importance != null && message.Importance == Importance.High;
                var hasAttachment = message.HasAttachments.HasValue && message.HasAttachments.Value;
                if (isImportant && hasAttachment)
                {
                    emailCard.AdditionalIcon1 = AdaptiveCardHelper.ImportantIcon;
                    emailCard.AdditionalIcon2 = AdaptiveCardHelper.AttachmentIcon;
                }
                else if (isImportant)
                {
                    emailCard.AdditionalIcon1 = AdaptiveCardHelper.ImportantIcon;
                }
                else if (hasAttachment)
                {
                    emailCard.AdditionalIcon1 = AdaptiveCardHelper.AttachmentIcon;
                }

                cards.Add(new Card("EmailOverviewItem", emailCard));
                emailList.Add(emailCard);
                updatedMessages.Add(message);
            }

            var tokens = new StringDictionary
            {
                { "TotalCount", totalCount.ToString() },
                { "EmailListDetails", SpeakHelper.ToSpeechEmailListString(updatedMessages, userState.GetUserTimeZone(), ConfigData.GetInstance().MaxReadSize) }
            };

            var avator = await GetMyPhotoUrlAsync(sc.Context);

            var maxPage = (totalCount / ConfigData.GetInstance().MaxDisplaySize) + (totalCount % ConfigData.GetInstance().MaxDisplaySize > 0 ? 1 : 0) - 1;
            var validShowEmailIndex = (state.ShowEmailIndex < 0) ? 0 : state.ShowEmailIndex;
            validShowEmailIndex = (validShowEmailIndex > maxPage) ? maxPage : validShowEmailIndex;
            var startIndex = (validShowEmailIndex * ConfigData.GetInstance().MaxDisplaySize) + 1;
            var endIndex = (startIndex + ConfigData.GetInstance().MaxDisplaySize - 1) > totalCount ? totalCount : (startIndex + ConfigData.GetInstance().MaxDisplaySize - 1);
            var overviewData = new EmailOverviewData()
            {
                Description = EmailCommonStrings.YourEmail,
                AvatorIcon = avator,
                TotalMessageNumber = totalCount.ToString(),
                HighPriorityMessagesNumber = importantCount.ToString(),
                Now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userState.GetUserTimeZone()).ToString(EmailCommonStrings.GeneralDateFormat),
                MailSourceType = string.Format(EmailCommonStrings.Source, AdaptiveCardHelper.GetSourceType(userState.MailSourceType)),
                EmailIndexer = string.Format(
                    EmailCommonStrings.PageIndexerFormat,
                    startIndex.ToString(),
                    endIndex.ToString(),
                    totalCount.ToString()),
                EmailList = emailList
            };

            var overviewCard = GetDivergedCardName(sc.Context, "EmailOverviewCard");
            if (state.SenderName != null)
            {
                overviewData.Description = string.Format(EmailCommonStrings.SearchBySender, state.SenderName);
                overviewCard = GetDivergedCardName(sc.Context, "EmailOverviewByCondition");
            }
            else if ((state.SearchTexts != null) /*|| (userState.GeneralSearchTexts != null)*/)
            {
                overviewData.Description = string.Format(EmailCommonStrings.SearchBySubject, state.SearchTexts);
                overviewCard = GetDivergedCardName(sc.Context, "EmailOverviewByCondition");
            }

            //var reply = ResponseManager.GetCardResponse(
            //            EmailSharedResponses.ShowEmailPrompt,
            //            new Card(overviewCard, overviewData),
            //            tokens,
            //            "items",
            //            cards);

            var reply = await LGHelper.GenerateAdaptiveCardAsync(
                        _lgMultiLangEngine,
                        sc.Context,
                        "[ShowEmailPromptWithFirstLastPrefix]",
                        new
                        {
                            totalCount = totalCount.ToString(),
                            emailListDetails = SpeakHelper.ToSpeechEmailListString(updatedMessages, userState.GetUserTimeZone(), ConfigData.GetInstance().MaxReadSize),
                            showEmailIndex = state.ShowEmailIndex,
                            showEmailCount = updatedMessages.Count,
                            maxEmailPage = maxPage
                        },
                        "[EmailOverviewCard(emailOverview)]",
                        new { emailOverview = overviewData });

            //if (state.ShowEmailIndex == 0)
            //{
            //    if (updatedMessages.Count == 1)
            //    {
            //        reply = ResponseManager.GetCardResponse(
            //            EmailSharedResponses.ShowOneEmailPrompt,
            //            new Card(overviewCard, overviewData),
            //            tokens,
            //            "items",
            //            cards);
            //    }
            //}
            //else
            //{
            //    reply = ResponseManager.GetCardResponse(
            //            EmailSharedResponses.ShowEmailPromptOtherPage,
            //            new Card(overviewCard, overviewData),
            //            tokens,
            //            "items",
            //            cards);
            //    if (updatedMessages.Count == 1)
            //    {
            //        reply = ResponseManager.GetCardResponse(
            //            EmailSharedResponses.ShowOneEmailPromptOtherPage,
            //            new Card(overviewCard, overviewData),
            //            tokens,
            //            "items",
            //            cards);
            //    }
            //}

            if (state.ShowEmailIndex < 0)
            {
                //var pagingInfo = ResponseManager.GetResponse(EmailSharedResponses.FirstPageAlready);
                //reply.Text = pagingInfo.Text + reply.Text;
                //reply.Speak = pagingInfo.Speak + reply.Speak;
                state.ShowEmailIndex = 0;
            }
            else if (state.ShowEmailIndex > maxPage)
            {
                //var pagingInfo = ResponseManager.GetResponse(EmailSharedResponses.LastPageAlready);
                //reply.Text = pagingInfo.Text + reply.Text;
                //reply.Speak = pagingInfo.Speak + reply.Speak;
                state.ShowEmailIndex--;
            }

            await sc.Context.SendActivityAsync(reply);
            return;
        }

        protected async Task<string> GetMyPhotoUrlAsync(ITurnContext context)
        {
            var state = await EmailStateAccessor.GetAsync(context);
            var token = state.Token;
            var service = ServiceManager.InitUserService(token, state.GetUserTimeZone(), state.MailSourceType);

            try
            {
                var user = await service.GetMeAsync();
                if (user != null && !string.IsNullOrEmpty(user.Photo))
                {
                    return user.Photo;
                }

                // return default value
                return string.Format(AdaptiveCardHelper.DefaultAvatarIconPathFormat, AdaptiveCardHelper.DefaultMe);
            }
            catch (Exception)
            {
                // won't clear conversation state hear, because sometime use api is not available, like user msa account.
                return string.Format(AdaptiveCardHelper.DefaultAvatarIconPathFormat, AdaptiveCardHelper.DefaultMe);
            }
        }

        protected async Task<string> GetUserPhotoUrlAsync(ITurnContext context, EmailAddress email)
        {
            var state = await EmailStateAccessor.GetAsync(context);
            var token = state.Token;
            var service = ServiceManager.InitUserService(token, state.GetUserTimeZone(), state.MailSourceType);
            var displayName = email.Name ?? email.Address;

            try
            {
                var url = await service.GetPhotoAsync(email.Address);
                if (!string.IsNullOrEmpty(url))
                {
                    return url;
                }

                // return default value
                return string.Format(AdaptiveCardHelper.DefaultAvatarIconPathFormat, displayName);
            }
            catch (Exception)
            {
                return string.Format(AdaptiveCardHelper.DefaultAvatarIconPathFormat, displayName);
            }
        }

        protected async Task<EmailCardData> ProcessRecipientPhotoUrl(ITurnContext context, EmailCardData data, IEnumerable<Recipient> recipients)
        {
            try
            {
                if (recipients == null || recipients.Count() == 0)
                {
                    throw new Exception("No recipient!");
                }

                var size = Math.Min(AdaptiveCardHelper.MaxDisplayRecipientNum, recipients.Count());

                for (var i = 0; i < size; i++)
                {
                    var photoUrl = await GetUserPhotoUrlAsync(context, recipients.ElementAt(i).EmailAddress);

                    switch (i)
                    {
                        case 0:
                            data.RecipientIcon0 = photoUrl;
                            break;
                        case 1:
                            data.RecipientIcon1 = photoUrl;
                            break;
                        case 2:
                            data.RecipientIcon2 = photoUrl;
                            break;
                        case 3:
                            data.RecipientIcon3 = photoUrl;
                            break;
                        case 4:
                            data.RecipientIcon4 = photoUrl;
                            break;
                    }
                }

                if (recipients.Count() > AdaptiveCardHelper.MaxDisplayRecipientNum)
                {
                    // the last recipient turns into number
                    var additionalNumber = recipients.Count() - AdaptiveCardHelper.MaxDisplayRecipientNum + 1;
                    data.AdditionalRecipientNumber = additionalNumber.ToString();
                }

                // return default value
                return data;
            }
            catch (ServiceException)
            {
                return null;
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
            var state = (EmailStateBase)sc.State.Dialog[EmailStateKey]; //await EmailStateAccessor.GetAsync(sc.Context);

            // Get focus message if any
            if (state.MessageList != null && state.UserSelectIndex >= 0 && state.UserSelectIndex < state.MessageList.Count())
            {
                state.Message.Clear();
                state.Message.Add(state.MessageList[state.UserSelectIndex]);
            }
        }

        protected EmailStateBase DigestSendEmailLuisResult(DialogContext dc, emailLuis luisResult, General generalLuisResult, SendEmailDialogState state, bool isBeginDialog)
        {
            try
            {
                var intent = luisResult.TopIntent().intent;
                var entity = luisResult.Entities;
                var generalEntity = generalLuisResult.Entities;

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

                    if (generalEntity != null && generalEntity.number != null && (entity.ordinal == null || entity.ordinal.Length == 0))
                    {
                        try
                        {
                            var emailList = state.MessageList;
                            var value = generalEntity.number[0];
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
                        return state;
                    }

                    switch (intent)
                    {
                        case emailLuis.Intent.SendEmail:
                        case emailLuis.Intent.Forward:
                        case emailLuis.Intent.Reply:
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
                                        if (!state.FindContactInfor.ContactsNameList.Contains(name))
                                        {
                                            state.FindContactInfor.ContactsNameList.Add(name);
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
                                        if (Utilities.Util.IsEmail(email) && !state.FindContactInfor.ContactsNameList.Contains(email))
                                        {
                                            state.FindContactInfor.ContactsNameList.Add(email);
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

                return state;
            }
            catch
            {
                return state;
            }
        }

        protected EmailStateBase DigestLuisResult(DialogContext dc, emailLuis luisResult, General generalLuisResult, EmailStateBase state, bool isBeginDialog)
        {
            try
            {
                var intent = luisResult.TopIntent().intent;
                var entity = luisResult.Entities;
                var generalEntity = generalLuisResult.Entities;

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

                    if (generalEntity != null && generalEntity.number != null && (entity.ordinal == null || entity.ordinal.Length == 0))
                    {
                        try
                        {
                            var emailList = state.MessageList;
                            var value = generalEntity.number[0];
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
                        return state;
                    }

                    switch (intent)
                    {
                        case emailLuis.Intent.CheckMessages:
                        case emailLuis.Intent.SearchMessages:
                        case emailLuis.Intent.ReadAloud:
                        case emailLuis.Intent.ShowNext:
                        case emailLuis.Intent.ShowPrevious:
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

                                if (entity.SenderName != null)
                                {
                                    state.SenderName = entity.SenderName[0];
                                }

                                if (entity.SearchTexts != null)
                                {
                                    state.SearchTexts = entity.SearchTexts[0];
                                }
                                else if (entity.EmailSubject != null)
                                {
                                    state.SearchTexts = entity.EmailSubject[0];
                                }

                                break;
                            }

                        default:
                            break;
                    }
                }

                return state;
            }
            catch
            {
                return state;
            }
        }

        protected bool IsReadMoreIntent(General.Intent? topIntent, string userInput)
        {
            var isReadMoreUserInput = userInput == null ? false : userInput.ToLowerInvariant().Contains(CommonStrings.More);
            return topIntent == General.Intent.ShowNext && isReadMoreUserInput;
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
            var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[EmailErrorMessage]", null);
            await sc.Context.SendActivityAsync(activity);

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
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            // send error message to bot user
            IMessageActivity activity = new Activity();
            if (ex.ExceptionType == SkillExceptionType.APIAccessDenied)
            {
                activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[EmailErrorMessageBotProblem]", null);
            }
            else if (ex.ExceptionType == SkillExceptionType.AccountNotActivated)
            {
                activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[EmailErrorMessageAccountProblem]", null);
            }
            else
            {
                activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[EmailErrorMessage]", null);
            }

            await sc.Context.SendActivityAsync(activity);

            // clear state
            await ClearAllState(sc);
        }

        // Workaround until adaptive card renderer in teams is upgraded to v1.2
        protected string GetDivergedCardName(ITurnContext turnContext, string card)
        {
            if (Microsoft.Bot.Builder.Dialogs.Choices.Channel.GetChannelId(turnContext) == Channels.Msteams)
            {
                return card + ".1.0";
            }
            else
            {
                return card;
            }
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