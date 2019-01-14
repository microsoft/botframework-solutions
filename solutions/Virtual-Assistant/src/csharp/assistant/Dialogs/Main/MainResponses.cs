// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using VirtualAssistant.Dialogs.Main.Resources;

namespace VirtualAssistant.Dialogs.Main
{
    public class MainResponses : TemplateManager
    {
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                {
                    ResponseIds.Cancelled,
                    (context, data) => MessageFactory.Text(MainStrings.CANCELLED, MainStrings.CANCELLED, InputHints.AcceptingInput)
                },
                {
                    ResponseIds.NoActiveDialog,
                    (context, data) => MessageFactory.Text(MainStrings.NO_ACTIVE_DIALOG, MainStrings.NO_ACTIVE_DIALOG, InputHints.AcceptingInput)
                },
                {
                    ResponseIds.Completed,
                    (context, data) => MessageFactory.Text(MainStrings.COMPLETED, MainStrings.COMPLETED, InputHints.AcceptingInput)
                },
                {
                    ResponseIds.Confused,
                    (context, data) => MessageFactory.Text(MainStrings.CONFUSED, MainStrings.CONFUSED, InputHints.AcceptingInput)
                },
                {
                    ResponseIds.Greeting,
                    (context, data) => MessageFactory.Text(MainStrings.GREETING, MainStrings.GREETING, InputHints.AcceptingInput)
                },
                {
                    ResponseIds.Error,
                    (context, data) => MessageFactory.Text(MainStrings.ERROR, MainStrings.ERROR, InputHints.AcceptingInput)
                },
                {
                    ResponseIds.Help,
                    (context, data) => BuildHelpCard(context, data)
                },
                {
                    ResponseIds.Intro,
                    (context, data) => BuildIntroCard(context, data)
                },
                {
                    ResponseIds.Qna,
                    (context, data) => BuildQnACard(context, data)
                },
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

            return MessageFactory.Attachment(attachment, ssml: card.Speak, inputHint: InputHints.AcceptingInput);
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
                    new CardAction(type: ActionTypes.ImBack, title: MainStrings.CALENDAR_SUGGESTEDACTION),
                    new CardAction(type: ActionTypes.ImBack, title: MainStrings.EMAIL_SUGGESTEDACTION),
                    new CardAction(type: ActionTypes.ImBack, title: MainStrings.MEETING_SUGGESTEDACTION),
                    new CardAction(type: ActionTypes.ImBack, title: MainStrings.POI_SUGGESTEDACTION),
                },
            };

            return response;
        }

        public static IMessageActivity BuildQnACard(ITurnContext turnContext, dynamic answer)
        {
            var response = turnContext.Activity.CreateReply();

            try
            {
                ThumbnailCard card = JsonConvert.DeserializeObject<ThumbnailCard>(answer);

                response.Attachments = new List<Attachment>
                {
                    card.ToAttachment(),
                };

                response.Speak = card.Title != null ? $"{card.Title} " : string.Empty;
                response.Speak += card.Subtitle != null ? $"{card.Subtitle} " : string.Empty;
                response.Speak += card.Text != null ? $"{card.Text} " : string.Empty;
            }
            catch (JsonException)
            {
                response.Text = answer;
            }

            return response;
        }

        public class ResponseIds
        {
            public const string Cancelled = "cancelled";
            public const string Completed = "completed";
            public const string Confused = "confused";
            public const string Greeting = "greeting";
            public const string Help = "help";
            public const string Intro = "intro";
            public const string Error = "error";
            public const string NoActiveDialog = "noActiveDialog";
            public const string Qna = "qna";
        }
    }
}