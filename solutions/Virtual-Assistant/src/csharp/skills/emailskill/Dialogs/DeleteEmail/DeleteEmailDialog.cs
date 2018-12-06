using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.DeleteEmail.Resources;
using EmailSkill.Dialogs.Shared.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;

namespace EmailSkill
{
    public class DeleteEmailDialog : EmailSkillDialog
    {
        public DeleteEmailDialog(
            ISkillConfiguration services,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IServiceManager serviceManager)
            : base(nameof(DeleteEmailDialog), services, emailStateAccessor, dialogStateAccessor, serviceManager)
        {
            var deleteEmail = new WaterfallStep[]
            {
                IfClearContextStep,
                GetAuthToken,
                AfterGetAuthToken,
                CollectSelectedEmail,
                PromptToDelete,
                DeleteEmail,
            };

            var showEmail = new WaterfallStep[]
            {
                ShowEmails,
            };

            var updateSelectMessage = new WaterfallStep[]
            {
                UpdateMessage,
                PromptUpdateMessage,
                AfterUpdateMessage,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.Delete, deleteEmail));
            AddDialog(new WaterfallDialog(Actions.Show, showEmail));
            AddDialog(new WaterfallDialog(Actions.UpdateSelectMessage, updateSelectMessage));
            InitialDialogId = Actions.Delete;
        }

        public async Task<DialogTurnResult> PromptToDelete(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var skillOptions = (EmailSkillDialogOptions)sc.Options;

                var focusedMessage = state.Message.FirstOrDefault();
                if (focusedMessage != null)
                {
                    return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(DeleteEmailResponses.DeleteConfirm) });
                }

                return await sc.BeginDialogAsync(Actions.Show, skillOptions);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> PromptCollectMessage(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var focusedMessage = state.Message.FirstOrDefault();
                if (focusedMessage != null)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(DeleteEmailResponses.DeletePrompt) });
                }

                return await sc.NextAsync();
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
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailSharedResponses.CancellingMessage));
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
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
    }
}