// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using EnterpriseBotSample.Dialogs.Cancel.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;

namespace EnterpriseBotSample.Dialogs.Cancel
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
                        inputHint: InputHints.AcceptingInput)
                },
                { ResponseIds.CancelDeniedMessage,
                    (context, data) =>
                    MessageFactory.Text(
                        text: CancelStrings.CANCEL_DENIED,
                        ssml: CancelStrings.CANCEL_DENIED,
                        inputHint: InputHints.AcceptingInput)
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
