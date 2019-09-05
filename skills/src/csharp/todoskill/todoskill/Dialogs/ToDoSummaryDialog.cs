using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using ToDoSkill.Models;
using ToDoSkill.Responses.Shared;
using ToDoSkill.Responses.ShowToDo;
using ToDoSkill.Services;
using ToDoSkill.Utilities;

namespace ToDoSkill.Dialogs
{
    public class ToDoSummaryDialog : ToDoSkillDialogBase
    {
        public ToDoSummaryDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            UserState userState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials,
            IHttpContextAccessor httpContext)
            : base(nameof(ToDoSummaryDialog), settings, services, responseManager, conversationState, userState, serviceManager, telemetryClient, appCredentials, httpContext)
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

            // Set starting dialog for component
            InitialDialogId = "summaryDialog";
        }

        protected async Task<DialogTurnResult> GetSummary(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                state.ListType = state.ListType ?? ToDoStrings.ToDo;
                var service = await InitListTypeIds(sc);
                var results = await service.GetTasksAsync(state.ListType);

                SemanticAction semanticAction = new SemanticAction("summary", new Dictionary<string, Entity>());

                var items = new JArray();
                var totalCount = results.Count;
                foreach (var result in results)
                {
                    items.Add(JObject.FromObject(new
                    {
                        title = result.Topic,
                    }));
                }

                var obj = JObject.FromObject(new
                {
                    name = "ToDoSkill.MeetingSummary",
                    totalCount = totalCount,
                    items = items
                });

                semanticAction.Entities.Add("summary", new Entity { Properties = obj });
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