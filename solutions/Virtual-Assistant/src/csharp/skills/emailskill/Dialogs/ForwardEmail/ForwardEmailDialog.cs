using EmailSkill.Dialogs.Shared.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EmailSkill
{
    public class ForwardEmailDialog : EmailSkillDialog
    {
        public ForwardEmailDialog(
            SkillConfiguration services,
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
            AddDialog(new WaterfallDialog(Action.Forward, forwardEmail));
            AddDialog(new WaterfallDialog(Action.Show, showEmail));
            AddDialog(new WaterfallDialog(Action.UpdateSelectMessage, updateSelectMessage));
            AddDialog(new ConfirmRecipientDialog(services, emailStateAccessor, dialogStateAccessor, serviceManager));

            InitialDialogId = Action.Forward;
        }

        public async Task<DialogTurnResult> ForwardEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var state = await _emailStateAccessor.GetAsync(sc.Context);

                    var token = state.MsGraphToken;
                    var message = state.Message;
                    var id = message.FirstOrDefault()?.Id;
                    var content = state.Content;
                    var recipients = state.Recipients;

                    var service = _serviceManager.InitMailService(token, state.GetUserTimeZone());

                    // send user message.
                    await service.ForwardMessage(id, content, recipients);

                    var nameListString = $"To: {state.Recipients.FirstOrDefault()?.EmailAddress.Name}";
                    if (state.Recipients.Count > 1)
                    {
                        nameListString += $" + {state.Recipients.Count - 1} more";
                    }

                    var emailCard = new EmailCardData
                    {
                        Subject = "Subject: FW: " + message.FirstOrDefault()?.Subject,
                        NameList = nameListString,
                        EmailContent = "Content: " + state.Content,
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
