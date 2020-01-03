using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using WhoSkill.Models;
using WhoSkill.Services;
using WhoSkill.Utilities;

namespace WhoSkill.Dialogs
{
    public class WhoIsDialog : WhoSkillDialogBase
    {
        public WhoIsDialog(
                BotSettings settings,
                ConversationState conversationState,
                MSGraphService msGraphService,
                LocaleTemplateEngineManager localeTemplateEngineManager,
                IBotTelemetryClient telemetryClient,
                MicrosoftAppCredentials appCredentials)
            : base(nameof(WhoIsDialog), settings, conversationState, msGraphService, localeTemplateEngineManager, telemetryClient, appCredentials)
        {
            var initDialog = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                InitService,
                ShowCandidates,
            };

            var showCandidates = new WaterfallStep[]
            {
                ShowCurrentPage,
                CollectUserChoice,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.InitDialog, initDialog) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowCandidates, showCandidates) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.InitDialog;
        }

        public async Task<DialogTurnResult> ShowCandidates(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.ShowCandidates);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // Send different reply according to skill state. But this step doesn't modify skill state.
        public async Task<DialogTurnResult> ShowCurrentPage(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await WhoStateAccessor.GetAsync(sc.Context);

                if (!state.AlreadySearched)
                {
                    if (string.IsNullOrEmpty(state.TargetName))
                    {
                        await sc.Context.SendActivityAsync("Please provide the name of the person you want to look up.");
                        return await sc.EndDialogAsync();
                    }
                    else
                    {
                        var users = await MSGraphService.GetUsers(state.TargetName);
                        state.Candidates = users;
                        state.AlreadySearched = true;
                    }
                }

                // Didn't find any candidate.
                if (state.Candidates == null || state.Candidates.Count == 0)
                {
                    await sc.Context.SendActivityAsync($"Sorry, I couldn’t find anyone named {state.TargetName}.");
                    await sc.EndDialogAsync();
                }

                if (state.Candidates.Count == 1)
                {
                    await sc.Context.SendActivityAsync($"Here's what I found for {state.TargetName}.");
                    var card = await GetCardForDetail(state.Candidates[0]);
                    await sc.Context.SendActivityAsync(card);
                    return await sc.EndDialogAsync();
                }
                else
                {
                    var replyMessage = $"I found multiple matches for {state.TargetName}. Please pick one. Or you can say 'next' or 'previous' to see more persons.";
                    await sc.Context.SendActivityAsync(replyMessage);

                    var candidates = state.Candidates.Skip(state.PageIndex * state.PageSize).Take(state.PageSize).ToList();
                    var card = await GetCardForPage(candidates);
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = card });
                }
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // Accept user's choice. Update skill state.
        public async Task<DialogTurnResult> CollectUserChoice(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await WhoStateAccessor.GetAsync(sc.Context);
            var luisResult = sc.Context.TurnState.Get<WhoLuis>(StateProperties.WhoLuisResultKey);
            var topIntent = luisResult.TopIntent().intent;
            var maxPageNumber = ((state.Candidates.Count - 1) / state.PageSize) + 1;

            if (state.Ordinal != int.MinValue)
            {
                // If user want to see someone's detail.
                var index = (state.PageIndex * state.PageSize) + state.Ordinal - 1;
                if (state.Ordinal > state.PageSize || state.Ordinal <= 0 || index >= state.Candidates.Count)
                {
                    await sc.Context.SendActivityAsync("Invalid number.");
                    state.Ordinal = int.MinValue;
                    return await sc.ReplaceDialogAsync(Actions.ShowCandidates);
                }

                var candidate = new List<Candidate>();
                candidate.Add(state.Candidates[index]);
                state.Candidates = candidate;
            }
            else if (topIntent == WhoLuis.Intent.ShowNextPage)
            {
                // Else if user want to see next page.
                if (state.PageIndex < maxPageNumber - 1)
                {
                    state.PageIndex++;
                }
                else
                {
                    await sc.Context.SendActivityAsync("Already last page.");
                }
            }
            else if (topIntent == WhoLuis.Intent.ShowPreviousPage)
            {
                // Else if user want to see previous page.
                if (state.PageIndex > 0)
                {
                    state.PageIndex--;
                }
                else
                {
                    await sc.Context.SendActivityAsync("Already first page.");
                }
            }
            else
            {
                await sc.Context.SendActivityAsync("Sorry, I don't understand. You can say cancel to start over.");
            }

            return await sc.ReplaceDialogAsync(Actions.ShowCandidates);
        }
    }
}
