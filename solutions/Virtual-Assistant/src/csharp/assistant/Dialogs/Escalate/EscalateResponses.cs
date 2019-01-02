// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using VirtualAssistant.Dialogs.Escalate.Resources;

namespace VirtualAssistant.Dialogs.Escalate
{
    public class EscalateResponses : TemplateManager
    {
        private LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                {
                    ResponseIds.SendEscalationMessage,
                    (context, data) =>
                    MessageFactory.Text(
                        text: EscalateStrings.PHONE_INFO,
                        ssml: EscalateStrings.PHONE_INFO,
                        inputHint: InputHints.AcceptingInput)
                },
            }
        };

        public EscalateResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        public class ResponseIds
        {
            public const string SendEscalationMessage = "sendPhone";
        }
    }
}