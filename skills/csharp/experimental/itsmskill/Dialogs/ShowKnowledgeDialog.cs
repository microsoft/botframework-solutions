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
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Responses;
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
                CheckSearch,
                InputSearch,
                SetTitle,
                GetAuthToken,
                AfterGetAuthToken,
                ShowKnowledgeLoop,
                IfCreateTicket,
                AfterIfCreateTicket
            };

            var showKnowledgeLoop = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ShowKnowledge,
                IfKnowledgeHelp
            };

            AddDialog(new WaterfallDialog(Actions.ShowKnowledge, showKnowledge));
            AddDialog(new WaterfallDialog(Actions.ShowKnowledgeLoop, showKnowledgeLoop));

            InitialDialogId = Actions.ShowKnowledge;

            ShowKnowledgeNoResponse = KnowledgeResponses.KnowledgeShowNone;
            ShowKnowledgeEndResponse = KnowledgeResponses.KnowledgeEnd;
            ShowKnowledgeResponse = KnowledgeResponses.IfFindWanted;
            ShowKnowledgePrompt = Actions.NavigateYesNoPrompt;
            KnowledgeHelpLoop = Actions.ShowKnowledgeLoop;
        }

        protected async Task<DialogTurnResult> ShowKnowledgeLoop(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            state.PageIndex = -1;

            return await sc.BeginDialogAsync(Actions.ShowKnowledgeLoop);
        }

        protected async Task<DialogTurnResult> IfCreateTicket(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var options = new PromptOptions()
            {
                Prompt = ResponseManager.GetResponse(KnowledgeResponses.IfCreateTicket)
            };

            return await sc.PromptAsync(nameof(ConfirmPrompt), options);
        }

        protected async Task<DialogTurnResult> AfterIfCreateTicket(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if ((bool)sc.Result)
            {
                var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
                state.DisplayExisting = false;

                // note that it replaces the active WaterfallDialog instead of ShowKnowledgeDialog
                return await sc.ReplaceDialogAsync(nameof(CreateTicketDialog));
            }
            else
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.ActionEnded));
                return await sc.CancelAllDialogsAsync();
            }
        }
    }
}
