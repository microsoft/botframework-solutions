﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.DependencyInjection;
using WhoSkill.Models;
using WhoSkill.Responses.Org;
using WhoSkill.Responses.Shared;
using WhoSkill.Responses.WhoIs;
using WhoSkill.Services;
using WhoSkill.Utilities;

namespace WhoSkill.Dialogs
{
    public class DirectReportsDialog : WhoSkillDialogBase
    {
        public DirectReportsDialog(
                BotSettings settings,
                ConversationState conversationState,
                MSGraphService msGraphService,
                LocaleTemplateEngineManager localeTemplateEngineManager,
                IBotTelemetryClient telemetryClient,
                MicrosoftAppCredentials appCredentials)
            : base(nameof(DirectReportsDialog), settings, conversationState, msGraphService, localeTemplateEngineManager, telemetryClient, appCredentials)
        {
            AddDialog(new WhoIsDialog(settings, conversationState, msGraphService, localeTemplateEngineManager, telemetryClient, appCredentials));
        }

        protected override async Task<DialogTurnResult> DisplayResult(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await WhoStateAccessor.GetAsync(sc.Context);
            if (state.PickedPerson == null)
            {
                var activity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.NoSearchResult);
                await sc.Context.SendActivityAsync(activity);
                return await sc.EndDialogAsync();
            }

            var data = new
            {
                TargetName = state.Keyword,
            };

            // Search the picked person's direct reports.
            if (state.Results == null)
            {
                var directReports = await MSGraphService.GetDirectReports(state.PickedPerson.Id);
                if (directReports == null || !directReports.Any())
                {
                    if (state.SearchCurrentUser)
                    {
                        var activity = TemplateEngine.GenerateActivityForLocale(OrgResponses.MyNoDirectReports);
                        await sc.Context.SendActivityAsync(activity);
                    }
                    else
                    {
                        var activity = TemplateEngine.GenerateActivityForLocale(OrgResponses.NoDirectReports, new { Person = data });
                        await sc.Context.SendActivityAsync(activity);
                    }

                    return await sc.EndDialogAsync();
                }
                else
                {
                    state.PageIndex = 0;
                    state.Results = directReports;
                }
            }

            if (state.SearchCurrentUser)
            {
                var reply = TemplateEngine.GenerateActivityForLocale(OrgResponses.MyDirectReports, new { Number = state.Results.Count });
                await sc.Context.SendActivityAsync(reply);
            }
            else
            {
                var reply = TemplateEngine.GenerateActivityForLocale(OrgResponses.DirectReports, new { Person = data, Number = state.Results.Count });
                await sc.Context.SendActivityAsync(reply);
            }

            var persons = state.Results.Skip(state.PageIndex * state.PageSize).Take(state.PageSize).ToList();
            var card = await GetCardForPage(persons);
            return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = card });
        }

        protected override async Task<DialogTurnResult> CollectUserChoiceAfterResult(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await WhoStateAccessor.GetAsync(sc.Context);
            var luisResult = sc.Context.TurnState.Get<WhoLuis>(StateProperties.WhoLuisResultKey);
            var topIntent = luisResult.TopIntent().intent;
            var maxPageNumber = ((state.Results.Count - 1) / state.PageSize) + 1;

            switch (topIntent)
            {
                case WhoLuis.Intent.ShowDetail:
                    {
                        // If user want to see someone's detail.
                        var index = (state.PageIndex * state.PageSize) + state.Ordinal - 1;
                        if (state.Ordinal > state.PageSize || state.Ordinal <= 0 || index >= state.Results.Count)
                        {
                            await sc.Context.SendActivityAsync("Invalid number.");
                            return await sc.ReplaceDialogAsync(Actions.DisplayResult);
                        }

                        var keyword = state.Results[index].Mail;
                        state.Init();
                        state.Keyword = keyword;
                        state.TriggerIntent = WhoLuis.Intent.WhoIs;
                        return await sc.BeginDialogAsync(nameof(WhoIsDialog));
                    }

                case WhoLuis.Intent.ShowNextPage:
                    {
                        // Else if user want to see next page.
                        if (state.PageIndex < maxPageNumber - 1)
                        {
                            state.PageIndex++;
                        }
                        else
                        {
                            var activity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.AlreadyLastPage);
                            await sc.Context.SendActivityAsync(activity);
                        }

                        return await sc.ReplaceDialogAsync(Actions.DisplayResult);
                    }

                case WhoLuis.Intent.ShowPreviousPage:
                    {
                        // Else if user want to see previous page.
                        if (state.PageIndex > 0)
                        {
                            state.PageIndex--;
                        }
                        else
                        {
                            var activity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.AlreadyFirstPage);
                            await sc.Context.SendActivityAsync(activity);
                        }

                        return await sc.ReplaceDialogAsync(Actions.DisplayResult);
                    }

                default:
                    {
                        var didntUnderstandActivity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.DidntUnderstandMessage);
                        await sc.Context.SendActivityAsync(didntUnderstandActivity);
                        return await sc.ReplaceDialogAsync(Actions.DisplayResult);
                    }
            }
        }
    }
}