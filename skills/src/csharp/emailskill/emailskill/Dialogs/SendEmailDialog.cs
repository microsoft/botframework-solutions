using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Models;
using EmailSkill.Models.DialogModel;
using EmailSkill.Prompts;
using EmailSkill.Responses.SendEmail;
using EmailSkill.Responses.Shared;
using EmailSkill.Services;
using EmailSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;

namespace EmailSkill.Dialogs
{
    public class SendEmailDialog : EmailSkillDialogBase
    {
        public SendEmailDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            FindContactDialog findContactDialog,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(SendEmailDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var sendEmail = new WaterfallStep[]
            {
                InitEmailSendDialogState,
                GetAuthToken,
                AfterGetAuthToken,
                CollectRecipient,
                ByPassOptionalField,
                CollectSubject,
                AfterCollection,
                CollectText,
                AfterCollection,
                ConfirmBeforeSending,
                ConfirmAllRecipient,
                SendEmail,
            };

            var collectRecipients = new WaterfallStep[]
            {
                SaveEmailSendDialogState,
                PromptRecipientCollection,
                GetRecipients,
            };

            var updateSubject = new WaterfallStep[]
            {
                SaveEmailSendDialogState,
                UpdateSubject,
                RetryCollectSubject,
                AfterUpdateSubject,
            };

            var updateContent = new WaterfallStep[]
            {
                SaveEmailSendDialogState,
                UpdateContent,
                PlayBackContent,
                AfterCollectContent,
            };

            var getRecreateInfo = new WaterfallStep[]
            {
                SaveEmailSendDialogState,
                GetRecreateInfo,
                AfterGetRecreateInfo,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new EmailWaterfallDialog(Actions.Send, sendEmail, EmailStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new EmailWaterfallDialog(Actions.CollectRecipient, collectRecipients, EmailStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new EmailWaterfallDialog(Actions.UpdateSubject, updateSubject, EmailStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new EmailWaterfallDialog(Actions.UpdateContent, updateContent, EmailStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new FindContactDialog(settings, services, responseManager, conversationState, serviceManager, telemetryClient));
            AddDialog(new EmailWaterfallDialog(Actions.GetRecreateInfo, getRecreateInfo, EmailStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new GetRecreateInfoPrompt(Actions.GetRecreateInfoPrompt));
            InitialDialogId = Actions.Send;
        }

        public async Task<DialogTurnResult> ByPassOptionalField(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (sc.Result != null && sc.Result is FindContactDialogOptions)
                {
                    var result = (FindContactDialogOptions)sc.Result;
                    sc.State.Dialog[EmailStateKey] = result.DialogState;
                }

                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                var skillOptions = (EmailSkillDialogOptions)sc.Options;

                if (!skillOptions.SubFlowMode)
                {
                    if ((state.FindContactInfor.Contacts != null) && (state.FindContactInfor.Contacts.Count > 0))
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];

                if (state.FindContactInfor.Contacts == null || state.FindContactInfor.Contacts.Count == 0)
                {
                    state.FindContactInfor.FirstRetryInFindContact = true;
                    return await sc.EndDialogAsync();
                }

                if (!string.IsNullOrWhiteSpace(state.Subject))
                {
                    return await sc.NextAsync();
                }

                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;
                skillOptions.DialogState = state;
                return await sc.BeginDialogAsync(Actions.UpdateSubject, skillOptions);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterCollection(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // save state to dialog state.
                if (sc.Result is EmailSkillDialogOptions skillOptions)
                {
                    sc.State.Dialog[EmailStateKey] = skillOptions.DialogState;
                }

                return await sc.NextAsync();
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];

                if (state.FindContactInfor.Contacts == null || state.FindContactInfor.Contacts.Count == 0)
                {
                    state.FindContactInfor.FirstRetryInFindContact = true;
                    return await sc.EndDialogAsync();
                }

                var recipientConfirmedMessage = ResponseManager.GetResponse(EmailSharedResponses.RecipientConfirmed, new StringDictionary() { { "UserName", await GetNameListStringAsync(sc) } });
                var noSubjectMessage = ResponseManager.GetResponse(SendEmailResponses.NoSubject);
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
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

                return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = ResponseManager.GetResponse(SendEmailResponses.RetryNoSubject), });
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                var skillOptions = (EmailSkillDialogOptions)sc.Options;

                if (!string.IsNullOrWhiteSpace(state.Subject))
                {
                    skillOptions.DialogState = state;
                    return await sc.EndDialogAsync(skillOptions, cancellationToken: cancellationToken);
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

                skillOptions.DialogState = state;
                return await sc.EndDialogAsync(skillOptions, cancellationToken: cancellationToken);
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];

                if (state.FindContactInfor.Contacts == null || state.FindContactInfor.Contacts.Count == 0)
                {
                    state.FindContactInfor.FirstRetryInFindContact = true;
                    return await sc.EndDialogAsync();
                }

                if (!string.IsNullOrWhiteSpace(state.Content))
                {
                    return await sc.NextAsync();
                }

                var options = (EmailSkillDialogOptions)sc.Options;
                options.SubFlowMode = true;
                options.DialogState = state;
                return await sc.BeginDialogAsync(Actions.UpdateContent, options);
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(SendEmailResponses.NoMessageBody) });
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var contentInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                    if (!EmailCommonPhrase.GetIsSkip(contentInput))
                    {
                        state.Content = contentInput;

                        var emailCard = new EmailCardData
                        {
                            Subject = EmailCommonStrings.MessageConfirm,
                            EmailContent = state.Content,
                        };

                        var stringToken = new StringDictionary
                        {
                            { "EmailContent", state.Content },
                        };

                        var replyMessage = ResponseManager.GetCardResponse(
                            SendEmailResponses.PlayBackMessage,
                            new Card("EmailContentPreview", emailCard),
                            stringToken);

                        await sc.Context.SendActivityAsync(replyMessage);

                        return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions()
                        {
                            Prompt = ResponseManager.GetResponse(SendEmailResponses.CheckContent),
                            RetryPrompt = ResponseManager.GetResponse(SendEmailResponses.ConfirmMessageRetry),
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.DialogState = state;

                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    return await sc.EndDialogAsync(skillOptions);
                }

                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SendEmailResponses.RetryContent));
                return await sc.ReplaceDialogAsync(Actions.GetRecreateInfo, options: skillOptions, cancellationToken: cancellationToken);
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                if (confirmResult)
                {
                    var userState = await EmailStateAccessor.GetAsync(sc.Context);
                    var token = userState.Token;

                    var service = ServiceManager.InitMailService(token, userState.GetUserTimeZone(), userState.MailSourceType);

                    // send user message.
                    var subject = state.Subject.Equals(EmailCommonStrings.EmptySubject) ? string.Empty : state.Subject;
                    var content = state.Content.Equals(EmailCommonStrings.EmptyContent) ? string.Empty : state.Content;
                    await service.SendMessageAsync(content, subject, state.FindContactInfor.Contacts);

                    var emailCard = new EmailCardData
                    {
                        Subject = state.Subject.Equals(EmailCommonStrings.EmptySubject) ? null : string.Format(EmailCommonStrings.SubjectFormat, state.Subject),
                        EmailContent = state.Content.Equals(EmailCommonStrings.EmptyContent) ? null : string.Format(EmailCommonStrings.ContentFormat, state.Content),
                    };
                    emailCard = await ProcessRecipientPhotoUrl(sc.Context, emailCard, state.FindContactInfor.Contacts);

                    var stringToken = new StringDictionary
                    {
                        { "Subject", state.Subject },
                    };

                    var recipientCard = state.FindContactInfor.Contacts.Count() > 5 ? "ConfirmCard_RecipientMoreThanFive" : "ConfirmCard_RecipientLessThanFive";
                    var replyMessage = ResponseManager.GetCardResponse(
                        EmailSharedResponses.SentSuccessfully,
                        new Card("EmailWithOutButtonCard", emailCard),
                        stringToken,
                        "items",
                        new List<Card>().Append(new Card(recipientCard, emailCard)));

                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else
                {
                    var skillOptions = (EmailSkillDialogOptions)sc.Options;
                    skillOptions.DialogState = state;
                    return await sc.ReplaceDialogAsync(Actions.GetRecreateInfo, options: skillOptions, cancellationToken: cancellationToken);
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

            //await ClearConversationState(sc);
            return await sc.EndDialogAsync(true);
        }

        public async Task<DialogTurnResult> GetRecreateInfo(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.GetRecreateInfoPrompt, new PromptOptions
                {
                    Prompt = ResponseManager.GetResponse(SendEmailResponses.GetRecreateInfo),
                    RetryPrompt = ResponseManager.GetResponse(SendEmailResponses.GetRecreateInfoRetry)
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;
                if (sc.Result != null)
                {
                    var recreateState = sc.Result as ResendEmailState?;
                    switch (recreateState.Value)
                    {
                        case ResendEmailState.Cancel:
                            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(EmailSharedResponses.CancellingMessage));
                            //await ClearConversationState(sc);
                            return await sc.EndDialogAsync(false, cancellationToken);
                        case ResendEmailState.Participants:
                            state.ClearParticipants();
                            skillOptions.DialogState = state;
                            return await sc.ReplaceDialogAsync(Actions.Send, options: skillOptions, cancellationToken: cancellationToken);
                        case ResendEmailState.Subject:
                            state.ClearSubject();
                            skillOptions.DialogState = state;
                            return await sc.ReplaceDialogAsync(Actions.Send, options: skillOptions, cancellationToken: cancellationToken);
                        case ResendEmailState.Content:
                            state.ClearContent();
                            skillOptions.DialogState = state;
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