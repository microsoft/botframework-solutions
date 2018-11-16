using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.Shared.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Skills;

namespace EmailSkill
{
    public class ForwardEmailDialog : EmailSkillDialog
    {
        public ForwardEmailDialog(
            ISkillConfiguration services,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IMailSkillServiceManager serviceManager)
            : base(nameof(ForwardEmailDialog), services, emailStateAccessor, dialogStateAccessor, serviceManager)
        {
            var forwardEmail = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                CollectNameList,
                CollectRecipients,
                CollectSelectedEmail,
                CollectAdditionalText,
                ConfirmBeforeSending,
                ForwardEmail,
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
            AddDialog(new WaterfallDialog(Actions.Forward, forwardEmail));
            AddDialog(new WaterfallDialog(Actions.Show, showEmail));
            AddDialog(new WaterfallDialog(Actions.UpdateSelectMessage, updateSelectMessage));
            AddDialog(new ConfirmRecipientDialog(services, emailStateAccessor, dialogStateAccessor, serviceManager));

            InitialDialogId = Actions.Forward;
        }

        public async Task<DialogTurnResult> ForwardEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var state = await EmailStateAccessor.GetAsync(sc.Context);

                    var token = state.MsGraphToken;
                    var message = state.Message;
                    var id = message.FirstOrDefault()?.Id;
                    var content = state.Content;
                    var recipients = state.Recipients;

                    var service = ServiceManager.InitMailService(token, state.GetUserTimeZone());

                    // send user message.
                    await service.ForwardMessageAsync(id, content, recipients);

                    var nameListString = CommonStrings.To + $"{state.Recipients.FirstOrDefault()?.EmailAddress.Name}";
                    if (state.Recipients.Count > 1)
                    {
                        nameListString += $" + {state.Recipients.Count - 1} more";
                    }

                    var emailCard = new EmailCardData
                    {
                        Subject = CommonStrings.Subject + "FW: " + message.FirstOrDefault()?.Subject,
                        NameList = nameListString,
                        EmailContent = CommonStrings.Content + state.Content,
                    };
                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(EmailSharedResponses.SentSuccessfully, "Dialogs/Shared/Resources/Cards/EmailWithOutButtonCard.json", emailCard);

                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailSharedResponses.CancellingMessage));
                }
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }

            await ClearConversationState(sc);
            return await sc.EndDialogAsync(true);
        }
    }
}
