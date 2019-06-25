using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using NewsSkill.Models;
using NewsSkill.Responses.TrendingArticles;
using NewsSkill.Services;

namespace NewsSkill.Dialogs
{
    public class TrendingArticlesDialog : NewsDialogBase
    {
        private NewsClient _client;
        private TrendingArticlesResponses _responder = new TrendingArticlesResponses();
        private AzureMapsService _mapsService;

        public TrendingArticlesDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            UserState userState,
            AzureMapsService mapsService,
            IBotTelemetryClient telemetryClient)
            : base(nameof(TrendingArticlesDialog), services, conversationState, userState, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var key = settings.Properties["BingNewsKey"] ?? throw new Exception("The BingNewsKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");
            var mapsKey = settings.Properties["AzureMapsKey"] ?? throw new Exception("The AzureMapsKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");

            _client = new NewsClient(key);
            _mapsService = mapsService;
            _mapsService.InitKeyAsync(mapsKey);

            var trendingArticles = new WaterfallStep[]
            {
                GetMarket,
                SetMarket,
                ShowArticles,
            };

            AddDialog(new WaterfallDialog(nameof(TrendingArticlesDialog), trendingArticles));
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
                Prompt = await _responder.RenderTemplate(sc.Context, sc.Context.Activity.Locale, TrendingArticlesResponses.MarketPrompt)
            });
        }

        private async Task<DialogTurnResult> SetMarket(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserAccessor.GetAsync(sc.Context, () => new NewsSkillUserState());

            if (userState.Market == null)
            {
                string country = (string)sc.Result;

                // use AzureMaps API to get country code from user input
                userState.Market = await _mapsService.GetCountryCodeAsync(country);
            }

            return await sc.NextAsync();
        }

        private async Task<DialogTurnResult> ShowArticles(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserAccessor.GetAsync(sc.Context, () => new NewsSkillUserState());

            var articles = await _client.GetTrendingNews(userState.Market);
            await _responder.ReplyWith(sc.Context, TrendingArticlesResponses.ShowArticles, articles);

            return await sc.EndDialogAsync();
        }
    }
}
