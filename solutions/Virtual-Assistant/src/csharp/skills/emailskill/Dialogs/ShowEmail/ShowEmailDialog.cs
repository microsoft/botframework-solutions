using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkill.Dialogs.ShowEmail.Resources;
using EmailSkill.Extensions;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Skills;

namespace EmailSkill
{
    public class ShowEmailDialog : EmailSkillDialog
    {
        public ShowEmailDialog(
            ISkillConfiguration services,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IMailSkillServiceManager serviceManager)
            : base(nameof(ShowEmailDialog), services, emailStateAccessor, dialogStateAccessor, serviceManager)
        {
            var showEmail = new WaterfallStep[]
            {
                IfClearContextStep,
                GetAuthToken,
                AfterGetAuthToken,
                ShowEmailsWithoutEnd,
                PromptToRead,
                CallReadOrDeleteDialog,
            };

            var readEmail = new WaterfallStep[]
            {
                ReadEmail,
                AfterReadOutEmail,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.Show, showEmail));
            AddDialog(new WaterfallDialog(Actions.Read, readEmail));
            AddDialog(new DeleteEmailDialog(services, emailStateAccessor, dialogStateAccessor, serviceManager));
            InitialDialogId = Actions.Show;
        }

        public async Task<DialogTurnResult> IfClearContextStep(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // clear context before show emails, and extract it from luis result again.
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var luisResult = state.LuisResult;

                var topIntent = luisResult?.TopIntent().intent;
                if (topIntent == Email.Intent.CheckMessages)
                {
                    await ClearConversationState(sc);
                    await DigestEmailLuisResult(sc, luisResult);
                }

                var generalLuisResult = state.GeneralLuisResult;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;
                if (generalTopIntent == General.Intent.Next)
                {
                    state.ShowEmailIndex++;
                }

                if (generalTopIntent == General.Intent.Previous && state.ShowEmailIndex > 0)
                {
                    state.ShowEmailIndex--;
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        public async Task<DialogTurnResult> ShowEmailsWithoutEnd(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                var messages = await GetMessagesAsync(sc);
                if (messages.Count > 0)
                {
                    await ShowMailList(sc, messages);
                    state.MessageList = messages;
                    return await sc.NextAsync();
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailSharedResponses.EmailNotFound, ResponseBuilder));
                    return await sc.EndDialogAsync(true);
                }
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        public async Task<DialogTurnResult> PromptToRead(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(ShowEmailResponses.ReadOutPrompt) });
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        public async Task<DialogTurnResult> CallReadOrDeleteDialog(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var luisResult = state.LuisResult;

                var topIntent = luisResult?.TopIntent().intent;
                if (topIntent == null)
                {
                    return await sc.EndDialogAsync(true);
                }

                return await sc.BeginDialogAsync(Actions.Read);
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        public async Task<DialogTurnResult> ReadEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                var luisResult = state.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;
                var generalLuisResult = state.GeneralLuisResult;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (topIntent == null)
                {
                    return await sc.EndDialogAsync(true);
                }

                var message = state.Message.FirstOrDefault();

                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                {
                    await sc.Context.SendActivityAsync(
                        sc.Context.Activity.CreateReply(EmailSharedResponses.CancellingMessage));
                    return await sc.EndDialogAsync(true);
                }
                else if (topIntent == Email.Intent.ReadAloud && message == null)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(ShowEmailResponses.ReadOutPrompt), });
                }
                else if (topIntent == Email.Intent.SelectItem || (topIntent == Email.Intent.ReadAloud && message != null))
                {
                    var nameListString = CommonStrings.To + $"{message?.ToRecipients.FirstOrDefault()?.EmailAddress.Name}";
                    if (message?.ToRecipients.Count() > 1)
                    {
                        nameListString += $" + {message.ToRecipients.Count() - 1} " + CommonStrings.More;
                    }

                    var emailCard = new EmailCardData
                    {
                        Subject = message?.Subject,
                        Sender = message?.Sender.EmailAddress.Name,
                        NameList = nameListString,
                        EmailContent = message?.BodyPreview,
                        EmailLink = message?.WebLink,
                        ReceivedDateTime = message?.ReceivedDateTime == null
                            ? CommonStrings.NotAvailable
                            : message.ReceivedDateTime.Value.UtcDateTime.ToRelativeString(state.GetUserTimeZone()),
                        Speak = message?.Subject + " " + CommonStrings.From + " " + message?.Sender.EmailAddress.Name + " " + message?.BodyPreview,
                    };

                    // Todo: workaround here to read out email details. Ignore body for now as we need a summary and filter.
                    var emailDetails = message?.Subject + " " + CommonStrings.From + " " + message?.Sender.EmailAddress.Name;
                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(ShowEmailResponses.ReadOutMessage, "Dialogs/Shared/Resources/Cards/EmailDetailCard.json", emailCard, null, new StringDictionary() { { "EmailDetails", emailDetails } });
                    await sc.Context.SendActivityAsync(replyMessage);

                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(ShowEmailResponses.ReadOutMorePrompt) });
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

        public async Task<DialogTurnResult> AfterReadOutEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var luisResult = state.LuisResult;

                var topIntent = luisResult?.TopIntent().intent;
                if (topIntent == null)
                {
                    return await sc.EndDialogAsync(true);
                }

                if (topIntent == Email.Intent.Delete)
                {
                    return await sc.BeginDialogAsync(nameof(DeleteEmailDialog));
                }

                if (topIntent == Email.Intent.ReadAloud || topIntent == Email.Intent.SelectItem)
                {
                    return await sc.BeginDialogAsync(Actions.Read);
                }
                else
                {
                    // return a signal for main flow need to start a new ComponentDialog.
                    return await sc.EndDialogAsync(true);
                }
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }
    }
}
