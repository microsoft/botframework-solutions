// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using VirtualAssistant.Dialogs.Escalate.Resources;

namespace VirtualAssistant
{
    public class EscalateResponses : TemplateManager
    {
        public const string SendPhone = "sendPhone";

        private LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { SendPhone, (context, data) => SendAcceptingInputReply(context, EscalateStrings.PHONE_INFO) },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public EscalateResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
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
