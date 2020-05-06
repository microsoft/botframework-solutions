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
using ITSMSkill.Responses.Ticket;
using ITSMSkill.Services;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;

namespace ITSMSkill.Dialogs
{
    public class CreateTicketDialog : SkillDialogBase
    {
        public CreateTicketDialog(
             BotSettings settings,
             BotServices services,
             ResponseManager responseManager,
             ConversationState conversationState,
             IServiceManager serviceManager,
             IBotTelemetryClient telemetryClient)
            : base(nameof(CreateTicketDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient)
        {
            var createTicket = new WaterfallStep[]
            {
                CheckTitle,
                InputTitle,
                SetTitle,
                //DisplayExistingLoop,
                CheckDescription,
                InputDescription,
                SetDescription,
                CheckUrgency,
                InputUrgency,
                SetUrgency,
                GetAuthToken,
                AfterGetAuthToken,
                CreateTicket
            };

            var displayExisting = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ShowKnowledge,
                IfKnowledgeHelp
            };

            var updateToken = new WaterfallStep[]
            {
                Relogin,
                AfterGetAuthToken
            };

            AddDialog(new WaterfallDialog(Actions.CreateTicket, createTicket));
            AddDialog(new WaterfallDialog(Actions.DisplayExisting, displayExisting));
            AddDialog(new WaterfallDialog(Actions.UpdateToken, updateToken));

            InitialDialogId = Actions.CreateTicket;

            // intended null
            // ShowKnowledgeNoResponse
            ShowKnowledgeHasResponse = KnowledgeResponses.ShowExistingToSolve;
            ShowKnowledgeEndResponse = KnowledgeResponses.KnowledgeEnd;
            ShowKnowledgeResponse = KnowledgeResponses.IfExistingSolve;
            ShowKnowledgePrompt = Actions.NavigateYesNoPrompt;
            KnowledgeHelpLoop = Actions.DisplayExisting;
        }

        protected async Task<DialogTurnResult> DisplayExistingLoop(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());

            if (state.DisplayExisting)
            {
                state.PageIndex = -1;
                return await sc.BeginDialogAsync(Actions.DisplayExisting);
            }
            else
            {
                return await sc.NextAsync();
            }
        }

        protected async Task<DialogTurnResult> CreateTicket(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            var management = ServiceManager.CreateManagement(Settings, sc.Result as TokenResponse, state.ServiceCache);
            var result = await management.CreateTicket(state.TicketTitle, state.TicketDescription, state.UrgencyLevel);

            if (!result.Success)
            {
               return await SendServiceErrorAndCancel(sc, result);
            }

            var card = GetTicketCard(sc.Context, result.Tickets[0]);

            await sc.Context.SendActivityAsync(ResponseManager.GetCardResponse(TicketResponses.TicketCreated, card, null));
            await sc.ReplaceDialogAsync(Actions.UpdateToken);
            return await sc.NextAsync();
        }
    }
}
