using EmailSkill.Dialogs.Shared.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.DeleteEmail.Resources;

namespace EmailSkill
{
    public class DeleteEmailDialog : EmailSkillDialog
    {
        public DeleteEmailDialog(
            ISkillConfiguration services,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IMailSkillServiceManager serviceManager)
            : base(nameof(DeleteEmailDialog), services, emailStateAccessor, dialogStateAccessor, serviceManager)
        {
            var deleteEmail = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                PromptToDelete,
                DeleteEmail,
            };

            var showEmail = new WaterfallStep[]
            {
                PromptCollectMessage,
                ShowEmails
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.Delete, deleteEmail));
            AddDialog(new WaterfallDialog(Action.Show, showEmail));
            InitialDialogId = Action.Delete;
        }

        public async Task<DialogTurnResult> PromptToDelete(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _emailStateAccessor.GetAsync(sc.Context);
                var focusedMessage = state.Message.FirstOrDefault();
                if (focusedMessage != null)
                {
                    return await sc.PromptAsync(Action.ConfirmRecipient, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(DeleteEmailResponses.DeleteConfirm) });
                }

                return await sc.BeginDialogAsync(Action.Show);
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        public async Task<DialogTurnResult> PromptCollectMessage(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var state = await _emailStateAccessor.GetAsync(sc.Context);
                var focusedMessage = state.Message.FirstOrDefault();
                if (focusedMessage != null)
                {
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(DeleteEmailResponses.DeletePrompt) });
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        public async Task<DialogTurnResult> DeleteEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var confirmResult = (bool)sc.Result;
                if (confirmResult == true)
                {
                    var state = await _emailStateAccessor.GetAsync(sc.Context);
                    var mailService = this._serviceManager.InitMailService(state.MsGraphToken, state.GetUserTimeZone());
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
                throw await HandleDialogExceptions(sc, ex);
            }
        }
    }
}
