// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Models.ServiceNow;
using ITSMSkill.Prompts;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Responses.Ticket;
using ITSMSkill.Services;
using ITSMSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace ITSMSkill.Dialogs
{
    public class UpdateTicketDialog : SkillDialogBase
    {
        public UpdateTicketDialog(
             BotSettings settings,
             BotServices services,
             ResponseManager responseManager,
             ConversationState conversationState,
             IServiceManager serviceManager,
             IBotTelemetryClient telemetryClient)
            : base(nameof(UpdateTicketDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient)
        {
            var updateTicket = new WaterfallStep[]
            {
                BeginSetNumberThenId,
                UpdateAttributeLoop,
                GetAuthToken,
                AfterGetAuthToken,
                UpdateTicket
            };

            var updateAttribute = new WaterfallStep[]
            {
                ShowUpdates,
                CheckAttribute,
                InputAttribute,
                SetAttribute,
                UpdateSelectedAttribute,
                UpdateLoop
            };

            var attributesForUpdate = new AttributeType[] { AttributeType.Title, AttributeType.Description, AttributeType.Urgency };

            AddDialog(new WaterfallDialog(Actions.UpdateTicket, updateTicket) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateAttribute, updateAttribute) { TelemetryClient = telemetryClient });
            AddDialog(new AttributeWithNoPrompt(Actions.UpdateAttributePrompt, attributesForUpdate));

            InitialDialogId = Actions.UpdateTicket;

            ConfirmAttributeResponse = TicketResponses.ConfirmUpdateAttribute;
            InputAttributeResponse = TicketResponses.UpdateAttribute;
            InputAttributePrompt = Actions.UpdateAttributePrompt;
        }

        protected async Task<DialogTurnResult> UpdateAttributeLoop(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Actions.UpdateAttribute);
        }

        protected async Task<DialogTurnResult> ShowUpdates(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(state.TicketTitle))
            {
                sb.AppendLine(string.Format(SharedStrings.Title, state.TicketTitle));
            }

            if (!string.IsNullOrEmpty(state.TicketDescription))
            {
                sb.AppendLine(string.Format(SharedStrings.Description, state.TicketDescription));
            }

            if (state.UrgencyLevel != UrgencyLevel.None)
            {
                sb.AppendLine(string.Format(SharedStrings.Urgency, state.UrgencyLevel.ToLocalizedString()));
            }

            if (sb.Length == 0)
            {
                // await sc.Context.SendActivityAsync(ResponseManager.GetResponse(TicketResponses.ShowUpdateNone));
            }
            else
            {
                var token = new StringDictionary()
                {
                    { "Attributes", sb.ToString() }
                };
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(TicketResponses.ShowUpdates, token));
            }

            return await sc.NextAsync();
        }

        protected async Task<DialogTurnResult> UpdateLoop(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());

            // state.AttributeType from Luis should be used first
            state.AttributeType = AttributeType.None;

            return await sc.ReplaceDialogAsync(Actions.UpdateAttribute);
        }

        protected async Task<DialogTurnResult> UpdateTicket(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());

            if (string.IsNullOrEmpty(state.TicketTitle) && string.IsNullOrEmpty(state.TicketDescription) && state.UrgencyLevel == UrgencyLevel.None)
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(TicketResponses.TicketNoUpdate));
                return await sc.NextAsync();
            }

            var management = ServiceManager.CreateManagement(Settings, sc.Result as TokenResponse, state.ServiceCache);
            var result = await management.UpdateTicket(state.Id, state.TicketTitle, state.TicketDescription, state.UrgencyLevel);

            if (!result.Success)
            {
                return await SendServiceErrorAndCancel(sc, result);
            }

            var card = GetTicketCard(sc.Context, result.Tickets[0]);

            await sc.Context.SendActivityAsync(ResponseManager.GetCardResponse(TicketResponses.TicketUpdated, card, null));
            return await sc.NextAsync();
        }
    }
}
