// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CalendarSkill.Dialogs.Cancel.Resources;
using Microsoft.Bot.Builder.TemplateManager;

namespace CalendarSkill
{
    public class CancelResponses : TemplateManager
    {
        // Constants
        public const string _cancelConfirmed = "Cancel.CancelConfirmed";

        // Fields
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { _cancelConfirmed, (context, data) => CancelStrings.CANCEL_CONFIRMED },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public CancelResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }
    }
}
