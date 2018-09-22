// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using CustomAssistant.Dialogs.Main.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace CustomAssistant
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

            response.Attachments = new List<Attachment>();
            response.Attachments.Add(new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(introCard),
            });

            return response;
        }

        public static IMessageActivity SendHelpCard(ITurnContext turnContext, dynamic data)
        {
            var response = turnContext.Activity.CreateReply();
            response.Attachments = new List<Attachment>()
            {
                new HeroCard()
                {
                    Title = MainStrings.HELP_TITLE,
                    Text = MainStrings.HELP_TEXT
                }.ToAttachment()
            };

            response.SuggestedActions = new SuggestedActions();
            response.SuggestedActions.Actions = new List<CardAction>()
            {
                new CardAction(type: ActionTypes.ImBack, text: "What's my schedule?", value: "What's my schedule?"),
                new CardAction(type: ActionTypes.ImBack, text: "Send an email", value: "Send an email"),
                new CardAction(type: ActionTypes.ImBack, text: "Schedule a meeting", value: "Schedule a meeting"),
                new CardAction(type: ActionTypes.ImBack, text: "Find a coffee shop nearby", value: "Find a coffee shop nearby"),
            };
            return response;
        }
    }
}
