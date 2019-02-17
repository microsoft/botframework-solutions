using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.DeleteEmail;
using EmailSkill.Dialogs.ForwardEmail;
using EmailSkill.Dialogs.ReplyEmail;
using EmailSkill.Dialogs.Shared;
using EmailSkill.Dialogs.Shared.DialogOptions;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkill.Dialogs.Shared.Resources.Cards;
using EmailSkill.Dialogs.Shared.Resources.Strings;
using EmailSkill.Dialogs.ShowEmail.Resources;
using EmailSkill.Extensions;
using EmailSkill.ServiceClients;
using EmailSkill.Util;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;

namespace EmailSkill.Dialogs.ShowEmail
{
    public class ShowEmailDialog : EmailSkillDialog
    {
        public ShowEmailDialog(
            SkillConfigurationBase services,
            ResponseManager responseManager,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ShowEmailDialog), services, responseManager, emailStateAccessor, dialogStateAccessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var showEmail = new WaterfallStep[]
            {
                IfClearContextStep,
                GetAuthToken,
                AfterGetAuthToken,
                Display
            };

            var readEmail = new WaterfallStep[]
            {
                ReadEmail,
                Reshow
            };

            var deleteEmail = new WaterfallStep[]
            {
                DeleteEmail,
                Reshow
            };

            var forwardEmail = new WaterfallStep[]
            {
                ForwardEmail,
                Reshow
            };

            var replyEmail = new WaterfallStep[]
            {
                ReplyEmail,
                Reshow
            };

            var displayEmail = new WaterfallStep[]
            {
                PagingStep,
                ShowEmails,
                PromptToHandle,
                CheckRead,
                HandleMore
            };

            var displayFilteredEmail = new WaterfallStep[]
            {
                ShowFilteredEmails,
                PromptToHandle,
                CheckRead,
                HandleMore
            };

