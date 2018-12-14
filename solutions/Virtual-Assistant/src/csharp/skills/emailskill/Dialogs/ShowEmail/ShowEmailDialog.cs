using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkill.Dialogs.ShowEmail.Resources;
using EmailSkill.Extensions;
using EmailSkill.Util;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;

namespace EmailSkill
{
    public class ShowEmailDialog : EmailSkillDialog
    {
        public ShowEmailDialog(
            ISkillConfiguration services,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ShowEmailDialog), services, emailStateAccessor, dialogStateAccessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var showEmail = new WaterfallStep[]
            {
                IfClearContextStep,
                GetAuthToken,
                AfterGetAuthToken,
                ShowEmailsWithoutEnd,
                PromptToHandle,
                CallHandleEmailDialog,
            };

            var readEmail = new WaterfallStep[]
            {
                ReadEmail,
                HandleMore,
            };

            var deleteEmail = new WaterfallStep[]
            {
                DeleteEmail,
                PromptToReshow
            };

            var forwardEmail = new WaterfallStep[]
            {
                ForwardEmail,
                PromptToReshow
            };

            var replyEmail = new WaterfallStep[]
            {
                ReplyEmail,
                PromptToReshow
            };

            var reshowEmail = new WaterfallStep[]
            {
                PagingStep,
                ShowEmailsWithoutEnd,
                PromptToHandleMore,
                HandleMore,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.Show, showEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Read, readEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Delete, deleteEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Forward, forwardEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Reply, replyEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Reshow, reshowEmail) { TelemetryClient = telemetryClient });
            AddDialog(new DeleteEmailDialog(services, emailStateAccessor, dialogStateAccessor, serviceManager, telemetryClient));
            AddDialog(new ReplyEmailDialog(services, emailStateAccessor, dialogStateAccessor, serviceManager, telemetryClient));
            AddDialog(new ForwardEmailDialog(services, emailStateAccessor, dialogStateAccessor, serviceManager, telemetryClient));
            InitialDialogId = Actions.Show;
        }

        protected async Task<DialogTurnResult> ShowEmailsWithoutEnd(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                var messages = await GetMessagesAsync(sc);
                if (messages.Count > 0)
                {
                    messages = await ShowMailList(sc, messages);

                    // Give focus when there is only one email.
                    if (messages.Count == 1)
                    {
                        state.Message.Add(messages[0]);
                    }

                    state.MessageList = messages;
                    return await sc.NextAsync();
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailSharedResponses.EmailNotFound, ResponseBuilder));
                    return await sc.EndDialogAsync(true);
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
        }

        protected async Task<DialogTurnResult> PromptToHandle(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                if (state.MessageList.Count == 1)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(ShowEmailResponses.ReadOutOnlyOnePrompt) });
                }
                else
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(ShowEmailResponses.ReadOutPrompt) });
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> PromptToHandleMore(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(ShowEmailResponses.ReadOutMorePrompt) });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CallHandleEmailDialog(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var luisResult = state.LuisResult;

                var topIntent = luisResult?.TopIntent().intent;
                if (topIntent == null)
                {
                    return await sc.EndDialogAsync(true);
                }

                return await sc.BeginDialogAsync(Actions.Read, skillOptions);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ReadEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var skillOptions = (EmailSkillDialogOptions)sc.Options;

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

                await DigestFocusEmailAsync(sc);
                var message = state.Message.FirstOrDefault();

                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                {
                    await sc.Context.SendActivityAsync(
                        sc.Context.Activity.CreateReply(EmailSharedResponses.CancellingMessage));
                    return await sc.EndDialogAsync(true);
                }
                else if ((promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                     || (topIntent == Email.Intent.ReadAloud && !IsReadMoreIntent(generalTopIntent, sc.Context.Activity.Text)))
                {
                    if (message == null && state.MessageList.Count > 1)
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(ShowEmailResponses.ReadOutPrompt), });
                    }
                }

                if ((topIntent == Email.Intent.SelectItem
                    || (topIntent == Email.Intent.ReadAloud && !IsReadMoreIntent(generalTopIntent, sc.Context.Activity.Text))
                    || (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true))
                    && message != null)
                {
                    var nameListString = DisplayHelper.ToDisplayRecipientsString_Summay(message.ToRecipients);

                    var emailCard = new EmailCardData
                    {
                        Subject = message.Subject,
                        Sender = message.Sender.EmailAddress.Name,
                        NameList = string.Format(CommonStrings.ToFormat, nameListString),
                        EmailContent = message.BodyPreview,
                        EmailLink = message.WebLink,
                        ReceivedDateTime = message?.ReceivedDateTime == null
                            ? CommonStrings.NotAvailable
                            : message.ReceivedDateTime.Value.UtcDateTime.ToRelativeString(state.GetUserTimeZone()),
                        Speak = SpeakHelper.ToSpeechEmailDetailString(message),
                    };

                    // Todo: workaround here to read out email details. Ignore body for now as we need a summary and filter.
                    var emailDetails = SpeakHelper.ToSpeechEmailDetailString(message);
                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(ShowEmailResponses.ReadOutMessage, "Dialogs/Shared/Resources/Cards/EmailDetailCard.json", emailCard, null, new StringDictionary() { { "EmailDetails", emailDetails } });
                    await sc.Context.SendActivityAsync(replyMessage);

                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(ShowEmailResponses.ReadOutMorePrompt) });
                }
                else
                {
                    return await sc.NextAsync();
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
        }

        protected async Task<DialogTurnResult> HandleMore(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var luisResult = state.LuisResult;

                var topIntent = luisResult?.TopIntent().intent;
                var topGeneralIntent = state.GeneralLuisResult?.TopIntent().intent;
                if (topIntent == null)
                {
                    return await sc.EndDialogAsync(true);
                }

                await DigestFocusEmailAsync(sc);

                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;

                if (IsReadMoreIntent(topGeneralIntent, sc.Context.Activity.Text))
                {
                    return await sc.BeginDialogAsync(Actions.Reshow, skillOptions);
                }
                else if (topIntent == Email.Intent.Delete)
                {
                    return await sc.BeginDialogAsync(Actions.Delete, skillOptions);
                }
                else if (topIntent == Email.Intent.Forward)
                {
                    return await sc.BeginDialogAsync(Actions.Forward, skillOptions);
                }
                else if (topIntent == Email.Intent.Reply)
                {
                    return await sc.BeginDialogAsync(Actions.Reply, skillOptions);
                }
                else if (topIntent == Email.Intent.ReadAloud || topIntent == Email.Intent.SelectItem)
                {
                    var message = state.Message.FirstOrDefault();

                    if (message == null)
                    {
                        return await sc.BeginDialogAsync(Actions.Reshow, skillOptions);
                    }
                    else
                    {
                        return await sc.BeginDialogAsync(Actions.Read, skillOptions);
                    }
                }
                else if (topIntent == Email.Intent.None && (topGeneralIntent == General.Intent.Previous || topGeneralIntent == General.Intent.Next))
                {
                    return await sc.BeginDialogAsync(Actions.Reshow, skillOptions);
                }
                else
                {
                    // return a signal for main flow need to start a new ComponentDialog.
                    return await sc.EndDialogAsync(true);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> DeleteEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;

                return await sc.BeginDialogAsync(nameof(DeleteEmailDialog), skillOptions);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ForwardEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;

                return await sc.BeginDialogAsync(nameof(ForwardEmailDialog), skillOptions);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ReplyEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;

                return await sc.BeginDialogAsync(nameof(ReplyEmailDialog), skillOptions);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> PromptToReshow(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                return await sc.BeginDialogAsync(Actions.Reshow, skillOptions);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}