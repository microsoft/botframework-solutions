using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.CognitiveServices.Search.NewsSearch.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;

namespace NewsSkill.Responses.TrendingArticles
{
    public class TrendingArticlesResponses : TemplateManager
    {
        public const string ShowArticles = "showArticles";
        public const string MarketPrompt = "marketPrompt";

        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { MarketPrompt, (context, data) => "What country are you in?" },
                { ShowArticles, (context, data) => ShowArticleCards(context, data) }
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public TrendingArticlesResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        private static object ShowArticleCards(ITurnContext context, dynamic data)
        {
            var response = context.Activity.CreateReply();
            var articles = data as List<NewsTopic>;

            if (articles.Any())
            {
                response.Text = "Here's what's trending:";

                if (articles.Count > 1)
                {
                    response.Speak = $"I found a few news stories, here's the title of the first: {articles[0].Name}";
                }
                else
                {
                    response.Speak = $"{articles[0].Description}";
                }

                response.Attachments = new List<Attachment>();
                response.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                foreach (var item in articles)
                {
                    var card = new HeroCard()
                    {
                        Title = item.Name,
                        Images = item?.Image?.Url != null ? new List<CardImage>()
                        {
                            new CardImage(item.Image.Url),
                        }
                        : null,
                        Buttons = new List<CardAction>()
                        {
                            new CardAction(ActionTypes.OpenUrl, title: "Read more", value: item.WebSearchUrl)
                        },
                    }.ToAttachment();

                    response.Attachments.Add(card);
                }
            }
            else
            {
                response.Text = "Sorry, I couldn't find any trending articles.";
                response.Speak = "Sorry, I couldn't find any trending articles.";
            }

            return response;
        }
    }
}
