// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using VirtualAssistant.Dialogs.Onboarding.Resources;

namespace VirtualAssistant.Dialogs.Onboarding
{
    public class OnboardingResponses : TemplateManager
    {
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                {
                    ResponseIds.NamePrompt,
                    (context, data) =>
                    MessageFactory.Text(
                        text: OnboardingStrings.NAME_PROMPT,
                        ssml: OnboardingStrings.NAME_PROMPT,
                        inputHint: InputHints.ExpectingInput)
                },
                {
                    ResponseIds.Greeting,
                    (context, data) =>
                    MessageFactory.Text(
                        text: string.Format(OnboardingStrings.GREETING, data.Name),
                        ssml: string.Format(OnboardingStrings.GREETING, data.Name),
                        inputHint: InputHints.IgnoringInput)
                }
            }
        };

        public OnboardingResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        public class ResponseIds
        {
            public const string NamePrompt = "namePrompt";
            public const string Greeting = "greeting";
        }
    }
}