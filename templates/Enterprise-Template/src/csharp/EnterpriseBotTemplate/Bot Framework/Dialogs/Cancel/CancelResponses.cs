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
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { ResponseIds.CancelConfirmedMessage,
                    (context, data) =>
                    MessageFactory.Text(
                        text: CancelStrings.CANCEL_CONFIRMED,
                        ssml: CancelStrings.CANCEL_CONFIRMED,
                        inputHint: InputHints.IgnoringInput)
                },
                { ResponseIds.CancelDeniedMessage,
                    (context, data) =>
                    MessageFactory.Text(
                        text: CancelStrings.CANCEL_DENIED,
                        ssml: CancelStrings.CANCEL_DENIED,
                        inputHint: InputHints.IgnoringInput)
                },
                { ResponseIds.CancelPrompt,
                    (context, data) =>
                    MessageFactory.Text(
                        text: CancelStrings.CANCEL_PROMPT,
                        ssml: CancelStrings.CANCEL_PROMPT,
                        inputHint: InputHints.ExpectingInput)
                },
            }
        };

        public CancelResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        public class ResponseIds
        {
            public const string CancelPrompt = "cancelPrompt";
            public const string CancelConfirmedMessage = "cancelConfirmed";
            public const string CancelDeniedMessage = "cancelDenied";
        }
    }
}
