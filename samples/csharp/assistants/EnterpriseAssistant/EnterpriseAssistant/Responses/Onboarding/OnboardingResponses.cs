// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;

namespace VirtualAssistant.Responses.Onboarding
{
    public class OnboardingResponses : TemplateManager
    {
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                {
                    ResponseIds.EmailPrompt,
                    (context, data) =>
                    MessageFactory.Text(
                        text: OnboardingStrings.EMAIL_PROMPT,
                        ssml: OnboardingStrings.EMAIL_PROMPT,
                        inputHint: InputHints.ExpectingInput)
                },
                {
                    ResponseIds.HaveEmailMessage,
                    (context, data) =>
                    MessageFactory.Text(
                        text: string.Format(OnboardingStrings.HAVE_EMAIL, data.email),
                        ssml: string.Format(OnboardingStrings.HAVE_EMAIL, data.email),
                        inputHint: InputHints.IgnoringInput)
                },
                {
                    ResponseIds.HaveLocationMessage,
                    (context, data) =>
                    MessageFactory.Text(
                        text: string.Format(OnboardingStrings.HAVE_LOCATION, data.Name, data.Location),
                        ssml: string.Format(OnboardingStrings.HAVE_LOCATION, data.Name, data.Location),
                        inputHint: InputHints.IgnoringInput)
                },
                {
                    ResponseIds.HaveNameMessage,
                    (context, data) =>
                    MessageFactory.Text(
                        text: string.Format(OnboardingStrings.HAVE_NAME, data.name),
                        ssml: string.Format(OnboardingStrings.HAVE_NAME, data.name),
                        inputHint: InputHints.IgnoringInput)
                },
                {
                    ResponseIds.NamePrompt,
                    (context, data) =>
                    MessageFactory.Text(
                        text: OnboardingStrings.NAME_PROMPT,
                        ssml: OnboardingStrings.NAME_PROMPT,
                        inputHint: InputHints.ExpectingInput)
                },
                {
                    ResponseIds.LocationPrompt,
                    (context, data) =>
                    MessageFactory.Text(
                        text: OnboardingStrings.LOCATION_PROMPT,
                        ssml: OnboardingStrings.LOCATION_PROMPT,
                        inputHint: InputHints.ExpectingInput)
                }
            }
        };

        public OnboardingResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        public class ResponseIds
        {
            public const string EmailPrompt = "emailPrompt";
            public const string HaveEmailMessage = "haveEmail";
            public const string HaveNameMessage = "haveName";
            public const string HaveLocationMessage = "haveLocation";
            public const string LocationPrompt = "locationPrompt";
            public const string NamePrompt = "namePrompt";
        }
    }
}
