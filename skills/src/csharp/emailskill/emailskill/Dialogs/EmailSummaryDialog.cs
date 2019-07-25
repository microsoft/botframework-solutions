using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Extensions;
using EmailSkill.Models;
using EmailSkill.Responses.Shared;
using EmailSkill.Responses.ShowEmail;
using EmailSkill.Services;
using EmailSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Graph;
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
            DeleteEmailDialog deleteEmailDialog,
            ReplyEmailDialog replyEmailDialog,
            ForwardEmailDialog forwardEmailDialog,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(EmailSummaryDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            var summaryDialog = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                GetSummary
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog("summaryDialog", summaryDialog) { TelemetryClient = telemetryClient });
            InitialDialogId = "summaryDialog";
        }

        protected async Task<DialogTurnResult> GetSummary(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                var (messages, totalCount, importantCount) = await GetMessagesAsync(sc);

                // Get display messages
                var displayMessages = new List<Message>();
                var startIndex = 0;
                for (var i = startIndex; i < messages.Count(); i++)
                {
                    displayMessages.Add(messages[i]);
                }

                var response = sc.Context.Activity.CreateReply();
                var entities = new Dictionary<string, Microsoft.Bot.Schema.Entity>();

                /*
                entities.Add("title", new Microsoft.Bot.Schema.Entity { Properties = JObject.FromObject(new { reminder = "Email" }) });
                entities.Add("totalCount", new Microsoft.Bot.Schema.Entity { Properties = JObject.FromObject(new { reminder = totalCount }) });
                foreach (var result in displayMessages)
                {
                    entities.Add((displayMessages.IndexOf(result) + 1).ToString(), new Microsoft.Bot.Schema.Entity { Properties = JObject.FromObject(new { reminder = result.Subject }) });
                }
                */

                response.Name = "emailSkill.EmailSummary";
                var items = new JArray();
                foreach (var message in displayMessages)
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
                    name = "emailSkill.EmailSummary",
                    totalCount = totalCount,
                    items = items
                });
                entities.Add(response.Name, new Microsoft.Bot.Schema.Entity { Properties = obj });
                response.SemanticAction = new SemanticAction("entity", entities);
                response.Type = ActivityTypes.EndOfConversation;

                await sc.Context.SendActivityAsync(response);
                return await sc.EndDialogAsync(true);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}