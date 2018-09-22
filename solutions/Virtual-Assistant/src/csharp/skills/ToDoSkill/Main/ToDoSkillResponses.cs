// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill
{
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.TemplateManager;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;

    /// <summary>
    /// ToDoSkillResponses.
    /// </summary>
    public class ToDoSkillResponses : TemplateManager
    {
        /// <summary>
        /// Intro.
        /// </summary>
        public const string Intro = "ToDoSkill.Intro";

        /// <summary>
        /// Help.
        /// </summary>
        public const string Help = "ToDoSkill.Help";

        /// <summary>
        /// Greeting.
        /// </summary>
        public const string Greeting = "ToDoSkill.Greeting";

        /// <summary>
        /// Confused.
        /// </summary>
        public const string Confused = "ToDoSkill.Confused";

        /// <summary>
        /// Cancelled.
        /// </summary>
        public const string Cancelled = "ToDoSkill.Cancelled";

        /// <summary>
        /// table of language functions which render output in various languages.
        /// </summary>
        private static LanguageTemplateDictionary responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { Intro, (context, data) => SendIntroCard(context, data) },
                { Help, (context, data) => SendHelpCard(context, data) },
                { Greeting, (context, data) => "Hi there!" },
                { Confused, (context, data) => "I'm sorry, I'm not sure how to help with that." },
                { Cancelled, (context, data) => "Ok, let's start over." },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ToDoSkillResponses"/> class.
        /// </summary>
        public ToDoSkillResponses()
        {
            this.Register(new DictionaryRenderer(responseTemplates));
        }

        private static IMessageActivity SendIntroCard(ITurnContext turnContext, dynamic data)
        {
            var response = turnContext.Activity.CreateReply();

            var introCard = File.ReadAllText(@".\Dialogs\Shared\Resources\Intro.json");

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
