// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using AdaptiveCards;
using EnterpriseBotSample.Dialogs.Main.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;

namespace EnterpriseBotSample.Dialogs.Main
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
                        inputHint: InputHints.AcceptingInput)
                },
                { ResponseIds.Completed,
                    (context, data) =>
                    MessageFactory.Text(
                        text: MainStrings.COMPLETED,
                        ssml: MainStrings.COMPLETED,
                        inputHint: InputHints.AcceptingInput)
                },
                { ResponseIds.Confused,
                    (context, data) =>
                    MessageFactory.Text(
                        text: MainStrings.CONFUSED,
                        ssml: MainStrings.CONFUSED,
                        inputHint: InputHints.AcceptingInput)
                },
                { ResponseIds.Greeting,
                    (context, data) =>
                    MessageFactory.Text(
                        text: MainStrings.GREETING,
                        ssml: MainStrings.GREETING,
                        inputHint: InputHints.AcceptingInput)
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
            var introCard = File.ReadAllText(MainStrings.INTRO_PATH);
            var card = AdaptiveCard.FromJson(introCard).Card;
            var attachment = new Attachment(AdaptiveCard.ContentType, content: card);

            var response = MessageFactory.Attachment(attachment, ssml: card.Speak, inputHint: InputHints.AcceptingInput);

            response.SuggestedActions = new SuggestedActions
            {
                Actions = new List<CardAction>()
                {
                    new CardAction(type: ActionTypes.ImBack, title: MainStrings.HELP_BTN_TEXT_1, value: MainStrings.HELP_BTN_VALUE_1),
                    new CardAction(type: ActionTypes.ImBack, title: MainStrings.HELP_BTN_TEXT_2, value: MainStrings.HELP_BTN_VALUE_2),
                    new CardAction(type: ActionTypes.OpenUrl, title: MainStrings.HELP_BTN_TEXT_3, value: MainStrings.HELP_BTN_VALUE_3),
                },
            };

            return response;
        }

        public static IMessageActivity BuildHelpCard(ITurnContext turnContext, dynamic data)
        {
            var attachment = new HeroCard()
            {
                Title = MainStrings.HELP_TITLE,
                Text = MainStrings.HELP_TEXT,
            }.ToAttachment();

            var response = MessageFactory.Attachment(attachment, ssml: MainStrings.HELP_TEXT, inputHint: InputHints.AcceptingInput);

            response.SuggestedActions = new SuggestedActions
            {
                Actions = new List<CardAction>()
                {
                    new CardAction(type: ActionTypes.ImBack, title: MainStrings.HELP_BTN_TEXT_1, value: MainStrings.HELP_BTN_VALUE_1),
                    new CardAction(type: ActionTypes.ImBack, title: MainStrings.HELP_BTN_TEXT_2, value: MainStrings.HELP_BTN_VALUE_2),
                    new CardAction(type: ActionTypes.OpenUrl, title: MainStrings.HELP_BTN_TEXT_3, value: MainStrings.HELP_BTN_VALUE_3),
                },
            };

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
