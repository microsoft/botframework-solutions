// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using VirtualAssistant.Dialogs.Main.Resources;

namespace VirtualAssistant
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
                { Cancelled, (context, data) => SendAcceptingInputReply(context, MainStrings.CANCELLED) },
                { Completed, (context, data) => SendAcceptingInputReply(context, MainStrings.COMPLETED) },
                { Confused, (context, data) => SendAcceptingInputReply(context, MainStrings.CONFUSED) },
                { Greeting, (context, data) => SendAcceptingInputReply(context, MainStrings.GREETING) },
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
            var introCard = MainStrings.ResourceManager.GetObject("Intro").ToString();

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

        public static IMessageActivity SendHelpCard(ITurnContext turnContext, dynamic data)
        {
            var response = turnContext.Activity.CreateReply();
            response.Attachments = new List<Attachment>()
            {
                new HeroCard()
                {
                    Title = MainStrings.HELP_TITLE,
                    Text = MainStrings.HELP_TEXT,
                }.ToAttachment(),
            };

            response.SuggestedActions = new SuggestedActions
            {
                Actions = new List<CardAction>()
            {
                new CardAction(type: ActionTypes.ImBack, title: MainStrings.CALENDAR_SUGGESTEDACTION),
                new CardAction(type: ActionTypes.ImBack, title: MainStrings.EMAIL_SUGGESTEDACTION),
                new CardAction(type: ActionTypes.ImBack, title: MainStrings.MEETING_SUGGESTEDACTION),
                new CardAction(type: ActionTypes.ImBack, title: MainStrings.POI_SUGGESTEDACTION),
            },
            };
            return response;
        }

        public static IMessageActivity SendAcceptingInputReply(ITurnContext turnContext, string text)
        {
            var reply = turnContext.Activity.CreateReply();
            reply.InputHint = InputHints.AcceptingInput;
            reply.Text = text;

            return reply;
        }
    }
}
