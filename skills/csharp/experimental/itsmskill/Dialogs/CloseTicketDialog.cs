// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Prompts;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Responses.Ticket;
using ITSMSkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;

namespace ITSMSkill.Dialogs
{
    public class CloseTicketDialog : SkillDialogBase
    {
        public CloseTicketDialog(
             BotSettings settings,
             BotServices services,
             ResponseManager responseManager,
             ConversationState conversationState,
             IServiceManager serviceManager,
             IBotTelemetryClient telemetryClient)
            : base(nameof(CloseTicketDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient)
        {
            var closeTicket = new WaterfallStep[]
            {
                BeginSetNumberThenId,
                CheckClosed,
                CheckReason,
                InputReason,
                SetReason,
                GetAuthToken,
                AfterGetAuthToken,
                CloseTicket
            };

            AddDialog(new WaterfallDialog(Actions.CloseTicket, closeTicket));

            InitialDialogId = Actions.CloseTicket;
        }

        protected async Task<DialogTurnResult> CheckClosed(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());

            if (state.TicketTarget.State == TicketState.Closed)
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(TicketResponses.TicketAlreadyClosed));
                return await sc.EndDialogAsync();
            }

            return await sc.NextAsync();
        }

        protected async Task<DialogTurnResult> CloseTicket(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            var management = ServiceManager.CreateManagement(Settings, sc.Result as TokenResponse, state.ServiceCache);
            var result = await management.CloseTicket(id: state.Id, reason: state.CloseReason);

            if (!result.Success)
            {
                return await SendServiceErrorAndCancel(sc, result);
            }

            var card = GetTicketCard(sc.Context, result.Tickets[0]);

            await sc.Context.SendActivityAsync(ResponseManager.GetCardResponse(TicketResponses.TicketClosed, card, null));
            return await sc.NextAsync();
        }
    }
}
