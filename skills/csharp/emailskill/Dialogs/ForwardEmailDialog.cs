// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Models;
using EmailSkill.Responses.Shared;
using EmailSkill.Services;
using EmailSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;

namespace EmailSkill.Dialogs
{
    public class ForwardEmailDialog : EmailSkillDialogBase
    {
        public ForwardEmailDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ForwardEmailDialog), serviceProvider, telemetryClient)
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
            AddDialog(new FindContactDialog(serviceProvider, telemetryClient));
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

                var service = ServiceManager.InitMailService((string)token, state.GetUserTimeZone(), state.MailSourceType);

                // send user message.
                var content = state.Content.Equals(EmailCommonStrings.EmptyContent) ? string.Empty : state.Content;
                await service.ForwardMessageAsync(id, content, recipients);

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

                var recipientCard = state.FindContactInfor.Contacts.Count() > 5 ? GetDivergedCardName(sc.Context, "ConfirmCard_RecipientMoreThanFive") : GetDivergedCardName(sc.Context, "ConfirmCard_RecipientLessThanFive");
                var reply = ResponseManager.GetCardResponse(
                    EmailSharedResponses.SentSuccessfully,
                    new Card("EmailWithOutButtonCard", emailCard),
                    stringToken,
                    "items",
                    new List<Card>().Append(new Card(recipientCard, emailCard)));

                await sc.Context.SendActivityAsync(reply);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }

            await ClearConversationState(sc);
            return await sc.EndDialogAsync(true);
        }
    }
}