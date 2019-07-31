using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Extensions;
using EmailSkill.Models;
using EmailSkill.Responses.DeleteEmail;
using EmailSkill.Responses.Shared;
using EmailSkill.Services;
using EmailSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace EmailSkill.Dialogs
{
    public class DeleteEmailDialog : EmailSkillDialogBase
    {
        private ResourceMultiLanguageGenerator _lgMultiLangEngine;

        public DeleteEmailDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(DeleteEmailDialog), settings, services, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            _lgMultiLangEngine = new ResourceMultiLanguageGenerator("DeleteEmail.lg");

            var deleteEmail = new WaterfallStep[]
            {
                IfClearContextStep,
                GetAuthToken,
                AfterGetAuthToken,
                SetDisplayConfig,
                CollectSelectedEmail,
                AfterCollectSelectedEmail,
                PromptToDelete,
                DeleteEmail,
            };

            var showEmail = new WaterfallStep[]
            {
                PagingStep,
                ShowEmails,
            };

            var updateSelectMessage = new WaterfallStep[]
            {
                UpdateMessage,
                PromptUpdateMessage,
                AfterUpdateMessage,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.Delete, deleteEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Show, showEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateSelectMessage, updateSelectMessage) { TelemetryClient = telemetryClient });
            InitialDialogId = Actions.Delete;
        }

        public async Task<DialogTurnResult> PromptToDelete(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var skillOptions = (EmailSkillDialogOptions)sc.Options;

                var message = state.Message?.FirstOrDefault();
                if (message != null)
                {
                    var nameListString = DisplayHelper.ToDisplayRecipientsString_Summay(message.ToRecipients);
                    var senderIcon = await GetUserPhotoUrlAsync(sc.Context, message.Sender.EmailAddress);
                    var emailCard = new EmailCardData
                    {
                        Subject = message.Subject,
                        EmailContent = message.BodyPreview,
                        Sender = message.Sender.EmailAddress.Name,
                        EmailLink = message.WebLink,
                        ReceivedDateTime = message?.ReceivedDateTime == null
                            ? CommonStrings.NotAvailable
                            : message.ReceivedDateTime.Value.UtcDateTime.ToDetailRelativeString(state.GetUserTimeZone()),
                        Speak = SpeakHelper.ToSpeechEmailDetailOverallString(message, state.GetUserTimeZone()),
                        SenderIcon = senderIcon,
                        RecipientsCount = message.ToRecipients.Count()
                    };
                    emailCard = await ProcessRecipientPhotoUrl(sc.Context, emailCard, message.ToRecipients);

                    var speech = SpeakHelper.ToSpeechEmailSendDetailString(message.Subject, nameListString, message.BodyPreview);
                    var prompt = await LGHelper.GenerateAdaptiveCardAsync(
                       _lgMultiLangEngine,
                       sc.Context,
                       "[DeleteConfirm]",
                       new { emailInfo = speech },
                       "[EmailDetailCard(emailDetails)]",
                       new { emailDetails = emailCard });

                    var retry = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ConfirmSendFailed]", null);
                    return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = prompt as Activity, RetryPrompt = retry as Activity });
                }

                skillOptions.SubFlowMode = true;
                return await sc.BeginDialogAsync(Actions.UpdateSelectMessage, skillOptions);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> DeleteEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var confirmResult = (bool)sc.Result;
                if (confirmResult == true)
                {
                    var state = await EmailStateAccessor.GetAsync(sc.Context);
                    var mailService = this.ServiceManager.InitMailService(state.Token, state.GetUserTimeZone(), state.MailSourceType);
                    var focusMessage = state.Message.FirstOrDefault();
                    await mailService.DeleteMessageAsync(focusMessage.Id);

                    var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[DeleteSuccessfully]", null);
                    await sc.Context.SendActivityAsync(activity);
                }
                else
                {
                    var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[CancellingMessage]", null);
                    await sc.Context.SendActivityAsync(activity);
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}