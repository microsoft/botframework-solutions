using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.ConfirmRecipient;
using EmailSkill.Dialogs.SendEmail.Resources;
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

namespace EmailSkill.Dialogs.SendEmail
{
    public class SendEmailDialog : EmailSkillDialog
    {
        public SendEmailDialog(
            SkillConfigurationBase services,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(SendEmailDialog), services, emailStateAccessor, dialogStateAccessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var sendEmail = new WaterfallStep[]
            {
                IfClearContextStep,
                GetAuthToken,
                AfterGetAuthToken,
                CollectRecipient,
                CollectSubject,
                CollectText,
                ConfirmBeforeSending,
                SendEmail,
            };

            var collectRecipients = new WaterfallStep[]
            {
                PromptRecipientCollection,
                GetRecipients,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.Send, sendEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectRecipient, collectRecipients) { TelemetryClient = telemetryClient });
            AddDialog(new ConfirmRecipientDialog(services, emailStateAccessor, dialogStateAccessor, serviceManager, telemetryClient));
            InitialDialogId = Actions.Send;
        }

        public async Task<DialogTurnResult> CollectSubject(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                if (state.Subject != null)
                {
                    return await sc.NextAsync();
                }

                var recipientConfirmedMessage = sc.Context.Activity.CreateReply(EmailSharedResponses.RecipientConfirmed, null, new StringDictionary() { { "UserName", await GetNameListStringAsync(sc) } });
                var noSubjectMessage = sc.Context.Activity.CreateReply(SendEmailResponses.NoSubject);
                noSubjectMessage.Text = recipientConfirmedMessage.Text + " " + noSubjectMessage.Text;
                noSubjectMessage.Speak = recipientConfirmedMessage.Speak + " " + noSubjectMessage.Speak;

                return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = noSubjectMessage, });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> CollectText(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var subject);
                    state.Subject = subject != null ? subject.ToString() : sc.Context.Activity.Text;
                }

                if (string.IsNullOrEmpty(state.Content))
                {
                    var noMessageBodyMessage = sc.Context.Activity.CreateReply(SendEmailResponses.NoMessageBody);
                    if (sc.Result == null)
                    {
                        var recipientConfirmedMessage = sc.Context.Activity.CreateReply(EmailSharedResponses.RecipientConfirmed, null, new StringDictionary() { { "UserName", await GetNameListStringAsync(sc) } });
                        noMessageBodyMessage.Text = recipientConfirmedMessage.Text + " " + noMessageBodyMessage.Text;
                        noMessageBodyMessage.Speak = recipientConfirmedMessage.Speak + " " + noMessageBodyMessage.Speak;
                    }

                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = noMessageBodyMessage });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> SendEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var state = await EmailStateAccessor.GetAsync(sc.Context);
                    var token = state.Token;

                    var service = ServiceManager.InitMailService(token, state.GetUserTimeZone(), state.MailSourceType);

                    // send user message.
                    await service.SendMessageAsync(state.Content, state.Subject, state.Recipients);

                    var nameListString = DisplayHelper.ToDisplayRecipientsString_Summay(state.Recipients);

                    var emailCard = new EmailCardData
                    {
                        Subject = string.Format(EmailCommonStrings.SubjectFormat, state.Subject),
                        NameList = string.Format(EmailCommonStrings.ToFormat, nameListString),
                        EmailContent = string.Format(EmailCommonStrings.ContentFormat, state.Content),
                    };
                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(EmailSharedResponses.SentSuccessfully, "Dialogs/Shared/Resources/Cards/EmailWithOutButtonCard.json", emailCard);

                    await sc.Context.SendActivityAsync(replyMessage);
                }
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
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