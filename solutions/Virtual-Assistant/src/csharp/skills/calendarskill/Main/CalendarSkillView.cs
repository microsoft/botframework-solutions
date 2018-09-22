// <copyright file="CalendarSkillView.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CalendarSkill
{
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.TemplateManager;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;

    /// <summary>
    /// CalendarSkillView.
    /// </summary>
    public class CalendarSkillView : TemplateManager
    {
        /// <summary>
        /// INTRO.
        /// </summary>
        public const string INTRO = "MainDialog.Intro";

        /// <summary>
        /// HELP.
        /// </summary>
        public const string HELP = "MainDialog.Help";

        /// <summary>
        /// GREETING.
        /// </summary>
        public const string GREETING = "MainDialog.Greeting";

        /// <summary>
        /// CONFUSED.
        /// </summary>
        public const string CONFUSED = "MainDialog.Confused";

        /// <summary>
        /// CANCELLED.
        /// </summary>
        public const string CANCELLED = "MainDialog.Cancelled";

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarSkillView"/> class.
        /// </summary>
        public CalendarSkillView()
        {
            this.ResponseTemplates = new LanguageTemplateDictionary
            {
                ["default"] = new TemplateIdMap
            {
                // { INTRO, (context, data) => Intro(context, data) },
                { HELP, (context, data) => Help(context, data) },
                { GREETING, (context, data) => "Hi there!" },
                { CONFUSED, (context, data) => "I'm sorry, I'm not sure how to help with that." },
                { CANCELLED, (context, data) => "Ok, let's start over." },
            },
                ["en"] = new TemplateIdMap { },
                ["fr"] = new TemplateIdMap { },
            };
            this.Register(new DictionaryRenderer(this.ResponseTemplates));
        }

        /// <summary>
        /// Gets or sets table of language functions which render output in various languages.
        /// </summary>
        /// <value>
        /// table of language functions which render output in various languages.
        /// </value>
        public LanguageTemplateDictionary ResponseTemplates { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarSkill"/> class.
        /// </summary>
        /// <param name="turnContext">Turn context.</param>
        /// <param name="data">Data.</param>
        /// <returns>Response.</returns>
        public static IMessageActivity Intro(ITurnContext turnContext, dynamic data)
        {
            var response = turnContext.Activity.CreateReply();

            var introCard = File.ReadAllText(@".\Resources\Intro.json");

            response.Attachments = new List<Attachment>();
            response.Attachments.Add(new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(introCard),
            });

            return response;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarSkill"/> class.
        /// </summary>
        /// <param name="turnContext">Turn context.</param>
        /// <param name="data">Data.</param>
        /// <returns>Response.</returns>
        public static IMessageActivity Help(ITurnContext turnContext, dynamic data)
        {
            var response = turnContext.Activity.CreateReply();
            response.Attachments = new List<Attachment>();

            response.Attachments.Add(new HeroCard()
            {
                Title = "Enterprise Bot",
                Text = "This card can be used to display information to help your user interact with your bot. \n\n The buttons below can be used for sample queries or links to external sites.",
                Buttons = new List<CardAction>()
                {
                    new CardAction(type: ActionTypes.ImBack, title: "Test LUIS", value: "Hello"),
                    new CardAction(type: ActionTypes.ImBack, title: "Test QnAMaker", value: "What is the sdk v4?"),
                    new CardAction(type: ActionTypes.OpenUrl, title: "Learn More", value: "https://docs.microsoft.com/en-us/azure/bot-service/?view=azure-bot-service-4.0"),
                },
            }.ToAttachment());

            return response;
        }
    }
}
