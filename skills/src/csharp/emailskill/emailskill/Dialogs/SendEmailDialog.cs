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
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace EmailSkill.Dialogs
{
    public class SendEmailDialog : EmailSkillDialogBase
    {
        private ResourceMultiLanguageGenerator _lgMultiLangEngine;

        public SendEmailDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            FindContactDialog findContactDialog,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(SendEmailDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            _lgMultiLangEngine = new ResourceMultiLanguageGenerator("SendEmail.lg");

            var sendEmail = new WaterfallStep[]
            {
                InitEmailSendDialogState,
                GetAuthToken,
                AfterGetAuthToken,
                CollectRecipient,
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
                //PromptRecipientCollection,
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

        //public async Task<DialogTurnResult> ByPassOptionalField(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        //{
        //    try
        //    {
        //        if (sc.Result != null && sc.Result is FindContactDialogOptions)
        //        {
        //            var result = (FindContactDialogOptions)sc.Result;
        //            sc.State.Dialog[EmailStateKey] = result.DialogState;
        //        }

        //        var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
        //        var skillOptions = (EmailSkillDialogOptions)sc.Options;

        //        if (!skillOptions.SubFlowMode)
        //        {
        //            if ((state.FindContactInfor.Contacts != null) && (state.FindContactInfor.Contacts.Count > 0))
        //            {
        //                // Bypass logic: Send an email to Michelle saying I will be late today ->  Use “I will be late today” as subject. No need to ask for subject/content
        //                // If information is detected as content, move to subject.
        //                if (string.IsNullOrEmpty(state.Subject))
        //                {
        //                    if (!string.IsNullOrEmpty(state.Content))
        //                    {
        //                        state.Subject = state.Content;
        //                        state.Content = EmailCommonStrings.EmptyContent;
        //                    }
        //                }
        //                else
        //                {
        //                    if (string.IsNullOrEmpty(state.Content))
        //                    {
        //                        state.Content = EmailCommonStrings.EmptyContent;
        //                    }
        //                }
        //            }
        //        }

        //        return await sc.NextAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        await HandleDialogExceptions(sc, ex);
        //        return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
        //    }
        //}

        public async Task<DialogTurnResult> CollectSubject(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (sc.Result != null && sc.Result is FindContactDialogOptions)
                {
                    var result = (FindContactDialogOptions)sc.Result;
                    sc.State.Dialog[EmailStateKey] = result.DialogState;
                }

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

                bool? isSkipByDefault = false;
                isSkipByDefault = Settings.DefaultValue?.SendEmail?.First(item => item.Name == "EmailSubject")?.IsSkipByDefault;
                if (isSkipByDefault.GetValueOrDefault())
                {
                    state.Subject = string.IsNullOrEmpty(EmailCommonStrings.DefaultSubject) ? EmailCommonStrings.EmptySubject : EmailCommonStrings.DefaultSubject;

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

                var recipientConfirmedMessage = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[RecipientConfirmed]", new { userName = await GetNameListStringAsync(sc) });
                var noSubjectMessage = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[NoSubject]", null);
                noSubjectMessage.Text = recipientConfirmedMessage.Text + " " + noSubjectMessage.Text;
                noSubjectMessage.Speak = recipientConfirmedMessage.Speak + " " + noSubjectMessage.Speak;

                return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = noSubjectMessage as Activity, });
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

                var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[RetryNoSubject]", null);
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = activity as Activity });
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

                bool? isSkipByDefault = false;
                isSkipByDefault = Settings.DefaultValue?.SendEmail?.First(item => item.Name == "EmailMessage")?.IsSkipByDefault;
                if (isSkipByDefault.GetValueOrDefault())
                {
                    state.Subject = string.IsNullOrEmpty(EmailCommonStrings.DefaultContent) ? EmailCommonStrings.EmptyContent : EmailCommonStrings.DefaultContent;

                    return await sc.NextAsync();
                }

                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;
                skillOptions.DialogState = state;
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];

                var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[NoMessageBody]", null);
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = activity as Activity });
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

                        var replyMessage = await LGHelper.GenerateAdaptiveCardAsync(
                        _lgMultiLangEngine,
                        sc.Context,
                        "[PlayBackMessage]",
                        new { emailContent = state.Content },
                        "[EmailContentPreview]",
                        new { emailDetails = emailCard });

                        await sc.Context.SendActivityAsync(replyMessage);

                        var checkContentActivity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[CheckContent]", null);
                        var confirmMessageRetryActivity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ConfirmMessageRetry]", null);
                        return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions()
                        {
                            Prompt = checkContentActivity as Activity,
                            RetryPrompt = confirmMessageRetryActivity as Activity,
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

                var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[RetryContent]", null);
                await sc.Context.SendActivityAsync(activity);
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
                        RecipientsCount = state.FindContactInfor.Contacts.Count()
                    };
                    emailCard = await ProcessRecipientPhotoUrl(sc.Context, emailCard, state.FindContactInfor.Contacts);

                    var replyMessage = await LGHelper.GenerateAdaptiveCardAsync(
                        _lgMultiLangEngine,
                        sc.Context,
                        "[SentSuccessfully]",
                        new { subject = state.Subject },
                        "[EmailWithOutButtonCard(emailDetails)]",
                        new { emailDetails = emailCard });

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
                var getRecreateInfoActivity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[GetRecreateInfo]", null);
                var getRecreateInfoRetryActivity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[GetRecreateInfoRetry]", null);
                return await sc.PromptAsync(Actions.GetRecreateInfoPrompt, new PromptOptions
                {
                    Prompt = getRecreateInfoActivity as Activity,
                    RetryPrompt = getRecreateInfoRetryActivity as Activity
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
                            var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[CancellingMessage]", null);
                            await sc.Context.SendActivityAsync(activity);
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