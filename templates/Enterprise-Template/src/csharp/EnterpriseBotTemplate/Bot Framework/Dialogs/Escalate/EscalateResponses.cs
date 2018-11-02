// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using $safeprojectname$.Dialogs.Escalate.Resources;
using $safeprojectname$.Extensions;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using System.Collections.Generic;

namespace $safeprojectname$
{
    public class EscalateResponses : TemplateManagerWithVoice
    {
        public const string SendPhone = "sendPhone";

        private LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { SendPhone, SendEscalateCard },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public EscalateResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        public static IMessageActivity SendEscalateCard(ITurnContext turnContext, dynamic data)
        {
            var response = turnContext.Activity.CreateReply();

            response.Speak = EscalateStrings.PHONE_INFO;
            response.Attachments = new List<Attachment>
            {
                new HeroCard()
                {
                    Text = EscalateStrings.PHONE_INFO,
                    Buttons = new List<CardAction>()
                {
                    new CardAction(type: ActionTypes.OpenUrl, title: "Call now", value: "tel:8001235555"),
                    new CardAction(type: ActionTypes.OpenUrl, title: "Open Teams", value: "msteams://")
                },
                }.ToAttachment()
            };

            return response;
        }
    }
}
