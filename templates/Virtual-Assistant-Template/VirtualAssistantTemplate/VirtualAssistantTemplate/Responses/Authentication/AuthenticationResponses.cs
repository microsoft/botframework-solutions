// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using VirtualAssistantTemplate.Responses.Authentication;

namespace VirtualAssistantTemplate.Responses.Authentication
{
    public class AuthenticationResponses : TemplateManager
    {
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { ResponseIds.LoginPrompt,
                    (context, data) =>
                    MessageFactory.Text(
                        text: AuthenticationStrings.PROMPT,
                        ssml: AuthenticationStrings.PROMPT,
                        inputHint: InputHints.AcceptingInput)
                },
                { ResponseIds.SucceededMessage,
                    (context, data) =>
                    MessageFactory.Text(
                        text: string.Format(AuthenticationStrings.SUCCEEDED, data.name),
                        ssml: string.Format(AuthenticationStrings.SUCCEEDED, data.name),
                        inputHint: InputHints.IgnoringInput)
                },
                { ResponseIds.FailedMessage,
                    (context, data) =>
                    MessageFactory.Text(
                        text: AuthenticationStrings.FAILED,
                        ssml: AuthenticationStrings.FAILED,
                        inputHint: InputHints.IgnoringInput)
                },
            }
        };

        public AuthenticationResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        public class ResponseIds
        {
            public const string LoginPrompt = "loginPrompt";
            public const string SucceededMessage = "succeededMessage";
            public const string FailedMessage = "failedMessage";
        }
    }
}
