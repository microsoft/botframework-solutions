using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.LanguageGeneration;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace EmailSkill.Util
{
    public class LGHelper
    {
        public static string GetFilePath(string dialogName, string fileName)
        {
            return AppContext.BaseDirectory + "Dialogs\\" + dialogName + "\\Resources\\LG\\" + fileName;
        }

        public static Activity GetCardResponseWithLG(
            string templateId,
            IEnumerable<Card> cards,
            object tokens = null,
            string attachmentLayout = AttachmentLayoutTypes.Carousel)
        {

            var engine = TemplateEngine.FromFile(LGHelper.GetFilePath("Shared", "SharedEmailResponses.lg"));
            var attachments = new List<Attachment>();

            foreach (var item in cards)
            {
                var json = engine.EvaluateTemplate("EmailCardTemplate", new { emailCardData = item.Data });

                // Deserialize/Serialize logic is needed to prevent JSON exception in prompts
                var card = AdaptiveCard.FromJson(json).Card;
                var cardObj = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(card));
                attachments.Add(new Attachment(AdaptiveCard.ContentType, content: cardObj));
            }

            var activityGenerator = new LGActivityGenerator(engine);
            var options = new ActivityGenerationConfig();
            options.TextSpeakTemplateId = templateId;

            var activity = activityGenerator.Generate(options, tokens);

            return MessageFactory.Carousel(attachments, activity.Text, activity.Speak, activity.InputHint) as Activity;
        }
    }
}
