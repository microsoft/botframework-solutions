using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Search.NewsSearch.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;

namespace NewsSkill
{
    public class FindArticlesResponses : TemplateManager
    {
        public const string TopicPrompt = "topicPrompt";
        public const string ShowArticles = "showArticles";

        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { TopicPrompt, (context, data) => "What topic are you interested in?" },
                { ShowArticles, (context, data) => ShowArticleCards(context, data) }
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public FindArticlesResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        private static object ShowArticleCards(ITurnContext context, dynamic data)
        {
            var response = context.Activity.CreateReply();
            var articles = data as List<NewsArticle>;

            if (articles.Any())
            {
                response.Text = "Here's what I found:";

                if (articles.Count > 1)
                {
                    response.Speak = $"I found a few news stories, here's a summary of the first: {articles[0].Description}";
                }
                else
                {
                    response.Speak = $"{articles[0].Description}";
                }

                response.Attachments = new List<Attachment>();
                response.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                foreach (var item in articles)
                {
                    var card = new ThumbnailCard()
                    {
                        Title = item.Name,
                        Subtitle = item.DatePublished,
                        Text = item.Description,
                        Images = item?.Image?.Thumbnail?.ContentUrl != null ? new List<CardImage>()
                        {
                            new CardImage(item.Image.Thumbnail.ContentUrl),
                        }
                        : null,
                        Buttons = new List<CardAction>()
                        {
                            new CardAction(ActionTypes.OpenUrl, title: "Read more", value: item.Url)
                        },
                    }.ToAttachment();

                    response.Attachments.Add(card);
                }
            }
            else
            {
                response.Text = "Sorry, I couldn't find any articles on that topic.";
                response.Speak = "Sorry, I couldn't find any articles on that topic.";
            }

            return response;
        }
    }
}
