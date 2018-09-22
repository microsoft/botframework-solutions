// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.TemplateManager;

namespace Microsoft.Bot.Solutions.Dialogs.Onboard
{
    public class OnboardingView : TemplateManager
    {
        public const string INTRO = "OnboardingDialog.Intro";
        public const string NAME_PROMPT = "OnboardingDialog.NamePrompt";
        public const string HAVE_NAME = "OnboardingDialog.HaveName";
        public const string PRIMARY_EMAIL_PROMPT = "OnboardingDialog.PrimaryEmailPrompt";
        public const string SECONDARY_EMAIL_PROMPT = "OnboardingDialog.SecondaryEmailPrompt";
        public const string HAVE_EMAIL = "OnboardingDialog.HaveEmail";
        public const string HAVE_SECONDARY_EMAIL = "OnboardingDialog.HaveSecondaryEmail";
        public const string LOCATION_PROMPT = "OnboardingDialog.LocationPrompt";
        public const string HAVE_LOCATION = "OnboardingDialog.HaveLocation";

        private static LanguageTemplateDictionary responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { NAME_PROMPT, (context, data) => "What is your name?" },
                { HAVE_NAME, (context, data) => $"Hi, {data.name}!" },
                { PRIMARY_EMAIL_PROMPT, (context, data) => "What is your primary mail address?" },
                { SECONDARY_EMAIL_PROMPT, (context, data) => "What is your secondary mail address?" },
                { HAVE_EMAIL, (context, data) => $"Got it. I've added {data.email} as your primary contact address." },
                { HAVE_SECONDARY_EMAIL, (context, data) => $"Got it. I've added {data.email} as your secondary contact address." },
                { LOCATION_PROMPT, (context, data) => "Finally, where are you located?" },
                { HAVE_LOCATION, (context, data) => $"Thanks, {data.name}. I've added {data.location} as your primary location. You're all set up!" },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="OnboardingView"/> class.
        /// </summary>
        public OnboardingView()
        {
            this.Register(new DictionaryRenderer(responseTemplates));
        }
    }
}
