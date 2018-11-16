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
                { Cancelled, (context, data) => MessageFactory.Text(MainStrings.CANCELLED, ssml: MainStrings.CANCELLED, inputHint: InputHints.IgnoringInput) },
                { Completed, (context, data) => MessageFactory.Text(MainStrings.COMPLETED, ssml: MainStrings.COMPLETED, inputHint: InputHints.IgnoringInput) },
                { Confused, (context, data) => MessageFactory.Text(MainStrings.CONFUSED, ssml: MainStrings.CONFUSED, inputHint: InputHints.IgnoringInput) },
                { Greeting, (context, data) => MessageFactory.Text(MainStrings.GREETING, ssml: MainStrings.GREETING, inputHint: InputHints.IgnoringInput) },
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

            response.InputHint = InputHints.AcceptingInput;
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

            response.InputHint = InputHints.AcceptingInput;
            return response;
        }
    }
}