            var redisplayEmail = new WaterfallStep[]
            {
                PromptToReshow,
                CheckReshow,
                HandleMore,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.Show, showEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Read, readEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Delete, deleteEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Forward, forwardEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Reply, replyEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Display, displayEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.DisplayFiltered, displayFilteredEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ReDisplay, redisplayEmail) { TelemetryClient = telemetryClient });
            AddDialog(new DeleteEmailDialog(services, responseManager, emailStateAccessor, dialogStateAccessor, serviceManager, telemetryClient));
            AddDialog(new ReplyEmailDialog(services, responseManager, emailStateAccessor, dialogStateAccessor, serviceManager, telemetryClient));
            AddDialog(new ForwardEmailDialog(services, responseManager, emailStateAccessor, dialogStateAccessor, serviceManager, telemetryClient));
            InitialDialogId = Actions.Show;
        }

        protected async Task<DialogTurnResult> PromptToHandle(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                if (state.MessageList.Count == 1)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(ShowEmailResponses.ReadOutOnlyOnePrompt) });
                }
                else
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(ShowEmailResponses.ReadOutPrompt) });
                }
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
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(ShowEmailResponses.ReadOutMorePrompt) });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CheckRead(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;

                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var luisResult = state.LuisResult;

                var topIntent = luisResult?.TopIntent().intent;
                if (topIntent == null)
                {
                    return await sc.EndDialogAsync(true);
                }

                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(EmailSharedResponses.CancellingMessage));
                    return await sc.EndDialogAsync(false);
                }
                else if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    return await sc.ReplaceDialogAsync(Actions.Read, skillOptions);
                }

                return await sc.NextAsync();
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
                if (message == null)
                {
                    state.Message.Add(state.MessageList[0]);
                    message = state.Message.FirstOrDefault();
                }

                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);

                if ((topIntent == EmailLU.Intent.None
                    || topIntent == EmailLU.Intent.SearchMessages
                    || (topIntent == EmailLU.Intent.ReadAloud && !IsReadMoreIntent(generalTopIntent, sc.Context.Activity.Text))
                    || (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true))
                    && message != null)
                {
                    var nameListString = DisplayHelper.ToDisplayRecipientsString_Summay(message.ToRecipients);

                    var emailCard = new EmailCardData
                    {
                        Subject = message.Subject,
                        Sender = message.Sender.EmailAddress.Name,
                        NameList = string.Format(EmailCommonStrings.ToFormat, nameListString),
                        EmailContent = message.BodyPreview,
                        EmailLink = message.WebLink,
                        ReceivedDateTime = message?.ReceivedDateTime == null
                            ? CommonStrings.NotAvailable
                            : message.ReceivedDateTime.Value.UtcDateTime.ToRelativeString(state.GetUserTimeZone()),
                        Speak = SpeakHelper.ToSpeechEmailDetailOverallString(message, state.GetUserTimeZone()),
                    };

                    var tokens = new StringDictionary()
                    {
                        { "EmailDetails", SpeakHelper.ToSpeechEmailDetailString(message, state.GetUserTimeZone()) },
                        { "EmailDetailsWithContent", SpeakHelper.ToSpeechEmailDetailString(message, state.GetUserTimeZone(), true) },
                    };

                    var replyMessage = ResponseManager.GetCardResponse(
                        ShowEmailResponses.ReadOutMessage,
                        new Card("EmailDetailCard", emailCard),
                        tokens);

                    // Set email as read.
                    var service = ServiceManager.InitMailService(state.Token, state.GetUserTimeZone(), state.MailSourceType);
                    await service.MarkMessageAsReadAsync(message.Id);

                    await sc.Context.SendActivityAsync(replyMessage);
                }

                return await sc.NextAsync();
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

        protected async Task<DialogTurnResult> CheckReshow(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;

                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(EmailSharedResponses.CancellingMessage));
                    return await sc.EndDialogAsync(true);
                }
                else if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    return await sc.ReplaceDialogAsync(Actions.Display, skillOptions);
                }

                return await sc.NextAsync();
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

                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                await DigestFocusEmailAsync(sc);

                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;

                if (topIntent == EmailLU.Intent.Delete)
                {
                    return await sc.BeginDialogAsync(Actions.Delete, skillOptions);
                }
                else if (topIntent == EmailLU.Intent.Forward)
                {
                    return await sc.BeginDialogAsync(Actions.Forward, skillOptions);
                }
                else if (topIntent == EmailLU.Intent.Reply)
                {
                    return await sc.BeginDialogAsync(Actions.Reply, skillOptions);
                }
                else if (IsReadMoreIntent(topGeneralIntent, userInput)
                    || (topIntent == EmailLU.Intent.None && (topGeneralIntent == General.Intent.Previous || topGeneralIntent == General.Intent.Next)))
                {
                    return await sc.ReplaceDialogAsync(Actions.Display, skillOptions);
                }
                else
                {
                    await DigestEmailLuisResult(sc, state.LuisResult, true);
                    await SearchEmailsFromList(sc, cancellationToken);

                    if (state.MessageList.Count > 0)
                    {
                        return await sc.ReplaceDialogAsync(Actions.DisplayFiltered, skillOptions);
                    }

                    // return a signal for main flow need to start a new ComponentDialog.
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(EmailSharedResponses.DidntUnderstandMessage));
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

        protected async Task<DialogTurnResult> Display(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                return await sc.ReplaceDialogAsync(Actions.Display, skillOptions);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> Reshow(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                return await sc.ReplaceDialogAsync(Actions.ReDisplay, skillOptions);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ShowFilteredEmails(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                if (state.MessageList.Count > 0)
                {
                    if (state.Message.Count == 0)
                    {
                        state.Message.Add(state.MessageList[0]);

                        if (state.MessageList.Count > 1)
                        {
                            await ShowMailList(sc, state.MessageList, state.MessageList.Count(), cancellationToken);
                            return await sc.NextAsync();
                        }
                        else if (state.MessageList.Count == 1)
                        {
                            return await sc.ReplaceDialogAsync(Actions.Read, options: sc.Options);
                        }
                    }
                    else
                    {
                        return await sc.ReplaceDialogAsync(Actions.Read, options: sc.Options);
                    }

                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(EmailSharedResponses.DidntUnderstandMessage));
                    return await sc.EndDialogAsync(true);
                }
                else
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(EmailSharedResponses.EmailNotFound));
                }

                return await sc.EndDialogAsync(true);
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
    }
}