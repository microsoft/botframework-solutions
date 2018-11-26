// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.TemplateManager;
using ToDoSkill.Dialogs.Cancel.Resources;

namespace ToDoSkill
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
