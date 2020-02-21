// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Extensions;
using EmailSkill.Models;
using EmailSkill.Models.Action;
using EmailSkill.Responses.DeleteEmail;
using EmailSkill.Responses.Shared;
using EmailSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Util;

namespace EmailSkill.Dialogs
{
    public class DeleteEmailDialog : EmailSkillDialogBase
    {
        public DeleteEmailDialog(
            LocaleTemplateEngineManager localeTemplateEngineManager,
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(DeleteEmailDialog), localeTemplateEngineManager, serviceProvider, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var deleteEmail = new WaterfallStep[]
            {
                IfClearContextStep,
                GetAuthToken,
                AfterGetAuthToken,
                SetDisplayConfig,
                CollectSelectedEmail,
                AfterCollectSelectedEmail,
                GetAuthToken,
                AfterGetAuthToken,
                PromptToDelete,
                AfterConfirmPrompt,
                GetAuthToken,
                AfterGetAuthToken,
                DeleteEmail,
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

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.Delete, deleteEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Show, showEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateSelectMessage, updateSelectMessage) { TelemetryClient = telemetryClient });
            InitialDialogId = Actions.Delete;
        }

        public async Task<DialogTurnResult> PromptToDelete(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var skillOptions = (EmailSkillDialogOptions)sc.Options;

                var message = state.Message?.FirstOrDefault();
                if (message != null)
                {
                    var nameListString = DisplayHelper.ToDisplayRecipientsString_Summay(message.ToRecipients);
                    var senderIcon = await GetUserPhotoUrlAsync(sc.Context, message.Sender.EmailAddress);
                    var emailCard = new EmailCardData
                    {
                        Subject = message.Subject,
                        EmailContent = message.BodyPreview,
                        Sender = message.Sender.EmailAddress.Name,
                        EmailLink = message.WebLink,
                        ReceivedDateTime = message?.ReceivedDateTime == null
                            ? CommonStrings.NotAvailable
                            : message.ReceivedDateTime.Value.UtcDateTime.ToDetailRelativeString(state.GetUserTimeZone()),
                        Speak = SpeakHelper.ToSpeechEmailDetailOverallString(message, state.GetUserTimeZone()),
                        SenderIcon = senderIcon
                    };
                    emailCard = await ProcessRecipientPhotoUrl(sc.Context, emailCard, message.ToRecipients);

                    var speech = SpeakHelper.ToSpeechEmailSendDetailString(message.Subject, nameListString, message.BodyPreview);
                    var prompt = TemplateEngine.GenerateActivityForLocale(
                        DeleteEmailResponses.DeleteConfirm,
                        new
                        {
                            emailInfo = speech,
                            emailDetails = emailCard
                        });

                    var retry = TemplateEngine.GenerateActivityForLocale(EmailSharedResponses.ConfirmSendFailed);
                    return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = prompt as Activity, RetryPrompt = retry as Activity });
                }

                skillOptions.SubFlowMode = true;
                return await sc.BeginDialogAsync(Actions.UpdateSelectMessage, skillOptions);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> DeleteEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                sc.Context.TurnState.TryGetValue(StateProperties.APIToken, out var token);
                var mailService = this.ServiceManager.InitMailService(token as string, state.GetUserTimeZone(), state.MailSourceType);
                var focusMessage = state.Message.FirstOrDefault();
                await mailService.DeleteMessageAsync(focusMessage.Id);
                var activity = TemplateEngine.GenerateActivityForLocale(DeleteEmailResponses.DeleteSuccessfully);
                await sc.Context.SendActivityAsync(activity);

                var skillOptions = sc.Options as EmailSkillDialogOptions;
                if (skillOptions != null && skillOptions.IsAction)
                {
                    var actionResult = new ActionResult() { ActionSuccess = true };
                    return await sc.EndDialogAsync(actionResult);
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}