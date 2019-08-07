using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.Solutions.Responses;
using AdaptiveCards;

namespace PointOfInterestSkill.Utilities
{
    public class LGHelper
    {
        public static async Task<IMessageActivity> GenerateMessageAsync(
            ResourceMultiLanguageGenerator lgMultiLangEngine,
            ITurnContext turnContext,
            string replyTemp,
            object replyArg = null)
        {
            var result = await lgMultiLangEngine.Generate(turnContext, replyTemp, replyArg);
            var activity = await new TextMessageActivityGenerator().CreateActivityFromText(result, null, turnContext, lgMultiLangEngine);

            return activity;
        }

        public static async Task<IMessageActivity> GenerateAdaptiveCardAsync(
            ResourceMultiLanguageGenerator lgMultiLangEngine,
            ITurnContext turnContext,
            string replyTemp,
            object replyArg,
            string cardTemp,
            object cardArg)
        {
            var replyText = await lgMultiLangEngine.Generate(turnContext, replyTemp, replyArg);
            var replyCard = await new TextMessageActivityGenerator().CreateActivityFromText(replyText, null, turnContext, lgMultiLangEngine);

            var cardContent = await lgMultiLangEngine.Generate(turnContext, cardTemp, cardArg);
            var reply = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JsonConvert.DeserializeObject(cardContent)
            };

            replyCard.Attachments.Add(reply);
            return replyCard;
        }

        public static async Task<IMessageActivity> GenerateAdaptiveCardAsync(
            ResourceMultiLanguageGenerator lgMultiLangEngine,
            ITurnContext turnContext,
            string replyTemp,
            object replyArg,
            IEnumerable<Card> cards)
        {
            var replyText = await lgMultiLangEngine.Generate(turnContext, replyTemp, replyArg);
            var replyCard = await new TextMessageActivityGenerator().CreateActivityFromText(replyText, null, turnContext, lgMultiLangEngine);

            foreach (var card in cards)
            {
                var cardContent = await lgMultiLangEngine.Generate(turnContext, $"[{card.Name}]", new { input = card.Data });
                var reply = new Attachment()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = JsonConvert.DeserializeObject(cardContent)
                };

                replyCard.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                replyCard.Attachments.Add(reply);
            }

            return replyCard;
        }
    }
}
