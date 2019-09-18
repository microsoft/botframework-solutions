using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Extensions;
using EmailSkill.Responses.EmailSummary;
using EmailSkill.Services;
using EmailSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace EmailSkill.Dialogs
{
    public class EmailSummaryDialog : EmailSkillDialogBase
    {
        public EmailSummaryDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(EmailSummaryDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            var getEmailSummary = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                GetEmailSummary
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.GetEmailSummary, getEmailSummary) { TelemetryClient = telemetryClient });
            InitialDialogId = Actions.GetEmailSummary;
        }

        protected async Task<DialogTurnResult> GetEmailSummary(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.Token))
                {
                    state.Clear();
                    return await sc.EndDialogAsync(true);
                }

                TimeZoneInfo userTimeZone = state.GetUserTimeZone();
                var searchDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone);
                state.StartDateTime = TimeZoneInfo.ConvertTimeToUtc(new DateTime(searchDate.Year, searchDate.Month, searchDate.Day), userTimeZone);
                state.EndDateTime = TimeZoneInfo.ConvertTimeToUtc(new DateTime(searchDate.Year, searchDate.Month, searchDate.Day, 23, 59, 59), userTimeZone);
                var (messages, totalCount, importantCount) = await GetMessagesAsync(sc);

                SemanticAction semanticAction = new SemanticAction("email_summary", new Dictionary<string, Microsoft.Bot.Schema.Entity>());
                var items = new JArray();
                foreach (var message in messages)
                {
                    items.Add(JObject.FromObject(new
                    {
                        title = message.Subject,
                        Sender = message.Sender.EmailAddress.Name,
                        EmailContent = message.BodyPreview,
                        EmailLink = message.WebLink,
                        ReceivedDateTime = message?.ReceivedDateTime == null
                            ? CommonStrings.NotAvailable
                            : message.ReceivedDateTime.Value.UtcDateTime.ToDetailRelativeString(state.GetUserTimeZone()),
                        Speak = SpeakHelper.ToSpeechEmailDetailOverallString(message, state.GetUserTimeZone())
                    }));
                }

                var obj = JObject.FromObject(new
                {
                    name = EmailSummaryStrings.EMAIL_SUMMARY_SHOW_NAME,
                    totalCount = totalCount,
                    items = items
                });

                semanticAction.Entities.Add("EmailSkill.EmailSummary", new Microsoft.Bot.Schema.Entity { Properties = obj });
                semanticAction.State = SemanticActionStates.Done;

                state.Clear();
                return await sc.EndDialogAsync(semanticAction);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}