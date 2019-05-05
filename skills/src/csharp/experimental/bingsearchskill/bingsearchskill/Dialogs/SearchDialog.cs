using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using BingSearchSkill.Models;
using BingSearchSkill.Responses.Search;
using BingSearchSkill.Services;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using BingSearchSkill.Models.Cards;
using Microsoft.Bot.Schema;
using System;

namespace BingSearchSkill.Dialogs
{
    public class SearchDialog : SkillDialogBase
    {
        private const string ApiKeyIndex = "BingSearchKey";
        private BotServices _services;
        private IStatePropertyAccessor<SkillState> _stateAccessor;

        public SearchDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IBotTelemetryClient telemetryClient)
            : base(nameof(SearchDialog), settings, services, responseManager, conversationState, telemetryClient)
        {
            _stateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));
            _services = services;
            Settings = settings;

            var sample = new WaterfallStep[]
            {
                PromptForQuestion,
                ShowResult,
                End,
            };

            AddDialog(new WaterfallDialog(nameof(SearchDialog), sample));
            AddDialog(new TextPrompt(DialogIds.NamePrompt));

            InitialDialogId = nameof(SearchDialog);
        }

        private async Task<DialogTurnResult> PromptForQuestion(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            GetEntityFromLuis(stepContext);

            var state = await _stateAccessor.GetAsync(stepContext.Context);
            if (string.IsNullOrWhiteSpace(state.SearchEntityName))
            {
                var prompt = ResponseManager.GetResponse(SearchResponses.AskEntityPrompt);
                return await stepContext.PromptAsync(DialogIds.NamePrompt, new PromptOptions { Prompt = prompt });
            }

            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> ShowResult(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context);
            var intent = state.LuisResult.TopIntent().intent;

            GetEntityFromLuis(stepContext);
            if (string.IsNullOrWhiteSpace(state.SearchEntityName))
            {
                stepContext.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : stepContext.Context.Activity.Text;

                state.SearchEntityName = userInput;
                state.SearchEntityType = SearchType.Unknown;
            }

            var key = Settings.Properties[ApiKeyIndex] ?? throw new Exception("The BingSearchKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");
            var client = new BingSearchClient(key);
            var entitiesResult = await client.GetSearchResult(state.SearchEntityName);

            var tokens = new StringDictionary
            {
                { "Name", entitiesResult.Value[0].Name },
            };

            Activity prompt = null;
            if (state.SearchEntityType == SearchType.Movie)
            {
                var movieData = new MovieCardData()
                {
                    Title = entitiesResult.Value[0].Name,
                    Description = entitiesResult.Value[0].Description,
                    IconPath = entitiesResult.Value[0].Image.ThumbnailUrl,
                    Score = "8.8/9.0",
                    Type = "test type",
                    Link_Showtimes = entitiesResult.Value[0].WebSearchUrl,
                    Link_Trailers = entitiesResult.Value[0].WebSearchUrl,
                    Link_Trivia = entitiesResult.Value[0].WebSearchUrl,
                    Link_View = entitiesResult.Value[0].WebSearchUrl,
                };

                prompt = ResponseManager.GetCardResponse(
                            SearchResponses.EntityKnowledge,
                            new Card("MovieCard", movieData),
                            tokens);
            }
            else
            {
                var celebrityData = new PersonCardData()
                {
                    Name = entitiesResult.Value[0].Name,
                    Description = entitiesResult.Value[0].Description,
                    IconPath = entitiesResult.Value[0].Image.ThumbnailUrl,
                    Link_View = entitiesResult.Value[0].WebSearchUrl,
                };

                prompt = ResponseManager.GetCardResponse(
                            SearchResponses.EntityKnowledge,
                            new Card("PersonCard", celebrityData),
                            tokens);
            }

            await stepContext.Context.SendActivityAsync(prompt);
            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> End(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context);
            state.Clear();

            return await stepContext.EndDialogAsync();
        }

        private async void GetEntityFromLuis(WaterfallStepContext stepContext)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context);

            if (state.LuisResult.Entities.MovieTitle != null)
            {
                state.SearchEntityName = state.LuisResult.Entities.MovieTitle[0];
                state.SearchEntityType = SearchType.Movie;
            }
            else if (state.LuisResult.Entities.MovieTitlePatten != null)
            {
                state.SearchEntityName = state.LuisResult.Entities.MovieTitlePatten[0];
                state.SearchEntityType = SearchType.Movie;
            }
            else if (state.LuisResult.Entities.CelebrityName != null)
            {
                state.SearchEntityName = state.LuisResult.Entities.CelebrityName[0];
                state.SearchEntityType = SearchType.Celebrity;
            }
            else if (state.LuisResult.Entities.CelebrityNamePatten != null)
            {
                state.SearchEntityName = state.LuisResult.Entities.CelebrityNamePatten[0];
                state.SearchEntityType = SearchType.Celebrity;
            }
        }

        private class DialogIds
        {
            public const string NamePrompt = "namePrompt";
        }
    }
}
