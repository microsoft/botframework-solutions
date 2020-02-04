using System;
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
    public class PeersDialog : WhoSkillDialogBase
    {
        public PeersDialog(
                BotSettings settings,
                ConversationState conversationState,
                MSGraphService msGraphService,
                LocaleTemplateEngineManager localeTemplateEngineManager,
                IBotTelemetryClient telemetryClient,
                MicrosoftAppCredentials appCredentials)
            : base(nameof(PeersDialog), settings, conversationState, msGraphService, localeTemplateEngineManager, telemetryClient, appCredentials)
        {
           // AddDialog(new WhoIsDialog(settings, conversationState, msGraphService, localeTemplateEngineManager, telemetryClient, appCredentials));
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

            // If this person don't have manager, or he is the only one reports to manager,
            // will send a NoPeers reply.
            string templateName = string.Empty;
            var manager = await MSGraphService.GetManager(state.PickedPerson.Id);
            if (manager != null)
            {
                var directReports = await MSGraphService.GetDirectReports(manager.Id);
                if (directReports == null || directReports.Count() == 1)
                {
                    templateName = OrgResponses.NoPeers;
                }
                else
                {
                    templateName = OrgResponses.Peers;
                    state.PageIndex = 0;
                    state.Results = directReports.Where(x => x.Id != state.PickedPerson.Id).ToList();
                }
            }
            else
            {
                templateName = OrgResponses.NoPeers;
            }

            var data = new
            {
                TargetName = state.Keyword,
            };
            if (templateName == OrgResponses.NoPeers)
            {
                if (state.SearchCurrentUser)
                {
                    templateName = OrgResponses.MyNoPeers;
                }

                var reply = TemplateEngine.GenerateActivityForLocale(templateName, new { Person = data });
                await sc.Context.SendActivityAsync(reply);
                return await sc.EndDialogAsync();
            }
            else
            {
                if (state.SearchCurrentUser)
                {
                    templateName = OrgResponses.MyPeers;
                }

                var reply = TemplateEngine.GenerateActivityForLocale(templateName, new { Person = data, Number = state.Results.Count });
                await sc.Context.SendActivityAsync(reply);

                var persons = state.Results.Skip(state.PageIndex * state.PageSize).Take(state.PageSize).ToList();
                var card = await GetCardForPage(persons);
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = card });
            }
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