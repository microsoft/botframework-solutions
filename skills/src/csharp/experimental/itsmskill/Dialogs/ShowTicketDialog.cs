// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
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
    public class ShowTicketDialog : SkillDialogBase
    {
        public ShowTicketDialog(
             BotSettings settings,
             BotServices services,
             ResponseManager responseManager,
             ConversationState conversationState,
             IServiceManager serviceManager,
             IBotTelemetryClient telemetryClient)
            : base(nameof(ShowTicketDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient)
        {
            var showTicket = new WaterfallStep[]
            {
                ShowAttributeLoop,
                GetAuthToken,
                AfterGetAuthToken,
                ShowTicket
            };

            var showAttribute = new WaterfallStep[]
            {
                ShowConstraints,
                UpdateMore,
                AfterUpdateMore,
                CheckAttributeNoConfirm,
                SetAttribute,
                UpdateSelectedAttribute
            };

            var attributesForShow = new AttributeType[] { AttributeType.Id, AttributeType.Description, AttributeType.Urgency };

            AddDialog(new WaterfallDialog(Actions.ShowTicket, showTicket) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowAttribute, showAttribute) { TelemetryClient = telemetryClient });
            AddDialog(new AttributePrompt(Actions.ShowAttributeNoYesNo, attributesForShow, false));
            AddDialog(new AttributePrompt(Actions.ShowAttributeHasYesNo, attributesForShow, true));

            InitialDialogId = Actions.ShowTicket;

            InputAttributeResponse = TicketResponses.ShowAttribute;
            InputAttributePrompt = Actions.ShowAttributeNoYesNo;
            InputMoreAttributeResponse = TicketResponses.ShowAttributeMore;
            InputMoreAttributePrompt = Actions.ShowAttributeHasYesNo;
        }

        protected async Task<DialogTurnResult> ShowAttributeLoop(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Actions.ShowAttribute);
        }

        protected async Task<DialogTurnResult> ShowConstraints(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(state.Id))
            {
                sb.AppendLine($"{SharedStrings.ID}{state.Id}");
            }

            if (!string.IsNullOrEmpty(state.TicketDescription))
            {
                sb.AppendLine($"{SharedStrings.Description}{state.TicketDescription}");
            }

            if (state.UrgencyLevel != UrgencyLevel.None)
            {
                sb.AppendLine($"{SharedStrings.Urgency}{ConvertUrgencyLevel(state.UrgencyLevel)}");
            }

            if (sb.Length == 0)
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(TicketResponses.ShowConstraintNone));
            }
            else
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(TicketResponses.ShowConstraints));
                await sc.Context.SendActivityAsync(sb.ToString());
            }

            return await sc.NextAsync();
        }

        protected async Task<DialogTurnResult> ShowTicket(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            if (state.Token == null)
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.AuthFailed));
                return await sc.EndDialogAsync();
            }

            var management = ServiceManager.CreateManagement(Settings, state.Token);
            var urgencies = new List<UrgencyLevel>();
            if (state.UrgencyLevel != UrgencyLevel.None)
            {
                urgencies.Add(state.UrgencyLevel);
            }

            var result = await management.SearchTicket(description: state.TicketDescription, urgencies: urgencies, id: state.Id);

            if (!result.Success)
            {
                var errorReplacements = new StringDictionary
                {
                    { "Error", result.ErrorMessage }
                };
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.ServiceFailed, errorReplacements));
                return await sc.EndDialogAsync();
            }

            if (result.Tickets == null || result.Tickets.Length == 0)
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(TicketResponses.TicketShowNone));
                return await sc.NextAsync();
            }
            else
            {
                var cards = new List<Card>();
                foreach (var ticket in result.Tickets)
                {
                    cards.Add(new Card()
                    {
                        Name = GetDivergedCardName(sc.Context, "Ticket"),
                        Data = ConvertTicket(ticket)
                    });
                }

                await sc.Context.SendActivityAsync(ResponseManager.GetCardResponse(TicketResponses.TicketShow, cards));
                return await sc.NextAsync();
            }
        }
    }
}
