// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using $safeprojectname$.Dialogs.Onboarding.Resources;
using $safeprojectname$.Extensions;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;

namespace $safeprojectname$.Dialogs.Onboarding
{
    public class OnboardingResponses : TemplateManager
    {
        public const string _namePrompt = "namePrompt";
        public const string _haveName = "haveName";
        public const string _emailPrompt = "emailPrompt";
        public const string _haveEmail = "haveEmail";
        public const string _locationPrompt = "locationPrompt";
        public const string _haveLocation = "haveLocation";

        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                {
                    _namePrompt,
                    (context, data) => CreateActivityFromString(OnboardingStrings.NAME_PROMPT, InputHints.ExpectingInput)
                },
                {
                    _haveName,
                    (context, data) => CreateActivityFromString(string.Format(OnboardingStrings.HAVE_NAME, data.name))
                },
                {
                    _emailPrompt,
                    (context, data) => CreateActivityFromString(OnboardingStrings.EMAIL_PROMPT, InputHints.ExpectingInput)
                },
                {
                    _haveEmail,
                    (context, data) => CreateActivityFromString(string.Format(OnboardingStrings.HAVE_EMAIL, data.email))
                },
                {
                    _locationPrompt,
                    (context, data) => CreateActivityFromString(OnboardingStrings.LOCATION_PROMPT, InputHints.ExpectingInput)
                },
                {
                    _haveLocation,
                    (context, data) => CreateActivityFromString(string.Format(OnboardingStrings.HAVE_LOCATION, data.Name, data.Location))
                },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public OnboardingResponses()
        {
            this.Register(new DictionaryRenderer(_responseTemplates));
        }

        private static Activity CreateActivityFromString(string message, string inputHint = InputHints.IgnoringInput)
        {
            return MessageFactory.Text(message, ssml: message, inputHint:inputHint);
        }
    }
}
