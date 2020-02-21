// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Models;
using EmailSkill.Models.Action;
using EmailSkill.Responses.Shared;
using EmailSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Util;

namespace EmailSkill.Dialogs
{
    public class ReplyEmailDialog : EmailSkillDialogBase
    {
        public ReplyEmailDialog(
            LocaleTemplateEngineManager localeTemplateEngineManager,
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ReplyEmailDialog), localeTemplateEngineManager, serviceProvider, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var replyEmail = new WaterfallStep[]
            {
                IfClearContextStep,
                GetAuthToken,
                AfterGetAuthToken,
                SetDisplayConfig,
                CollectSelectedEmail,
                AfterCollectSelectedEmail,
                CollectAdditionalText,
                AfterCollectAdditionalText,
                GetAuthToken,
                AfterGetAuthToken,
                ConfirmBeforeSending,
                AfterConfirmPrompt,
                GetAuthToken,
                AfterGetAuthToken,
                ReplyEmail,
            };

            var showEmail = new WaterfallStep[]
            {
                PagingStep,
                GetAuthToken,
                AfterGetAuthToken,
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
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                sc.Context.TurnState.TryGetValue(StateProperties.APIToken, out var token);
                var message = state.Message.FirstOrDefault();

                var service = ServiceManager.InitMailService(token as string, state.GetUserTimeZone(), state.MailSourceType);

                // reply user message.
                if (message != null)
                {
                    var content = state.Content.Equals(EmailCommonStrings.EmptyContent) ? string.Empty : state.Content;
                    await service.ReplyToMessageAsync(message.Id, content);
                }

                var emailCard = new EmailCardData
                {
                    Subject = state.Subject.Equals(EmailCommonStrings.EmptySubject) ? null : state.Subject,
                    EmailContent = state.Content.Equals(EmailCommonStrings.EmptyContent) ? null : state.Content,
                };
                emailCard = await ProcessRecipientPhotoUrl(sc.Context, emailCard, state.FindContactInfor.Contacts);

                var stringToken = new StringDictionary
                {
                    { "Subject", state.Subject },
                };

                var reply = TemplateEngine.GenerateActivityForLocale(
                EmailSharedResponses.SentSuccessfully,
                new
                {
                    subject = state.Subject,
                    emailDetails = emailCard
                });

                await sc.Context.SendActivityAsync(reply);
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
    }
}