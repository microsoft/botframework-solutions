// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using AdaptiveCards;
using $safeprojectname$.Dialogs.Main.Resources;
using $safeprojectname$.Extensions;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;

namespace $safeprojectname$.Dialogs.Main
{
    public class MainResponses : TemplateManagerWithVoice
    {
        // Constants
        public const string Cancelled = "cancelled";
        public const string Completed = "completed";
        public const string Confused = "confused";
        public const string Greeting = "greeting";
        public const string Help = "help";
        public const string Intro = "intro";

        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { Cancelled, (context, data) => MainStrings.CANCELLED },
                { Completed, (context, data) => MainStrings.COMPLETED },
                { Confused, (context, data) => MainStrings.CONFUSED },
                { Greeting, (context, data) => MainStrings.GREETING },
                { Help, (context, data) => SendHelpCard(context, data) },
                { Intro, (context, data) => SendIntroCard(context, data) },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public MainResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        public static IMessageActivity SendIntroCard(ITurnContext turnContext, dynamic data)
        {
            var response = turnContext.Activity.CreateReply();

            var introCard = File.ReadAllText(@".\Dialogs\Main\Resources\Intro.json");
            AdaptiveCard card = AdaptiveCard.FromJson(introCard).Card;
            response.Speak = card.Speak;
            response.Attachments = new List<Attachment>
            {
                new Attachment()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = card
                }
            };

            response.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction(type: ActionTypes.ImBack, title: "Test LUIS", value: "Hi there!"),
                    new CardAction(type: ActionTypes.ImBack, title: "Test QnAMaker", value: "Why did Microsoft develop the Bot Framework?"),
                    new CardAction(type: ActionTypes.OpenUrl, title: "Learn More", value: "https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-enterprise-template-overview?view=azure-bot-service-4.0")
                }
            };

            return response;
        }

        public static IMessageActivity SendHelpCard(ITurnContext turnContext, dynamic data)
        {
            var response = turnContext.Activity.CreateReply();
            response.Speak = MainStrings.HELP_TEXT;
            response.Attachments = new List<Attachment>
            {
                new HeroCard()
                {
                    Title = MainStrings.HELP_TITLE,
                    Text = MainStrings.HELP_TEXT,
                    Buttons = new List<CardAction>()
                {
                    new CardAction(type: ActionTypes.ImBack, title: "Test LUIS", value: "Hello"),
                    new CardAction(type: ActionTypes.ImBack, title: "Test QnAMaker", value: "What is the sdk v4?"),
                    new CardAction(type: ActionTypes.OpenUrl, title: "Learn More", value: "https://docs.microsoft.com/en-us/azure/bot-service/?view=azure-bot-service-4.0"),
                },
                }.ToAttachment()
            };

            return response;
        }
    }
}