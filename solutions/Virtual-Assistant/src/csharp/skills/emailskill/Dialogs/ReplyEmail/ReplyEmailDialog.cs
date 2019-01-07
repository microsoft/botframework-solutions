using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.Shared;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkill.Dialogs.Shared.Resources.Cards;
using EmailSkill.Dialogs.Shared.Resources.Strings;
using EmailSkill.ServiceClients;
using EmailSkill.Util;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;

namespace EmailSkill.Dialogs.ReplyEmail
{
    public class ReplyEmailDialog : EmailSkillDialog
    {
        public ReplyEmailDialog(
            SkillConfigurationBase services,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ReplyEmailDialog), services, emailStateAccessor, dialogStateAccessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var replyEmail = new WaterfallStep[]
            {
                IfClearContextStep,
                GetAuthToken,
                AfterGetAuthToken,
                CollectSelectedEmail,
                AfterCollectSelectedEmail,
                CollectAdditionalText,
                ConfirmBeforeSending,
                ReplyEmail,
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
            AddDialog(new WaterfallDialog(Actions.Reply, replyEmail));

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.Show, showEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateSelectMessage, updateSelectMessage) { TelemetryClient = telemetryClient });

            InitialDialogId = Actions.Reply;
        }

        public async Task<DialogTurnResult> ReplyEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var state = await EmailStateAccessor.GetAsync(sc.Context);
                    var token = state.Token;
                    var message = state.Message.FirstOrDefault();
                    var content = state.Content;

                    var service = ServiceManager.InitMailService(token, state.GetUserTimeZone(), state.MailSourceType);

                    // reply user message.
                    if (message != null)
                    {
                        await service.ReplyToMessageAsync(message.Id, content);
                    }

                    var nameListString = DisplayHelper.ToDisplayRecipientsString_Summay(message?.ToRecipients);

                    var emailCard = new EmailCardData
                    {
                        Subject = string.Format(EmailCommonStrings.ForwardReplyFormat, message?.Subject),
                        NameList = string.Format(EmailCommonStrings.ToFormat, nameListString),
                        EmailContent = string.Format(EmailCommonStrings.ContentFormat, state.Content),
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
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }

            await ClearConversationState(sc);
            return await sc.EndDialogAsync(true);
        }
    }
}