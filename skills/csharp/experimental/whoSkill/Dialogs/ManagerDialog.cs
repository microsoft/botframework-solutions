using System;
using System.Collections.Generic;
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
    public class ManagerDialog : WhoSkillDialogBase
    {
        public ManagerDialog(
                BotSettings settings,
                ConversationState conversationState,
                MSGraphService msGraphService,
                LocaleTemplateEngineManager localeTemplateEngineManager,
                IBotTelemetryClient telemetryClient,
                MicrosoftAppCredentials appCredentials)
            : base(nameof(ManagerDialog), settings, conversationState, msGraphService, localeTemplateEngineManager, telemetryClient, appCredentials)
        {
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
            var manager = await MSGraphService.GetManager(state.PickedPerson.Id);
            if (manager != null)
            {
                state.Results = new List<Candidate>() { manager };

                if (state.SearchCurrentUser)
                {
                    var reply = TemplateEngine.GenerateActivityForLocale(OrgResponses.MyManager);
                    await sc.Context.SendActivityAsync(reply);
                }
                else
                {
                    var reply = TemplateEngine.GenerateActivityForLocale(OrgResponses.Manager, new { Person = data });
                    await sc.Context.SendActivityAsync(reply);
                }

                var card = await GetCardForDetail(state.Results[0]);
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = card });
            }
            else
            {
                if (state.SearchCurrentUser)
                {
                    var reply = TemplateEngine.GenerateActivityForLocale(OrgResponses.MyNoManager);
                    await sc.Context.SendActivityAsync(reply);
                }
                else
                {
                    var activity = TemplateEngine.GenerateActivityForLocale(OrgResponses.NoManager, new { Person = data });
                    await sc.Context.SendActivityAsync(activity);
                }

                return await sc.EndDialogAsync();
            }
        }

        protected override async Task<DialogTurnResult> CollectUserChoiceAfterResult(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await WhoStateAccessor.GetAsync(sc.Context);
            var luisResult = sc.Context.TurnState.Get<WhoLuis>(StateProperties.WhoLuisResultKey);
            var topIntent = luisResult.TopIntent().intent;

            switch (topIntent)
            {
                case WhoLuis.Intent.Manager:
                    {
                        var keyword = state.Results[0].Mail;
                        state.Init();
                        state.Keyword = keyword;
                        state.TriggerIntent = WhoLuis.Intent.Manager;
                        return await sc.ReplaceDialogAsync(Actions.SearchKeyword);
                    }

                default:
                    {
                        var didntUnderstandActivity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.DidntUnderstandMessage);
                        await sc.Context.SendActivityAsync(didntUnderstandActivity);
                        return await sc.EndDialogAsync();
                    }
            }
        }
    }
}