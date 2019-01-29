using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.FindContact;
using EmailSkill.Dialogs.FindContact.Resources;
using EmailSkill.Dialogs.SendEmail.Prompts;
using EmailSkill.Dialogs.SendEmail.Resources;
using EmailSkill.Dialogs.Shared;
using EmailSkill.Dialogs.Shared.DialogOptions;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkill.Dialogs.Shared.Resources.Cards;
using EmailSkill.Dialogs.Shared.Resources.Strings;
using EmailSkill.ServiceClients;
using EmailSkill.Util;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using static EmailSkill.Models.SendEmailStateModel;

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
                ByPassOptionalField,
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

            var updateSubject = new WaterfallStep[]
            {
                UpdateSubject,
                RetryCollectSubject,
                AfterUpdateSubject,
            };

            var updateContent = new WaterfallStep[]
            {
                UpdateContent,
                PlayBackContent,
                AfterCollectContent,
            };

            var getRecreateInfo = new WaterfallStep[]
            {
                GetRecreateInfo,
                AfterGetRecreateInfo,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.Send, sendEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectRecipient, collectRecipients) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateSubject, updateSubject) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateContent, updateContent) { TelemetryClient = telemetryClient });
            AddDialog(new FindContactDialog(services, emailStateAccessor, dialogStateAccessor, serviceManager, telemetryClient));
            AddDialog(new WaterfallDialog(Actions.GetRecreateInfo, getRecreateInfo) { TelemetryClient = telemetryClient });
            AddDialog(new GetRecreateInfoPrompt(Actions.GetRecreateInfoPrompt));
            InitialDialogId = Actions.Send;
        }

        public async Task<DialogTurnResult> ByPassOptionalField(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var skillOptions = (EmailSkillDialogOptions)sc.Options;

                if (!skillOptions.SubFlowMode)
                {
                    if (state.NameList.Count > 0 || state.EmailList.Count > 0)
                    {
                        // Bypass logic: Send an email to Michelle saying I will be late today ->  Use “I will be late today” as subject. No need to ask for subject/content
                        // If information is detected as content, move to subject.
                        if (string.IsNullOrEmpty(state.Subject))
                        {
                            if (!string.IsNullOrEmpty(state.Content))
                            {
                                state.Subject = state.Content;
                                state.Content = EmailCommonStrings.EmptyContent;
                            }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(state.Content))
                            {
                                state.Content = EmailCommonStrings.EmptyContent;
                            }
                        }
                    }
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
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

                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;
                return await sc.BeginDialogAsync(Actions.UpdateSubject, skillOptions);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> UpdateSubject(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                if (state.Recipients.Count == 0 || state.Recipients == null)
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(FindContactResponses.UserNotFoundAgain, null, new StringDictionary() { { "source", state.MailSourceType == Model.MailSource.Microsoft ? "Outlook" : "Gmail" } }));
                    state.FirstRetryInFindContact = true;
                    return await sc.EndDialogAsync();
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

        public async Task<DialogTurnResult> RetryCollectSubject(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var subject);
                    var subjectInput = subject != null ? subject.ToString() : sc.Context.Activity.Text;

                    if (!EmailCommonPhrase.GetIsSkip(subjectInput))
                    {
                        state.Subject = subjectInput;
                    }
                }

                if (!string.IsNullOrWhiteSpace(state.Subject))
                {
                    return await sc.NextAsync();
                }

                return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = sc.Context.Activity.CreateReply(SendEmailResponses.RetryNoSubject), });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterUpdateSubject(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                if (!string.IsNullOrWhiteSpace(state.Subject))
                {
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }

                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var subject);
                    var subjectInput = subject != null ? subject.ToString() : sc.Context.Activity.Text;

                    if (!EmailCommonPhrase.GetIsSkip(subjectInput))
                    {
                        state.Subject = subjectInput;
                    }
                    else
                    {
                        state.Subject = EmailCommonStrings.EmptySubject;
                    }
                }

                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
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

                if (!string.IsNullOrWhiteSpace(state.Content))
                {
                    return await sc.NextAsync();
                }

                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;
                return await sc.BeginDialogAsync(Actions.UpdateContent, skillOptions);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> UpdateContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(SendEmailResponses.NoMessageBody) });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> PlayBackContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var contentInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                    if (!EmailCommonPhrase.GetIsSkip(contentInput))
                    {
                        state.Content = contentInput;

                        var emailCard = new EmailCardData
                        {
                            EmailContent = string.Format(EmailCommonStrings.ContentFormat, state.Content),
                        };

                        var stringToken = new StringDictionary
                        {
                            { "EmailContent", state.Content },
                        };

                        var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(SendEmailResponses.PlayBackMessage, "Dialogs/Shared/Resources/Cards/EmailContentPreview.json", emailCard, ResponseBuilder, stringToken);
                        await sc.Context.SendActivityAsync(replyMessage);

                        return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions()
                        {
                            Prompt = sc.Context.Activity.CreateReply(SendEmailResponses.CheckContent),
                            RetryPrompt = sc.Context.Activity.CreateReply(SendEmailResponses.ConfirmMessage_Retry, ResponseBuilder),
                        });
                    }
                    else
                    {
                        state.Content = EmailCommonStrings.EmptyContent;
                        return await sc.EndDialogAsync(cancellationToken);
                    }
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterCollectContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    return await sc.EndDialogAsync(true);
                }

                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SendEmailResponses.RetryContent));
                return await sc.ReplaceDialogAsync(Actions.GetRecreateInfo, options: sc.Options, cancellationToken: cancellationToken);
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

                    var stringToken = new StringDictionary
                    {
                        { "Subject", state.Subject },
                    };
                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(EmailSharedResponses.SentSuccessfully, "Dialogs/Shared/Resources/Cards/EmailWithOutButtonCard.json", emailCard, tokens: stringToken);
                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else
                {
                    return await sc.ReplaceDialogAsync(Actions.GetRecreateInfo, options: sc.Options, cancellationToken: cancellationToken);
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

        public async Task<DialogTurnResult> GetRecreateInfo(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.GetRecreateInfoPrompt, new PromptOptions
                {
                    Prompt = sc.Context.Activity.CreateReply(SendEmailResponses.GetRecreateInfo),
                    RetryPrompt = sc.Context.Activity.CreateReply(SendEmailResponses.GetRecreateInfo_Retry)
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterGetRecreateInfo(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;
                if (sc.Result != null)
                {
                    ResendEmailState? recreateState = sc.Result as ResendEmailState?;
                    switch (recreateState.Value)
                    {
                        case ResendEmailState.Cancel:
                            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailSharedResponses.CancellingMessage));
                            await ClearConversationState(sc);
                            return await sc.EndDialogAsync(false, cancellationToken);
                        case ResendEmailState.Participants:
                            state.ClearParticipants();
                            return await sc.ReplaceDialogAsync(Actions.Send, options: skillOptions, cancellationToken: cancellationToken);
                        case ResendEmailState.Subject:
                            state.ClearSubject();
                            return await sc.ReplaceDialogAsync(Actions.Send, options: skillOptions, cancellationToken: cancellationToken);
                        case ResendEmailState.Content:
                            state.ClearContent();
                            return await sc.ReplaceDialogAsync(Actions.Send, options: skillOptions, cancellationToken: cancellationToken);
                        default:
                            // should not go to this part. place an error handling for save.
                            await HandleDialogExceptions(sc, new Exception("Get unexpect state in recreate."));
                            return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                    }
                }
                else
                {
                    // should not go to this part. place an error handling for save.
                    await HandleDialogExceptions(sc, new Exception("Get unexpect result in recreate."));
                    return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}