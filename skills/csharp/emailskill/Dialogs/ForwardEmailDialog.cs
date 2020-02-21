// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
    public class ForwardEmailDialog : EmailSkillDialogBase
    {
        public ForwardEmailDialog(
            LocaleTemplateEngineManager localeTemplateEngineManager,
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ForwardEmailDialog), localeTemplateEngineManager, serviceProvider, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var forwardEmail = new WaterfallStep[]
            {
                IfClearContextStep,
                GetAuthToken,
                AfterGetAuthToken,
                SetDisplayConfig,
                CollectSelectedEmail,
                AfterCollectSelectedEmail,
                CollectRecipient,
                CollectAdditionalText,
                AfterCollectAdditionalText,
                GetAuthToken,
                AfterGetAuthToken,
                ConfirmBeforeSending,
                ConfirmAllRecipient,
                AfterConfirmPrompt,
                GetAuthToken,
                AfterGetAuthToken,
                ForwardEmail,
            };

            var showEmail = new WaterfallStep[]
            {
                PagingStep,
                GetAuthToken,
                AfterGetAuthToken,
                ShowEmails,
            };

            var collectRecipients = new WaterfallStep[]
            {
                PromptRecipientCollection,
                GetRecipients,
            };

            var updateSelectMessage = new WaterfallStep[]
            {
                UpdateMessage,
                PromptUpdateMessage,
                AfterUpdateMessage,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.Forward, forwardEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Show, showEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectRecipient, collectRecipients) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateSelectMessage, updateSelectMessage) { TelemetryClient = telemetryClient });
            AddDialog(new FindContactDialog(localeTemplateEngineManager, serviceProvider, telemetryClient));
            InitialDialogId = Actions.Forward;
        }

        public async Task<DialogTurnResult> ForwardEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                sc.Context.TurnState.TryGetValue(StateProperties.APIToken, out var token);
                var message = state.Message;
                var id = message.FirstOrDefault()?.Id;
                var recipients = state.FindContactInfor.Contacts;

                var service = ServiceManager.InitMailService(token as string, state.GetUserTimeZone(), state.MailSourceType);

                // send user message.
                var content = state.Content.Equals(EmailCommonStrings.EmptyContent) ? string.Empty : state.Content;
                await service.ForwardMessageAsync(id, content, recipients);

                var emailCard = new EmailCardData
                {
                    Subject = state.Subject.Equals(EmailCommonStrings.EmptySubject) ? null : state.Subject,
                    EmailContent = state.Content.Equals(EmailCommonStrings.EmptyContent) ? null : state.Content,
                };
                emailCard = await ProcessRecipientPhotoUrl(sc.Context, emailCard, state.FindContactInfor.Contacts);

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