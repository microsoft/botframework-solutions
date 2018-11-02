// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using $safeprojectname$.Dialogs.Onboarding.Resources;
using $safeprojectname$.Extensions;
using Microsoft.Bot.Builder.TemplateManager;

namespace $safeprojectname$
{
    public class OnboardingResponses : TemplateManagerWithVoice
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
                    (context, data) => OnboardingStrings.NAME_PROMPT
                },
                {
                    _haveName,
                    (context, data) => string.Format(OnboardingStrings.HAVE_NAME, data.name)
                },
                {
                    _emailPrompt,
                    (context, data) => OnboardingStrings.EMAIL_PROMPT
                },
                {
                    _haveEmail,
                    (context, data) => string.Format(OnboardingStrings.HAVE_EMAIL, data.email)
                },
                {
                    _locationPrompt,
                    (context, data) => OnboardingStrings.LOCATION_PROMPT
                },
                {
                    _haveLocation,
                    (context, data) => string.Format(OnboardingStrings.HAVE_LOCATION, data.Name, data.Location)
                },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public OnboardingResponses()
        {
            this.Register(new DictionaryRenderer(_responseTemplates));
        }
    }
}
