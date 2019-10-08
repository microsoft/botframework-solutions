using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using NewsSkill.Models;
using NewsSkill.Responses.FavoriteTopics;
using NewsSkill.Services;

namespace NewsSkill.Dialogs
{
    public class FavoriteTopicsDialog : NewsDialogBase
    {
        private NewsClient _client;
        private FavoriteTopicsResponses _responder = new FavoriteTopicsResponses();

        public FavoriteTopicsDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            UserState userState,
            AzureMapsService mapsService,
            IBotTelemetryClient telemetryClient)
            : base(nameof(FavoriteTopicsDialog), settings, services, conversationState, userState, mapsService, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var newsKey = settings.BingNewsKey ?? throw new Exception("The BingNewsKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");

            _client = new NewsClient(newsKey);

            var favoriteTopics = new WaterfallStep[]
            {
                GetMarket,
                SetMarket,
                SetFavorites,
                ShowArticles,
            };

            AddDialog(new WaterfallDialog(nameof(FavoriteTopicsDialog), favoriteTopics));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new TextPrompt(nameof(TextPrompt), MarketPromptValidatorAsync));
        }

        private async Task<DialogTurnResult> SetFavorites(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await ConvAccessor.GetAsync(sc.Context, () => new NewsSkillState());
            var userState = await UserAccessor.GetAsync(sc.Context, () => new NewsSkillUserState());

            // if intent is SetFavorites or not set in state yet
            if (convState.LuisResult.TopIntent().intent == Luis.NewsLuis.Intent.SetFavoriteTopics || userState.Category == null)
            {
                // show card with categories the user can choose
                var categories = new PromptOptions()
                {
                    Choices = new List<Choice>(),
                };

                categories.Choices.Add(new Choice("Business"));
                categories.Choices.Add(new Choice("Entertainment"));
                categories.Choices.Add(new Choice("Health"));
                categories.Choices.Add(new Choice("Politics"));
                categories.Choices.Add(new Choice("World"));
                categories.Choices.Add(new Choice("Sports"));

                return await sc.PromptAsync(nameof(ChoicePrompt), new PromptOptions()
                {
                    Prompt = await _responder.RenderTemplate(sc.Context, sc.Context.Activity.Locale, FavoriteTopicsResponses.FavoriteTopicPrompt),
                    Choices = categories.Choices
                });
            }

            return await sc.NextAsync(userState.Category);
        }

        private async Task<DialogTurnResult> ShowArticles(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserAccessor.GetAsync(sc.Context, () => new NewsSkillUserState());

            userState.Category = (FoundChoice)sc.Result;

            // show favorite articles
            var articles = await _client.GetNewsByCategory(userState.Category.Value, userState.Market);
            await _responder.ReplyWith(sc.Context, FavoriteTopicsResponses.ShowArticles, articles);

            return await sc.EndDialogAsync();
        }
    }
}
