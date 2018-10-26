// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using CustomerSupportTemplate.Dialogs.Main.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace CustomerSupportTemplate
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
                { Cancelled, (context, data) => MainStrings.Cancelled },
                { Completed, (context, data) => MainStrings.Completed },
                { Confused, (context, data) => MainStrings.Confused },
                { Greeting, (context, data) => MainStrings.Greeting },
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

            var introCard = File.ReadAllText(@".\Dialogs\Main\Resources\IntroCard.json");

            response.Attachments = new List<Attachment>
            {
                new Attachment()
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = JsonConvert.DeserializeObject(introCard),
                }
            };

            response.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction(ActionTypes.ImBack, "Reset my password", value:  "Reset my password"),
                    new CardAction(ActionTypes.ImBack, "Check order status", value:  "Check order status"),
                    new CardAction(ActionTypes.ImBack, "Check return status", value:  "Check return status"),
                    new CardAction(ActionTypes.ImBack, "Shipping options", value:  "Shipping options"),
                    new CardAction(ActionTypes.ImBack, "Find a store", value:  "Find a store"),
                }
            };

            return response;
        }

        public static IMessageActivity SendHelpCard(ITurnContext turnContext, dynamic data)
        {
            var response = turnContext.Activity.CreateReply();

            var helpCard = File.ReadAllText(@".\Dialogs\Main\Resources\HelpCard.json");
            response.Attachments = new List<Attachment>
            {
                new Attachment()
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = JsonConvert.DeserializeObject(helpCard),
                }
            };

            response.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction(ActionTypes.ImBack, "Reset my password", value:  "Reset my password"),
                    new CardAction(ActionTypes.ImBack, "Check order status", value:  "Check order status"),
                    new CardAction(ActionTypes.ImBack, "Check return status", value:  "Check return status"),
                    new CardAction(ActionTypes.ImBack, "Shipping options", value:  "Shipping options"),
                    new CardAction(ActionTypes.ImBack, "Find a store", value:  "Find a store"),
                }
            };

            return response;
        }
    }
}
