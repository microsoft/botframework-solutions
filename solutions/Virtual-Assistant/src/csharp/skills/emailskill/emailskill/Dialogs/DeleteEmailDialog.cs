using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Models;
using EmailSkill.Responses.DeleteEmail;
using EmailSkill.Responses.Shared;
using EmailSkill.ServiceClients;
using EmailSkill.Services;
using EmailSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;

namespace EmailSkill.Dialogs
{
    public class DeleteEmailDialog : EmailSkillDialogBase
    {
        public DeleteEmailDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(DeleteEmailDialog), settings, services, responseManager, emailStateAccessor, dialogStateAccessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

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

                var focusedMessage = state.Message?.FirstOrDefault();
                if (focusedMessage != null)
                {
                    var nameListString = DisplayHelper.ToDisplayRecipientsString_Summay(focusedMessage.ToRecipients);
                    var emailCard = new EmailCardData
                    {
                        Subject = string.Format(EmailCommonStrings.SubjectFormat, focusedMessage.Subject),
                        NameList = string.Format(EmailCommonStrings.ToFormat, nameListString),
                        EmailContent = string.Format(EmailCommonStrings.ContentFormat, focusedMessage.BodyPreview),
                    };

                    var speech = SpeakHelper.ToSpeechEmailSendDetailString(focusedMessage.Subject, nameListString, focusedMessage.BodyPreview);
                    var tokens = new StringDictionary
                    {
                        { "EmailDetails", speech },
                    };

                    var prompt = ResponseManager.GetCardResponse(
                        DeleteEmailResponses.DeleteConfirm,
                        new Card("EmailWithOutButtonCard", emailCard),
                        tokens);

                    var retry = ResponseManager.GetResponse(EmailSharedResponses.ConfirmSendFailed);

                    return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = prompt, RetryPrompt = retry });
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
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(DeleteEmailResponses.DeleteSuccessfully));
                }
                else
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(EmailSharedResponses.CancellingMessage));
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