// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CustomerSupportTemplate.Dialogs.Cancel.Resources;
using Microsoft.Bot.Builder.TemplateManager;

namespace CustomerSupportTemplate
{
    public class CancelResponses : TemplateManager
    {
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { ResponseIds.ConfirmCancelPrompt, (context, data) => CancelStrings.ConfirmCancelPrompt },
                { ResponseIds.CancelConfirmedMessage, (context, data) => CancelStrings.CancelConfirmedMessage },
                { ResponseIds.CancelDeniedMessage, (context, data) => CancelStrings.CancelDeniedMessage },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public CancelResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        public class ResponseIds
        {
            public const string ConfirmCancelPrompt = "Cancel.ConfirmCancelPrompt";
            public const string CancelConfirmedMessage = "Cancel.CancelConfirmed";
            public const string CancelDeniedMessage = "Cancel.CancelDenied";
        }
    }
}
