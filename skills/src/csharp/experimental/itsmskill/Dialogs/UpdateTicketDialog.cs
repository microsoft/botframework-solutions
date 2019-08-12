// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Responses.Ticket;
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
                CheckId,
                InputId,
                SetId,
                UpdateAttributeLoop,
                GetAuthToken,
                AfterGetAuthToken,
                UpdateTicket
            };

            var updateAttribute = new WaterfallStep[]
            {
                CheckAttributeNoConfirm,
                SetAttribute,
                UpdateSelectedAttribute,
                UpdateMore,
                AfterUpdateMore
            };

            AddDialog(new WaterfallDialog(Actions.UpdateTicket, updateTicket) { TelemetryClient = telemetryClient });

            AddDialog(new WaterfallDialog(Actions.UpdateAttribute, updateAttribute) { TelemetryClient = telemetryClient });

            InitialDialogId = Actions.UpdateTicket;
        }

        protected async Task<DialogTurnResult> UpdateAttributeLoop(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Actions.UpdateAttribute);
        }

        protected async Task<DialogTurnResult> UpdateSelectedAttribute(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            if (state.AttributeType == AttributeType.Description)
            {
                return await sc.BeginDialogAsync(Actions.SetDescription);
            }
            else if (state.AttributeType == AttributeType.Urgency)
            {
                return await sc.BeginDialogAsync(Actions.SetUrgency);
            }
            else
            {
                throw new Exception($"Invalid AttributeType: {state.AttributeType}");
            }
        }

        protected async Task<DialogTurnResult> UpdateMore(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var options = new PromptOptions()
            {
                Prompt = ResponseManager.GetResponse(SharedResponses.InputAttributeMore)
            };

            return await sc.PromptAsync(Actions.UpdateAttributeHasYesNo, options);
        }

        protected async Task<DialogTurnResult> AfterUpdateMore(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (sc.Result == null)
            {
                return await sc.NextAsync();
            }

            var type = (AttributeType)sc.Result;
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            state.AttributeType = type;

            if (state.AttributeType == AttributeType.Description)
            {
                state.TicketDescription = null;
            }
            else if (state.AttributeType == AttributeType.Urgency)
            {
                state.UrgencyLevel = UrgencyLevel.None;
            }
            else if (state.AttributeType == AttributeType.None)
            {
            }
            else
            {
                throw new Exception($"Invalid AttributeType: {state.AttributeType}");
            }

            return await sc.ReplaceDialogAsync(Actions.UpdateAttribute);
        }

        protected async Task<DialogTurnResult> UpdateTicket(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            if (state.Token == null)
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.AuthFailed));
                return await sc.EndDialogAsync();
            }

            var management = ServiceManager.CreateManagement(Settings, state.Token);
            var result = await management.UpdateTicket(state.Id, state.TicketDescription, state.UrgencyLevel);

            if (!result.Success)
            {
                var errorReplacements = new StringDictionary
                {
                    { "Error", result.ErrorMessage }
                };
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.ServiceFailed, errorReplacements));
                return await sc.EndDialogAsync();
            }

            var card = new Card()
            {
                Name = GetDivergedCardName(sc.Context, "Ticket"),
                Data = ConvertTicket(result.Tickets[0])
            };

            await sc.Context.SendActivityAsync(ResponseManager.GetCardResponse(TicketResponses.TicketUpdated, card, null));
            return await sc.NextAsync();
        }
    }
}
