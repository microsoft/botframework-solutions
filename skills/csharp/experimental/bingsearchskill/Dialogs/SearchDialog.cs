using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using BingSearchSkill.Models;
using BingSearchSkill.Models.Cards;
using BingSearchSkill.Responses.Search;
using BingSearchSkill.Services;
using BingSearchSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace BingSearchSkill.Dialogs
{
    public class SearchDialog : SkillDialogBase
    {
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
            //if (string.IsNullOrWhiteSpace(state.SearchEntityName))
            //{
            //    var prompt = ResponseManager.GetResponse(SearchResponses.AskEntityPrompt);
            //    return await stepContext.PromptAsync(DialogIds.NamePrompt, new PromptOptions { Prompt = prompt });
            //}

            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> ShowResult(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context);
            var intent = state.LuisResult.TopIntent().intent;

            GetEntityFromLuis(stepContext);
            var userInput = string.Empty;
            if (string.IsNullOrWhiteSpace(state.SearchEntityName))
            {
                stepContext.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                userInput = content != null ? content.ToString() : stepContext.Context.Activity.Text;

                state.SearchEntityName = userInput;
                state.SearchEntityType = SearchResultModel.EntityType.Unknown;
            }

            var bingSearchKey = Settings.BingSearchKey ?? throw new Exception("The BingSearchKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");
            var bingAnswerSearchKey = Settings.BingAnswerSearchKey ?? throw new Exception("The BingSearchKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");
            var client = new BingSearchClient(bingSearchKey, bingAnswerSearchKey);
            // https://github.com/MicrosoftDocs/azure-docs/blob/master/articles/cognitive-services/Labs/Answer-Search/overview.md
            var entitiesResult = await client.GetSearchResult(state.SearchEntityName, "en-us", state.SearchEntityType);

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
                        Name = movieInfo.Name,
                        Description = movieInfo.Description,
                        Image = movieInfo.Image,
                        Rating = $"{movieInfo.Rating}",
                        GenreArray = string.Join(" ▪ ", movieInfo.Genre),
                        ContentRating = movieInfo.ContentRating,
                        Duration = movieInfo.Duration,
                        Year = movieInfo.Year,
                    };

                    tokens.Add("Speak", movieInfo.Description);

                    prompt = ResponseManager.GetCardResponse(
                                SearchResponses.EntityKnowledge,
                                new Card(GetDivergedCardName(stepContext.Context, "MovieCard"), movieData),
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

                    if (Channel.GetChannelId(stepContext.Context) == Channels.Msteams && !string.IsNullOrEmpty(entitiesResult[0].ThumbnailUrl))
                    {
                        celebrityData.IconPath = entitiesResult[0].ThumbnailUrl;
                    }

                    tokens.Add("Speak", entitiesResult[0].Description);

                    prompt = ResponseManager.GetCardResponse(
                                SearchResponses.EntityKnowledge,
                                new Card(GetDivergedCardName(stepContext.Context, "PersonCard"), celebrityData),
                                tokens);
                }
                else
                {
                    if (userInput.Contains("president"))
                    {
                        prompt = ResponseManager.GetResponse(SearchResponses.AnswerSearchResultPrompt, new StringDictionary()
                        {
                            { "Answer", "Sorry I do not know this answer yet."},
                            { "Url", "www.bing.com" }
                        });
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
