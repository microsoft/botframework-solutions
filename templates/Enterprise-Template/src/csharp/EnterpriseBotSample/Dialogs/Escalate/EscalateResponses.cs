// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using EnterpriseBotSample.Dialogs.Escalate.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using System.Collections.Generic;

namespace EnterpriseBotSample.Dialogs.Escalate
{
    public class EscalateResponses : TemplateManager
    {
        private LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { ResponseIds.SendPhoneMessage, (context, data) => BuildEscalateCard(context, data) },
            }
        };

        public EscalateResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        public static IMessageActivity BuildEscalateCard(ITurnContext turnContext, dynamic data)
        {
            var attachment = new HeroCard()
            {
                Text = EscalateStrings.PHONE_INFO,
                Buttons = new List<CardAction>()
                {
                    new CardAction(type: ActionTypes.OpenUrl, title: "Call now", value: "tel:18005551234"),
                    new CardAction(type: ActionTypes.OpenUrl, title: "Open Teams", value: "msteams://")
                },
            }.ToAttachment();

            return MessageFactory.Attachment(attachment, null, EscalateStrings.PHONE_INFO, InputHints.AcceptingInput);
        }

        public class ResponseIds
        {
            public const string SendPhoneMessage = "sendPhoneMessage";
        }
    }
}
