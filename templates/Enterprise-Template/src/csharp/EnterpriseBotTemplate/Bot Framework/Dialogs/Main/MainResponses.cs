// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using AdaptiveCards;
using $safeprojectname$.Dialogs.Main.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;

namespace $safeprojectname$.Dialogs.Main
{
    public class MainResponses : TemplateManager
    {
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { ResponseIds.Cancelled,
                    (context, data) => 
                    MessageFactory.Text(
                        text: MainStrings.CANCELLED, 
                        ssml: MainStrings.CANCELLED, 
                        inputHint: InputHints.IgnoringInput)
                },
                { ResponseIds.Completed,
                    (context, data) => 
                    MessageFactory.Text(
                        text: MainStrings.COMPLETED, 
                        ssml: MainStrings.COMPLETED, 
                        inputHint: InputHints.IgnoringInput)
                },
                { ResponseIds.Confused,
                    (context, data) => 
                    MessageFactory.Text(
                        text: MainStrings.CONFUSED, 
                        ssml: MainStrings.CONFUSED, 
                        inputHint: InputHints.IgnoringInput)
                },
                { ResponseIds.Greeting,
                    (context, data) => 
                    MessageFactory.Text(
                        text: MainStrings.GREETING, 
                        ssml: MainStrings.GREETING, 
                        inputHint: InputHints.IgnoringInput)
                },
                { ResponseIds.Help, (context, data) => BuildHelpCard(context, data) },
                { ResponseIds.Intro, (context, data) => BuildIntroCard(context, data) },
            }
        };

        public MainResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        public static IMessageActivity BuildIntroCard(ITurnContext turnContext, dynamic data)
        {
            var introCard = File.ReadAllText(@".\Dialogs\Main\Resources\Intro.json");
            var card = AdaptiveCard.FromJson(introCard).Card;
            var attachment = new Attachment(AdaptiveCard.ContentType, content: card);

            var response = MessageFactory.Attachment(attachment, ssml: card.Speak, inputHint: InputHints.AcceptingInput);
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

        public static IMessageActivity BuildHelpCard(ITurnContext turnContext, dynamic data)
        {
            var attachment = new HeroCard()
            {
                Title = MainStrings.HELP_TITLE,
                Text = MainStrings.HELP_TEXT,
                Buttons = new List<CardAction>()
                {
                    new CardAction(type: ActionTypes.ImBack, title: "Test LUIS", value: "Hello"),
                    new CardAction(type: ActionTypes.ImBack, title: "Test QnAMaker", value: "What is the sdk v4?"),
                    new CardAction(type: ActionTypes.OpenUrl, title: "Learn More", value: "https://docs.microsoft.com/en-us/azure/bot-service/?view=azure-bot-service-4.0"),
                },
            }.ToAttachment();

            var response = MessageFactory.Attachment(attachment, ssml: MainStrings.HELP_TEXT, inputHint: InputHints.AcceptingInput);
            return response;
        }

        public class ResponseIds
        {
            // Constants
            public const string Cancelled = "cancelled";
            public const string Completed = "completed";
            public const string Confused = "confused";
            public const string Greeting = "greeting";
            public const string Help = "help";
            public const string Intro = "intro";
        }
    }
}