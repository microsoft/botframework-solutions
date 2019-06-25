﻿using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using NewsSkill.Models;
using NewsSkill.Responses.FindArticles;
using NewsSkill.Services;

namespace NewsSkill.Dialogs
{
    public class FindArticlesDialog : NewsDialogBase
    {
        private NewsClient _client;
        private FindArticlesResponses _responder = new FindArticlesResponses();
        private AzureMapsService _mapsService;

        public FindArticlesDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            UserState userState,
            AzureMapsService mapsService,
            IBotTelemetryClient telemetryClient)
            : base(nameof(FindArticlesDialog), services, conversationState, userState, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var newsKey = settings.Properties["BingNewsKey"] ?? throw new Exception("The BingNewsKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");
            var mapsKey = settings.Properties["AzureMapsKey"] ?? throw new Exception("The AzureMapsKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");

            _client = new NewsClient(newsKey);
            _mapsService = mapsService;
            _mapsService.InitKeyAsync(mapsKey);

            var findArticles = new WaterfallStep[]
            {
                GetMarket,
                SetMarket,
                GetQuery,
                GetSite,
                ShowArticles,
            };

            AddDialog(new WaterfallDialog(nameof(FindArticlesDialog), findArticles));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
        }

        private async Task<DialogTurnResult> GetMarket(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserAccessor.GetAsync(sc.Context, () => new NewsSkillUserState());

            // Check if there's already a location
            if (userState.Market != null)
            {
                if (userState.Market.Length > 0)
                {
                    return await sc.NextAsync(userState.Market);
                }
            }

            // Prompt user for location
            return await sc.PromptAsync(nameof(TextPrompt), new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(sc.Context, sc.Context.Activity.Locale, FindArticlesResponses.MarketPrompt)
            });
        }

        private async Task<DialogTurnResult> SetMarket(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserAccessor.GetAsync(sc.Context, () => new NewsSkillUserState());

            if (userState.Market == null)
            {
                string country = (string)sc.Result;

                // use AzureMaps API to get country code from country input by user
               userState.Market = await _mapsService.GetCountryCodeAsync(country);
            }

            return await sc.NextAsync();
        }

        private async Task<DialogTurnResult> GetQuery(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await ConvAccessor.GetAsync(sc.Context, () => new NewsSkillState());

            // Let's see if we have a topic
            if (convState.LuisResult.Entities.topic != null)
            {
                // If we have a topic let's skip the topic prompt
                if (convState.LuisResult.Entities.topic.Length > 0)
                {
                    return await sc.NextAsync(convState.LuisResult.Entities.topic[0]);
                }
            }

            return await sc.PromptAsync(nameof(TextPrompt), new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(sc.Context, sc.Context.Activity.Locale, FindArticlesResponses.TopicPrompt)
            });
        }

        private async Task<DialogTurnResult> GetSite(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await ConvAccessor.GetAsync(sc.Context, () => new NewsSkillState());

            string query = (string)sc.Result;

            // if site specified in luis, add to query
            if (convState.LuisResult.Entities.site != null)
            {
                if (convState.LuisResult.Entities.site.Length > 0)
                {
                    string site = convState.LuisResult.Entities.site[0].Replace(" ", string.Empty);
                    query = string.Concat(query, $" site:{site}");
                }
            }

            return await sc.NextAsync(query);
        }

        private async Task<DialogTurnResult> ShowArticles(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserAccessor.GetAsync(sc.Context, () => new NewsSkillUserState());

            var query = (string)sc.Result;

            var articles = await _client.GetNewsForTopic(query, userState.Market);
            await _responder.ReplyWith(sc.Context, FindArticlesResponses.ShowArticles, articles);

            return await sc.EndDialogAsync();
        }
    }
}