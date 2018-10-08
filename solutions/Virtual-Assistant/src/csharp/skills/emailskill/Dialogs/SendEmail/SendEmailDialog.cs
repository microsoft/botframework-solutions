using EmailSkill.Dialogs.SendEmail.Resources;
using EmailSkill.Dialogs.Shared.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EmailSkill
{
    public class SendEmailDialog : EmailSkillDialog
    {
        public SendEmailDialog(
            SkillConfiguration services,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IMailSkillServiceManager serviceManager)
            : base(nameof(SendEmailDialog), services, emailStateAccessor, dialogStateAccessor, serviceManager)
        {
            var sendEmail = new WaterfallStep[]
           {
                GetAuthToken,
                AfterGetAuthToken,
                CollectNameList,
                CollectRecipients,
                CollectSubject,
                CollectText,
                ConfirmBeforeSending,
                SendEmail,
           };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.Send, sendEmail));
            AddDialog(new ConfirmRecipientDialog(services, emailStateAccessor, dialogStateAccessor, serviceManager));
            InitialDialogId = Action.Send;
        }

        public async Task<DialogTurnResult> CollectSubject(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _emailStateAccessor.GetAsync(sc.Context);
                if (state.Subject != null)
                {
                    return await sc.NextAsync();
                }

                var recipientConfirmedMessage = sc.Context.Activity.CreateReply(EmailSharedResponses.RecipientConfirmed, null, new StringDictionary() { { "UserName", await GetNameListString(sc) } });
                var noSubjectMessage =
                    sc.Context.Activity.CreateReply(SendEmailResponses.NoSubject);
                noSubjectMessage.Text = recipientConfirmedMessage.Text + " " + noSubjectMessage.Text;
                noSubjectMessage.Speak += recipientConfirmedMessage.Speak + " " + noSubjectMessage.Speak;

                return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = noSubjectMessage, });
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        public async Task<DialogTurnResult> CollectText(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _emailStateAccessor.GetAsync(sc.Context);
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
                        var recipientConfirmedMessage = sc.Context.Activity.CreateReply(EmailSharedResponses.RecipientConfirmed, null, new StringDictionary() { { "UserName", await GetNameListString(sc) } });
                        noMessageBodyMessage.Text = recipientConfirmedMessage.Text + " " + noMessageBodyMessage.Text;
                        noMessageBodyMessage.Speak += recipientConfirmedMessage.Speak + " " + noMessageBodyMessage.Speak;
                    }

                    return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = noMessageBodyMessage });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        public async Task<DialogTurnResult> SendEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var state = await _emailStateAccessor.GetAsync(sc.Context);
                    var token = state.MsGraphToken;

                    var service = _serviceManager.InitMailService(token, state.GetUserTimeZone());

                    // send user message.
                    await service.SendMessage(state.Content, state.Subject, state.Recipients);

                    var nameListString = $"To: {state.Recipients.FirstOrDefault()?.EmailAddress.Name}";
                    if (state.Recipients.Count > 1)
                    {
                        nameListString += $" + {state.Recipients.Count - 1} more";
                    }

                    var emailCard = new EmailCardData
                    {
                        Subject = "Subject: " + state.Subject,
                        NameList = nameListString,
                        EmailContent = "Content: " + state.Content,
                    };
                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(EmailSharedResponses.SentSuccessfully, "Dialogs/Shared/Resources/Cards/EmailWithOutButtonCard.json", emailCard);

                    await sc.Context.SendActivityAsync(replyMessage);
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
