// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;

namespace VirtualAssistant.Responses.Cancel
{
    public class CancelResponses : TemplateManager
    {
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                {
                    ResponseIds.CancelConfirmedMessage,
                    (context, data) =>
                    MessageFactory.Text(
                        text: CancelStrings.CANCEL_CONFIRMED,
                        ssml: CancelStrings.CANCEL_CONFIRMED,
                        inputHint: InputHints.AcceptingInput)
                },
                {
                    ResponseIds.CancelDeniedMessage,
                    (context, data) =>
                    MessageFactory.Text(
                        text: CancelStrings.CANCEL_DENIED,
                        ssml: CancelStrings.CANCEL_DENIED,
                        inputHint: InputHints.AcceptingInput)
                },
                {
                    ResponseIds.CancelPrompt,
                    (context, data) =>
                    MessageFactory.Text(
                        text: CancelStrings.CANCEL_PROMPT,
                        ssml: CancelStrings.CANCEL_PROMPT,
                        inputHint: InputHints.ExpectingInput)
                },
                {
                    ResponseIds.NothingToCancelMessage,
                    (context, data) =>
                    MessageFactory.Text(
                        text: CancelStrings.NOTHING_TO_CANCEL,
                        ssml: CancelStrings.NOTHING_TO_CANCEL,
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
            public const string NothingToCancelMessage = "nothingToCancel";
        }
    }
}
