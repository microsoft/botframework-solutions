// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using $safeprojectname$.Dialogs.Cancel.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;

namespace $safeprojectname$.Dialogs.Cancel
{
    public class CancelResponses : TemplateManager
    {
        // Constants
        public const string _confirmPrompt = "Cancel.ConfirmCancelPrompt";
        public const string _cancelConfirmed = "Cancel.CancelConfirmed";
        public const string _cancelDenied = "Cancel.CancelDenied";

        // Fields
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { _confirmPrompt, (context, data) => MessageFactory.Text(CancelStrings.CANCEL_PROMPT, ssml: CancelStrings.CANCEL_PROMPT, inputHint: InputHints.ExpectingInput) },
                { _cancelConfirmed, (context, data) => MessageFactory.Text(CancelStrings.CANCEL_CONFIRMED, ssml: CancelStrings.CANCEL_CONFIRMED, inputHint: InputHints.IgnoringInput) },
                { _cancelDenied, (context, data) => MessageFactory.Text(CancelStrings.CANCEL_DENIED, ssml: CancelStrings.CANCEL_DENIED, inputHint: InputHints.IgnoringInput) },
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
