using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Models;
using EmailSkill.Models.DialogModel;
using EmailSkill.Responses.Shared;
using EmailSkill.Services;
using EmailSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;

namespace EmailSkill.Dialogs
{
    public class ForwardEmailDialog : EmailSkillDialogBase
    {
        private ResourceMultiLanguageGenerator _lgMultiLangEngine;

        public ForwardEmailDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            FindContactDialog findContactDialog,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(ForwardEmailDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            _lgMultiLangEngine = new ResourceMultiLanguageGenerator("ForwardEmail.lg");

            var forwardEmail = new WaterfallStep[]
            {
                InitEmailSendDialogState,
                GetAuthToken,
                AfterGetAuthToken,
                SetDisplayConfig,
                CollectSelectedEmail,
                AfterCollectSelectedEmail,
                CollectRecipient,
                CollectAdditionalText,
                AfterCollectAdditionalText,
                ConfirmBeforeSending,
                ConfirmAllRecipient,
                ForwardEmail,
            };

            var showEmail = new WaterfallStep[]
            {
                SaveEmailSendDialogState,
                PagingStep,
                ShowEmails,
            };

            var collectRecipients = new WaterfallStep[]
            {
                SaveEmailSendDialogState,
                //PromptRecipientCollection,
                GetRecipients,
            };

            var updateSelectMessage = new WaterfallStep[]
            {
                SaveEmailSendDialogState,
                UpdateMessage,
                PromptUpdateMessage,
                AfterUpdateMessage,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new EmailWaterfallDialog(Actions.Forward, forwardEmail, EmailStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new EmailWaterfallDialog(Actions.Show, showEmail, EmailStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new EmailWaterfallDialog(Actions.CollectRecipient, collectRecipients, EmailStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new EmailWaterfallDialog(Actions.UpdateSelectMessage, updateSelectMessage, EmailStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new FindContactDialog(settings, services, responseManager, conversationState, serviceManager, telemetryClient));
            InitialDialogId = Actions.Forward;
        }

        public async Task<DialogTurnResult> ForwardEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                    var userState = await EmailStateAccessor.GetAsync(sc.Context);

                    var token = userState.Token;
                    var message = state.Message;
                    var id = message.FirstOrDefault()?.Id;
                    var recipients = state.FindContactInfor.Contacts;

                    var service = ServiceManager.InitMailService(token, userState.GetUserTimeZone(), userState.MailSourceType);

                    // send user message.
                    var content = state.Content.Equals(EmailCommonStrings.EmptyContent) ? string.Empty : state.Content;
                    await service.ForwardMessageAsync(id, content, recipients);

                    var emailCard = new EmailCardData
                    {
                        Subject = state.Subject.Equals(EmailCommonStrings.EmptySubject) ? null : state.Subject,
                        EmailContent = state.Content.Equals(EmailCommonStrings.EmptyContent) ? null : state.Content,
                        RecipientsCount = state.FindContactInfor.Contacts.Count()
                    };
                    emailCard = await ProcessRecipientPhotoUrl(sc.Context, emailCard, state.FindContactInfor.Contacts);

                    var reply = await LGHelper.GenerateAdaptiveCardAsync(
                        _lgMultiLangEngine,
                        sc.Context,
                        "[SentSuccessfully]",
                        new { subject = state.Subject },
                        "[EmailWithOutButtonCard(emailDetails)]",
                        new { emailDetails = emailCard });

                    await sc.Context.SendActivityAsync(reply);
                }
                else
                {
                    var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[CancellingMessage]", null);
                    await sc.Context.SendActivityAsync(activity);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }

            //await ClearConversationState(sc);
            return await sc.EndDialogAsync(true);
        }
    }
}