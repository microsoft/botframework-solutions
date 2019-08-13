// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Prompts;
using ITSMSkill.Responses.Knowledge;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace ITSMSkill.Dialogs
{
    public class ShowKnowledgeDialog : SkillDialogBase
    {
        public ShowKnowledgeDialog(
             BotSettings settings,
             BotServices services,
             ResponseManager responseManager,
             ConversationState conversationState,
             IServiceManager serviceManager,
             IBotTelemetryClient telemetryClient)
            : base(nameof(ShowKnowledgeDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient)
        {
            var showKnowledge = new WaterfallStep[]
            {
                CheckDescription,
                InputDescription,
                SetDescription,
                GetAuthToken,
                AfterGetAuthToken,
                ShowKnowledge
            };

            AddDialog(new WaterfallDialog(Actions.ShowKnowledge, showKnowledge));

            InitialDialogId = Actions.ShowKnowledge;
        }

        protected async Task<DialogTurnResult> ShowKnowledge(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            if (state.Token == null)
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.AuthFailed));
                return await sc.EndDialogAsync();
            }

            var management = ServiceManager.CreateManagement(Settings, state.Token);
            var result = await management.SearchKnowledge(state.TicketDescription);

            if (!result.Success)
            {
                var errorReplacements = new StringDictionary
                {
                    { "Error", result.ErrorMessage }
                };
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.ServiceFailed, errorReplacements));
                return await sc.EndDialogAsync();
            }

            if (result.Knowledges == null || result.Knowledges.Length == 0)
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(KnowledgeResponses.KnowledgeShowNone));
                return await sc.NextAsync();
            }
            else
            {
                var cards = new List<Card>();
                foreach (var knowledge in result.Knowledges)
                {
                    cards.Add(new Card()
                    {
                        Name = GetDivergedCardName(sc.Context, "Knowledge"),
                        Data = ConvertKnowledge(knowledge)
                    });
                }

                await sc.Context.SendActivityAsync(ResponseManager.GetCardResponse(KnowledgeResponses.KnowledgeShow, cards));
                return await sc.NextAsync();
            }
        }
    }
}
