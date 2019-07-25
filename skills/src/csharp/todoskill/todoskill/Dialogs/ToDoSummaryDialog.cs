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
                state.LastListType = state.ListType;
                var service = await InitListTypeIds(sc);
                var topIntent = state.LuisResult?.TopIntent().intent;
                var results = await service.GetTasksAsync(state.ListType);

                var response = sc.Context.Activity.CreateReply();
                var entities = new Dictionary<string, Entity>();
                /*
                entities.Add("title", new Entity { Properties = JObject.FromObject(new { reminder = "Todo" }) });
                foreach (var result in results)
                {
                    entities.Add((results.IndexOf(result) + 1).ToString(), new Entity { Properties = JObject.FromObject(new { reminder = result.Topic }) });
                }
                */

                response.Name = "todoSkill.MeetingSummary";
                var items = new JArray();
                var totalCount = results.Count;
                foreach (var result in results)
                {
                    items.Add(JObject.FromObject(new
                    {
                        title = result.Topic,
                        time = result.ReminderDateTime.ToString()
                    }));
                }
                var obj = JObject.FromObject(new
                {
                    name = "todoSkill.MeetingSummary",
                    totalCount = totalCount,
                    items = items
                });
                entities.Add(response.Name, new Microsoft.Bot.Schema.Entity { Properties = obj });
                response.SemanticAction = new SemanticAction("entity", entities);
                response.Type = ActivityTypes.EndOfConversation;

                //await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ShowToDoResponses.NoTasksMessage, new StringDictionary() { { "listType", state.ListType } }));
                await sc.Context.SendActivityAsync(response);
                return await sc.EndDialogAsync(true);
                //return new DialogTurnResult(DialogTurnStatus.Complete, results);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}