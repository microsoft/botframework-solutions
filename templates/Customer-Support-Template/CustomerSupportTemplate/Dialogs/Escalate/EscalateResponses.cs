// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CustomerSupportTemplate.Dialogs.Escalate.Resources;
using Microsoft.Bot.Builder.TemplateManager;

namespace CustomerSupportTemplate
{
    public class EscalateResponses : TemplateManager
    {
        private LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { ResponseIds.EscalationMessage, (context, data) => EscalateStrings.EscalationMessage },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public EscalateResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        public class ResponseIds
        {
            public const string EscalationMessage = "escalationMessage";
        }
    }
}
