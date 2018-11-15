// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using $safeprojectname$.Dialogs.SignIn.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;

namespace $safeprojectname$.Dialogs.Shared
{
    public class SignInResponses : TemplateManager
    {
        // Constants
        public const string SignInPrompt = "namePrompt";
        public const string Succeeded = "haveName";
        public const string Failed = "emailPrompt";

        // Fields
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                {
                    SignInPrompt,
                    (context, data) => CreateActivityFromString(SignInStrings.PROMPT, inputHint:InputHints.AcceptingInput)
                },
                {
                    Succeeded,
                    (context, data) => CreateActivityFromString(string.Format(SignInStrings.SUCCEEDED, data.name))
                },
                {
                    Failed,
                    (context, data) => CreateActivityFromString(SignInStrings.FAILED)
                },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public SignInResponses()
        {
            this.Register(new DictionaryRenderer(_responseTemplates));
        }

        private static Activity CreateActivityFromString(string message, string inputHint = InputHints.IgnoringInput)
        {
            return MessageFactory.Text(message, ssml: message, inputHint:inputHint);
        }
    }
}
