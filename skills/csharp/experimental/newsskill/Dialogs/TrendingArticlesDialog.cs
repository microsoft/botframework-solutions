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

        public TrendingArticlesDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            UserState userState,
            AzureMapsService mapsService,
            IBotTelemetryClient telemetryClient)
            : base(nameof(TrendingArticlesDialog), settings, services, conversationState, userState, mapsService, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var key = settings.BingNewsKey ?? throw new Exception("The BingNewsKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");

            _client = new NewsClient(key);

            var trendingArticles = new WaterfallStep[]
            {
                GetMarket,
                SetMarket,
                ShowArticles,
            };

            AddDialog(new WaterfallDialog(nameof(TrendingArticlesDialog), trendingArticles));
            AddDialog(new TextPrompt(nameof(TextPrompt), MarketPromptValidatorAsync));
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
