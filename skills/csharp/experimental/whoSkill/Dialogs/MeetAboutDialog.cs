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
using WhoSkill.Models;
using WhoSkill.Responses.MeetAbout;
using WhoSkill.Responses.Shared;
using WhoSkill.Responses.WhoIs;
using WhoSkill.Services;
using WhoSkill.Utilities;

namespace WhoSkill.Dialogs
{
    public class MeetAboutDialog : WhoSkillDialogBase
    {
        public MeetAboutDialog(
                BotSettings settings,
                ConversationState conversationState,
                MSGraphService msGraphService,
                LocaleTemplateEngineManager localeTemplateEngineManager,
                IBotTelemetryClient telemetryClient,
                MicrosoftAppCredentials appCredentials)
            : base(nameof(MeetAboutDialog), settings, conversationState, msGraphService, localeTemplateEngineManager, telemetryClient, appCredentials)
        {
        }

        protected override async Task<DialogTurnResult> SearchKeyword(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await WhoStateAccessor.GetAsync(sc.Context);

            // This dialog don't need search candidates first, because it searchs current user's meeting.
            state.Candidates = new List<Candidate> { new Candidate() };
            return await sc.NextAsync();
        }

        protected override async Task<DialogTurnResult> DisplayResult(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await WhoStateAccessor.GetAsync(sc.Context);
            if (state.Results == null)
            {
                List<Candidate> results = null;
                if (string.IsNullOrEmpty(state.Keyword))
                {
                }
                else
                {
                    results = await MSGraphService.GetMeetingContacts(state.Keyword);
                }

                if (results == null)
                {
                    var activity = TemplateEngine.GenerateActivityForLocale(MeetAboutResponses.NoPeopleMeetsAboutKeywordRencently, new { Keyword = state.Keyword });
                    await sc.Context.SendActivityAsync(activity);
                    return await sc.EndDialogAsync();
                }
                else
                {
                    state.Results = results;
                }
            }

            if (string.IsNullOrEmpty(state.Keyword))
            {
                var reply = TemplateEngine.GenerateActivityForLocale(
                    MeetAboutResponses.PeopleMeetsRencently,
                    new
                    {
                        Number = state.Results.Count
                    });
                await sc.Context.SendActivityAsync(reply);
            }
            else
            {
                var reply = TemplateEngine.GenerateActivityForLocale(
                    MeetAboutResponses.PeopleMeetsAboutKeywordRencently,
                    new
                    {
                        Keyword = state.Keyword,
                        Number = state.Results.Count
                    });
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