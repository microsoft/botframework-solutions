using System.Collections.Generic;
using System.IO;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace CalendarSkill
{
    public class CalendarSkillResponses : TemplateManager
    {
        public const string Intro = "CalendarSkill.Intro";
        public const string Help = "CalendarSkill.Help";
        public const string Greeting = "CalendarSkill.Greeting";
        public const string Confused = "CalendarSkill.Confused";
        public const string Cancelled = "CalendarSkill.Cancelled";

        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { Intro, (context, data) => SendIntroCard(context, data) },
                { Help, (context, data) => SendHelpCard(context, data) },
                { Greeting, (context, data) => "Hi there! I'm the Calendar Skill!" },
                { Confused, (context, data) => "I'm sorry, the Calendar Skill cannot help with that." },
                { Cancelled, (context, data) => "Ok, let's start over." },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public CalendarSkillResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        private static IMessageActivity SendIntroCard(ITurnContext turnContext, dynamic data)
        {
            var response = turnContext.Activity.CreateReply();

            var introCard = File.ReadAllText(@".\CalendarSkill\Resources\Intro.json");

            response.Attachments = new List<Attachment>
            {
                new Attachment()
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = JsonConvert.DeserializeObject(introCard),
                },
            };

            return response;
        }

        private static IMessageActivity SendHelpCard(ITurnContext turnContext, dynamic data)
        {
            var response = turnContext.Activity.CreateReply();
            response.Attachments = new List<Attachment>
            {
                new HeroCard()
                {
                    Title = "Enterprise Bot",
                    Text = "This card can be used to display information to help your user interact with your bot. \n\n The buttons below can be used for sample queries or links to external sites.",
                    Buttons = new List<CardAction>()
                    {
                        new CardAction(type: ActionTypes.ImBack, title: "Test LUIS", value: "Hello"),
                        new CardAction(type: ActionTypes.ImBack, title: "Test QnAMaker", value: "What is the sdk v4?"),
                        new CardAction(type: ActionTypes.OpenUrl, title: "Learn More", value: "https://docs.microsoft.com/en-us/azure/bot-service/?view=azure-bot-service-4.0"),
                    },
                }.ToAttachment(),
            };

            return response;
        }
    }
}
