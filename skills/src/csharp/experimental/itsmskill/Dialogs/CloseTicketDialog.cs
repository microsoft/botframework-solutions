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
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

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
                CheckId,
                InputId,
                SetId,
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

        protected async Task<DialogTurnResult> CloseTicket(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            var management = ServiceManager.CreateManagement(Settings, state.Token);
            var result = await management.CloseTicket(id: state.Id, reason: state.CloseReason);

            if (!result.Success)
            {
                var errorReplacements = new StringDictionary
                {
                    { "Error", result.ErrorMessage }
                };
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.ServiceFailed, errorReplacements));
                return await sc.CancelAllDialogsAsync();
            }

            var card = new Card()
            {
                Name = GetDivergedCardName(sc.Context, "Ticket"),
                Data = ConvertTicket(result.Tickets[0])
            };

            await sc.Context.SendActivityAsync(ResponseManager.GetCardResponse(TicketResponses.TicketClosed, card, null));
            return await sc.NextAsync();
        }
    }
}
