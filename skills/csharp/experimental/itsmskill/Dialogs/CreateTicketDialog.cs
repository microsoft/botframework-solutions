// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using ITSMSkill.Dialogs.Teams;
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
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;

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
                DisplayExistingLoop,
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

            var createTicketTaskModule = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                CreateTicketTeamsTaskModuleStep
            };

            var displayExisting = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ShowKnowledge,
                IfKnowledgeHelp
            };

            AddDialog(new WaterfallDialog(Actions.CreateTicket, createTicket));
            AddDialog(new WaterfallDialog(Actions.DisplayExisting, displayExisting));
            AddDialog(new WaterfallDialog(Actions.CreateTickTeamsTaskModule, createTicketTaskModule));

            InitialDialogId = Actions.CreateTicket;

            // intended null
            // ShowKnowledgeNoResponse
            ShowKnowledgeHasResponse = KnowledgeResponses.ShowExistingToSolve;
            ShowKnowledgeEndResponse = KnowledgeResponses.KnowledgeEnd;
            ShowKnowledgeResponse = KnowledgeResponses.IfExistingSolve;
            ShowKnowledgePrompt = Actions.NavigateYesNoPrompt;
            KnowledgeHelpLoop = Actions.DisplayExisting;
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default)
        {
            if (dc.Context.Activity.ChannelId.Contains("msteams"))
            {
                return await dc.BeginDialogAsync(Actions.CreateTickTeamsTaskModule, options, cancellationToken);
            }

            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
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

        protected async Task<DialogTurnResult> CreateTicketTeamsTaskModuleStep(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var reply = sc.Context.Activity.CreateReply();

            var adaptiveCard = TicketDialogHelper.ServiceNowTickHubAdaptiveCard();
            reply = sc.Context.Activity.CreateReply();
            reply.Attachments = new List<Attachment>()
            {
                new Microsoft.Bot.Schema.Attachment() { ContentType = AdaptiveCard.ContentType, Content = adaptiveCard }
            };

            await sc.Context.SendActivityAsync(reply, cancellationToken);
            return await sc.EndDialogAsync();
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
            return await sc.NextAsync();
        }
    }
}
