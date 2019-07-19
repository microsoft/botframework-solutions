using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Models;
using EmailSkill.Models.DialogModel;
using EmailSkill.Responses.Shared;
using EmailSkill.Services;
using EmailSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;

namespace EmailSkill.Dialogs
{
    public class ReplyEmailDialog : EmailSkillDialogBase
    {
        private ResourceMultiLanguageGenerator _lgMultiLangEngine;

        public ReplyEmailDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(ReplyEmailDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            _lgMultiLangEngine = new ResourceMultiLanguageGenerator("ReplyEmail.lg");

            var replyEmail = new WaterfallStep[]
            {
                InitEmailSendDialogState,
                GetAuthToken,
                AfterGetAuthToken,
                SetDisplayConfig,
                CollectSelectedEmail,
                AfterCollectSelectedEmail,
                CollectAdditionalText,
                AfterCollectAdditionalText,
                ConfirmBeforeSending,
                ReplyEmail,
            };

            var showEmail = new WaterfallStep[]
            {
                SaveEmailSendDialogState,
                PagingStep,
                ShowEmails,
            };

            var updateSelectMessage = new WaterfallStep[]
            {
                SaveEmailSendDialogState,
                UpdateMessage,
                PromptUpdateMessage,
                AfterUpdateMessage,
            };
            AddDialog(new EmailWaterfallDialog(Actions.Reply, replyEmail, EmailStateAccessor));

            // Define the conversation flow using a waterfall model.
            AddDialog(new EmailWaterfallDialog(Actions.Show, showEmail, EmailStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new EmailWaterfallDialog(Actions.UpdateSelectMessage, updateSelectMessage, EmailStateAccessor) { TelemetryClient = telemetryClient });

            InitialDialogId = Actions.Reply;
        }

        public async Task<DialogTurnResult> ReplyEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                    var userState = await EmailStateAccessor.GetAsync(sc.Context);
                    var token = userState.Token;
                    var message = state.Message.FirstOrDefault();

                    var service = ServiceManager.InitMailService(token, userState.GetUserTimeZone(), userState.MailSourceType);

                    // reply user message.
                    if (message != null)
                    {
                        var content = state.Content.Equals(EmailCommonStrings.EmptyContent) ? string.Empty : state.Content;
                        await service.ReplyToMessageAsync(message.Id, content);
                    }

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

            return await sc.EndDialogAsync(true);
        }
    }
}