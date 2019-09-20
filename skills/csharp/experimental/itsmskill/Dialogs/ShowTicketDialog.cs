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
using ITSMSkill.Models.ServiceNow;
using ITSMSkill.Prompts;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Responses.Ticket;
using ITSMSkill.Services;
using ITSMSkill.Utilities;
using Luis;
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
                ShowConstraints,
                BeginShowTicketLoop,
                BeginShowAttributeLoop,
                LoopShowTicket,
            };

            var showAttribute = new WaterfallStep[]
            {
                CheckAttribute,
                InputAttribute,
                SetAttribute,
                UpdateSelectedAttribute,
                LoopShowAttribute,
            };

            var showTicketLoop = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ShowTicket,
                IfContinueShow
            };

            var attributesForShow = new AttributeType[] { AttributeType.Number, AttributeType.Description, AttributeType.Urgency, AttributeType.State };

            AddDialog(new WaterfallDialog(Actions.ShowTicket, showTicket) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowAttribute, showAttribute) { TelemetryClient = telemetryClient });
            AddDialog(new AttributeWithNoPrompt(Actions.ShowAttributePrompt, attributesForShow));
            AddDialog(new WaterfallDialog(Actions.ShowTicketLoop, showTicketLoop) { TelemetryClient = telemetryClient });

            InitialDialogId = Actions.ShowTicket;

            // never used
            // ConfirmAttributeResponse
            InputAttributeResponse = TicketResponses.ShowAttribute;
            InputAttributePrompt = Actions.ShowAttributePrompt;
        }

        protected async Task<DialogTurnResult> BeginShowAttributeLoop(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Actions.ShowAttribute);
        }

        protected async Task<DialogTurnResult> BeginShowTicketLoop(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            state.PageIndex = -1;

            return await sc.BeginDialogAsync(Actions.ShowTicketLoop);
        }

        protected async Task<DialogTurnResult> ShowConstraints(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());

            // always prompt for search
            state.AttributeType = AttributeType.None;

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(state.TicketNumber))
            {
                sb.AppendLine($"{SharedStrings.TicketNumber}{state.TicketNumber}");
            }

            if (!string.IsNullOrEmpty(state.TicketDescription))
            {
                sb.AppendLine($"{SharedStrings.Description}{state.TicketDescription}");
            }

            if (state.UrgencyLevel != UrgencyLevel.None)
            {
                sb.AppendLine($"{SharedStrings.Urgency}{state.UrgencyLevel.ToLocalizedString()}");
            }

            if (state.TicketState != TicketState.None)
            {
                sb.AppendLine($"{SharedStrings.TicketState}{state.TicketState.ToLocalizedString()}");
            }

            if (sb.Length == 0)
            {
                // await sc.Context.SendActivityAsync(ResponseManager.GetResponse(TicketResponses.ShowConstraintNone));
            }
            else
            {
                var token = new StringDictionary()
                {
                    { "Attributes", sb.ToString() }
                };

                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(TicketResponses.ShowConstraints, token));
            }

            return await sc.NextAsync();
        }

        protected async Task<DialogTurnResult> LoopShowTicket(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.ReplaceDialogAsync(Actions.ShowTicket);
        }

        protected async Task<DialogTurnResult> LoopShowAttribute(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.ReplaceDialogAsync(Actions.ShowAttribute);
        }

        protected async Task<DialogTurnResult> ShowTicket(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());

            bool firstDisplay = false;
            if (state.PageIndex == -1)
            {
                firstDisplay = true;
                state.PageIndex = 0;
            }

            var management = ServiceManager.CreateManagement(Settings, state.Token);

            var urgencies = new List<UrgencyLevel>();
            if (state.UrgencyLevel != UrgencyLevel.None)
            {
                urgencies.Add(state.UrgencyLevel);
            }

            var states = new List<TicketState>();
            if (state.TicketState != TicketState.None)
            {
                states.Add(state.TicketState);
            }

            var countResult = await management.CountTicket(description: state.TicketDescription, urgencies: urgencies, number: state.TicketNumber, states: states);

            if (!countResult.Success)
            {
                return await SendServiceErrorAndCancel(sc, countResult);
            }

            // adjust PageIndex
            int maxPage = Math.Max(0, (countResult.Tickets.Length - 1) / Settings.LimitSize);
            state.PageIndex = Math.Max(0, Math.Min(state.PageIndex, maxPage));

            // TODO handle consistency with count
            var result = await management.SearchTicket(state.PageIndex, description: state.TicketDescription, urgencies: urgencies, number: state.TicketNumber, states: states);

            if (!result.Success)
            {
                return await SendServiceErrorAndCancel(sc, result);
            }

            if (result.Tickets == null || result.Tickets.Length == 0)
            {
                if (firstDisplay)
                {
                    var options = new PromptOptions()
                    {
                        Prompt = ResponseManager.GetResponse(TicketResponses.TicketShowNone)
                    };

                    return await sc.PromptAsync(Actions.NavigateYesNoPrompt, options);
                }
                else
                {
                    // it is unlikely to happen now
                    var token = new StringDictionary()
                    {
                        { "Page", (state.PageIndex + 1).ToString() }
                    };

                    var options = new PromptOptions()
                    {
                        Prompt = ResponseManager.GetResponse(TicketResponses.TicketEnd, token)
                    };

                    return await sc.PromptAsync(Actions.NavigateYesNoPrompt, options);
                }
            }
            else
            {
                var cards = new List<Card>();
                foreach (var ticket in result.Tickets)
                {
                    cards.Add(GetTicketCard(sc.Context, ticket));
                }

                var token = new StringDictionary()
                {
                    { "Page", $"{state.PageIndex + 1}/{maxPage + 1}" },
                    { "Navigate", GetNavigateString(state.PageIndex, maxPage) },
                };

                var options = new PromptOptions()
                {
                    Prompt = ResponseManager.GetCardResponse(TicketResponses.TicketShow, cards, token)
                };

                // Workaround. In teams, HeroCard will be used for prompt and adaptive card could not be shown. So send them separatly
                if (Channel.GetChannelId(sc.Context) == Channels.Msteams)
                {
                    await sc.Context.SendActivityAsync(options.Prompt);
                    options.Prompt = null;
                }

                return await sc.PromptAsync(Actions.NavigateYesNoPrompt, options);
            }
        }

        protected async Task<DialogTurnResult> IfContinueShow(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var intent = (GeneralLuis.Intent)sc.Result;
            if (intent == GeneralLuis.Intent.Reject)
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.ActionEnded));
                return await sc.CancelAllDialogsAsync();
            }
            else if (intent == GeneralLuis.Intent.Confirm)
            {
                return await sc.EndDialogAsync();
            }
            else if (intent == GeneralLuis.Intent.ShowNext)
            {
                var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
                state.PageIndex += 1;
                return await sc.ReplaceDialogAsync(Actions.ShowTicketLoop);
            }
            else if (intent == GeneralLuis.Intent.ShowPrevious)
            {
                var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
                state.PageIndex = Math.Max(0, state.PageIndex - 1);
                return await sc.ReplaceDialogAsync(Actions.ShowTicketLoop);
            }
            else
            {
                throw new Exception($"Invalid GeneralLuis.Intent ${intent}");
            }
        }
    }
}
