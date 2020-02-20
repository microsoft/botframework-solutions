// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Models;
using EmailSkill.Models.Action;
using EmailSkill.Prompts;
using EmailSkill.Responses.SendEmail;
using EmailSkill.Responses.Shared;
using EmailSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;

namespace EmailSkill.Dialogs
{
    public class SendEmailDialog : EmailSkillDialogBase
    {
        public SendEmailDialog(
            LocaleTemplateEngineManager localeTemplateEngineManager,
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(SendEmailDialog), localeTemplateEngineManager, serviceProvider, telemetryClient)
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
                GetAuthToken,
                AfterGetAuthToken,
                ConfirmBeforeSending,
                ConfirmAllRecipient,
                AfterConfirmPrompt,
                GetAuthToken,
                AfterGetAuthToken,
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
            AddDialog(new FindContactDialog(localeTemplateEngineManager, serviceProvider, telemetryClient));
            AddDialog(new WaterfallDialog(Actions.GetRecreateInfo, getRecreateInfo) { TelemetryClient = telemetryClient });
            AddDialog(new GetRecreateInfoPrompt(Actions.GetRecreateInfoPrompt));
            InitialDialogId = Actions.Send;
        }

        public async Task<DialogTurnResult> CollectSubject(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

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

                if (state.FindContactInfor.Contacts == null || state.FindContactInfor.Contacts.Count == 0)
                {
                    state.FindContactInfor.FirstRetryInFindContact = true;
                    return await sc.EndDialogAsync();
                }

                var recipientConfirmedMessage = TemplateEngine.GenerateActivityForLocale(EmailSharedResponses.RecipientConfirmed, new { userName = await GetNameListStringAsync(sc, false) });
                var noSubjectMessage = TemplateEngine.GenerateActivityForLocale(SendEmailResponses.NoSubject);
                noSubjectMessage.Text = recipientConfirmedMessage.Text + " " + noSubjectMessage.Text;
                noSubjectMessage.Speak = recipientConfirmedMessage.Speak + " " + noSubjectMessage.Speak;

                return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = noSubjectMessage as Activity });
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

                var activity = TemplateEngine.GenerateActivityForLocale(SendEmailResponses.RetryNoSubject);
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
                var activity = TemplateEngine.GenerateActivityForLocale(SendEmailResponses.NoMessageBody);
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
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var contentInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                    if (!EmailCommonPhrase.GetIsSkip(contentInput))
                    {
                        state.Content = contentInput;

                        var replyMessage = TemplateEngine.GenerateActivityForLocale(
                        SendEmailResponses.PlayBackMessage,
                        new
                        {
                            emailContent = state.Content,
                        });

                        var confirmMessageRetryActivity = TemplateEngine.GenerateActivityForLocale(SendEmailResponses.ConfirmMessageRetry);
                        return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions()
                        {
                            Prompt = replyMessage as Activity,
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
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    return await sc.EndDialogAsync(true);
                }

                var activity = TemplateEngine.GenerateActivityForLocale(SendEmailResponses.RetryContent);
                await sc.Context.SendActivityAsync(activity);
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
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                sc.Context.TurnState.TryGetValue(StateProperties.APIToken, out var token);

                var service = ServiceManager.InitMailService(token as string, state.GetUserTimeZone(), state.MailSourceType);

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

                var replyMessage = TemplateEngine.GenerateActivityForLocale(
                    EmailSharedResponses.SentSuccessfully,
                    new
                    {
                        subject = state.Subject,
                        emailDetails = emailCard
                    });

                await sc.Context.SendActivityAsync(replyMessage);
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
            var skillOptions = sc.Options as EmailSkillDialogOptions;
            if (skillOptions != null && skillOptions.IsAction)
            {
                var actionResult = new ActionResult() { ActionSuccess = true };
                return await sc.EndDialogAsync(actionResult);
            }

            return await sc.EndDialogAsync(true);
        }

        public async Task<DialogTurnResult> GetRecreateInfo(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var getRecreateInfoActivity = TemplateEngine.GenerateActivityForLocale(SendEmailResponses.GetRecreateInfo);
                var getRecreateInfoRetryActivity = TemplateEngine.GenerateActivityForLocale(SendEmailResponses.GetRecreateInfoRetry);
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
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;
                if (sc.Result != null)
                {
                    var recreateState = sc.Result as ResendEmailState?;
                    switch (recreateState.Value)
                    {
                        case ResendEmailState.Cancel:
                            var activity = TemplateEngine.GenerateActivityForLocale(EmailSharedResponses.CancellingMessage);
                            await sc.Context.SendActivityAsync(activity);
                            await ClearConversationState(sc);
                            return await sc.EndDialogAsync(false, cancellationToken);
                        case ResendEmailState.Recipients:
                            state.ClearParticipants();
                            return await sc.ReplaceDialogAsync(Actions.Send, options: skillOptions, cancellationToken: cancellationToken);
                        case ResendEmailState.Subject:
                            state.ClearSubject();
                            return await sc.ReplaceDialogAsync(Actions.Send, options: skillOptions, cancellationToken: cancellationToken);
                        case ResendEmailState.Body:
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