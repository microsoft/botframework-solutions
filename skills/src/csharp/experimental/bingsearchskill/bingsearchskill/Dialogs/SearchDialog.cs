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
using BingSearchSkill.Utilities;

namespace BingSearchSkill.Dialogs
{
    public class SearchDialog : SkillDialogBase
    {
        private const string BingSearchApiKeyIndex = "BingSearchKey";
        private const string BingAnswerSearchApiKeyIndex = "BingAnswerSearchKey";
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
                state.SearchEntityType = SearchResultModel.EntityType.Unknown;
            }

            var bingSearchKey = Settings.Properties[BingSearchApiKeyIndex] ?? throw new Exception("The BingSearchKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");
            var bingAnswerSearchKey = Settings.Properties[BingAnswerSearchApiKeyIndex] ?? throw new Exception("The BingSearchKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");
            var client = new BingSearchClient(bingSearchKey, bingAnswerSearchKey);
            var entitiesResult = await client.GetSearchResult(state.SearchEntityName, state.SearchEntityType);

            Activity prompt = null;
            if (entitiesResult != null && entitiesResult.Count > 0)
            {
                var tokens = new StringDictionary
                {
                    { "Name", entitiesResult[0].Name },
                };

                if (entitiesResult[0].Type == SearchResultModel.EntityType.Movie)
                {
                    var movieInfo = MovieHelper.GetMovieInfoFromUrl(entitiesResult[0].Url);
                    tokens["Name"] = movieInfo.Name;
                    var movieData = new MovieCardData()
                    {
                        Title = movieInfo.Name,
                        Description = movieInfo.Description,
                        IconPath = movieInfo.Image,
                        Score = $"{movieInfo.Rating}/10",
                        Type = string.Join(", ", movieInfo.Genre),
                        Link_Trailers = $"https://www.imdb.com/{movieInfo.TrailerUrl}",
                        Link_Trivia = $"https://www.imdb.com/{movieInfo.Url}trivia",
                        Link_View = entitiesResult[0].Url,
                    };

                    prompt = ResponseManager.GetCardResponse(
                                SearchResponses.EntityKnowledge,
                                new Card("MovieCard", movieData),
                                tokens);
                }
                else if (entitiesResult[0].Type == SearchResultModel.EntityType.Person)
                {
                    var celebrityData = new PersonCardData()
                    {
                        Name = entitiesResult[0].Name,
                        Description = entitiesResult[0].Description,
                        IconPath = entitiesResult[0].ImageUrl,
                        Link_View = entitiesResult[0].Url,
                        EntityTypeDisplayHint = entitiesResult[0].EntityTypeDisplayHint
                    };

                    prompt = ResponseManager.GetCardResponse(
                                SearchResponses.EntityKnowledge,
                                new Card("PersonCard", celebrityData),
                                tokens);
                }
                else
                {
                    prompt = ResponseManager.GetResponse(SearchResponses.AnswerSearchResultPrompt, new StringDictionary()
                    {
                        { "Answer", entitiesResult[0].Description},
                        { "Url", entitiesResult[0].Url}
                    });
                }
            }
            else
            {
                prompt = ResponseManager.GetResponse(SearchResponses.NoResultPrompt);
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
                state.SearchEntityType = SearchResultModel.EntityType.Movie;
            }
            else if (state.LuisResult.Entities.MovieTitlePatten != null)
            {
                state.SearchEntityName = state.LuisResult.Entities.MovieTitlePatten[0];
                state.SearchEntityType = SearchResultModel.EntityType.Movie;
            }
            else if (state.LuisResult.Entities.CelebrityName != null)
            {
                state.SearchEntityName = state.LuisResult.Entities.CelebrityName[0];
                state.SearchEntityType = SearchResultModel.EntityType.Person;
            }
            else if (state.LuisResult.Entities.CelebrityNamePatten != null)
            {
                state.SearchEntityName = state.LuisResult.Entities.CelebrityNamePatten[0];
                state.SearchEntityType = SearchResultModel.EntityType.Person;
            }
        }

        private class DialogIds
        {
            public const string NamePrompt = "namePrompt";
        }
    }
}
