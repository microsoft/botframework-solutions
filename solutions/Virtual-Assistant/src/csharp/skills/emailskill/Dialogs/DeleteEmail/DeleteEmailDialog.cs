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
                ShowEmails,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.Delete, deleteEmail));
            AddDialog(new WaterfallDialog(Actions.Show, showEmail));
            InitialDialogId = Actions.Delete;
        }

        public async Task<DialogTurnResult> PromptToDelete(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var focusedMessage = state.Message.FirstOrDefault();
                if (focusedMessage != null)
                {
                    return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(DeleteEmailResponses.DeleteConfirm) });
                }

                return await sc.BeginDialogAsync(Actions.Show);
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
                    var state = await EmailStateAccessor.GetAsync(sc.Context);
                    var mailService = this.ServiceManager.InitMailService(state.MsGraphToken, state.GetUserTimeZone());
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
