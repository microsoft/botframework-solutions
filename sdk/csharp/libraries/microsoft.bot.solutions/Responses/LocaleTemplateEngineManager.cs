// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Responses
{
    /// <summary>
    /// Multi locale Template Manager for language generation. This template manager will enumerate multi-locale LG files and will select
    /// the appropriate template using the current culture to perform template evaluation.
    /// </summary>
    public class LocaleTemplateEngineManager
    {
        private readonly LanguagePolicy languageFallbackPolicy;
        private readonly string localeDefault;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocaleTemplateEngineManager"/> class.
        /// </summary>
        /// <param name="localeLGFiles">A dictionary of locale and LG file(s).</param>
        /// <param name="fallbackLocale">The default fallback locale to use.</param>
        public LocaleTemplateEngineManager(Dictionary<string, List<string>> localeLGFiles, string fallbackLocale)
        {
            if (localeLGFiles == null)
            {
                throw new ArgumentNullException(nameof(localeLGFiles));
            }

            if (string.IsNullOrEmpty(fallbackLocale))
            {
                throw new ArgumentNullException(nameof(fallbackLocale));
            }

            foreach (KeyValuePair<string, List<string>> filesPerLocale in localeLGFiles)
            {
                foreach (string file in filesPerLocale.Value)
                {
                    TemplateEnginesPerLocale[filesPerLocale.Key] = LGParser.ParseFile(file);
                }
            }

            languageFallbackPolicy = new LanguagePolicy();
            localeDefault = fallbackLocale;
        }

        public Dictionary<string, LGFile> TemplateEnginesPerLocale { get; set; } = new Dictionary<string, LGFile>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Create an activity through Language Generation using the thread culture or provided override.
        /// </summary>
        /// <param name="templateName">Language Generation template.</param>
        /// <param name="data">Data for Language Generation to use during response generation.</param>
        /// <param name="localeOverride">Optional override for locale.</param>
        /// <returns>Activity.</returns>
        /// <remarks>
        /// The InputHint property of the returning activity is set to be null if it's acceptingInput so
        /// when the activity is being used in a prompt it'll be set to expectingInput.
        /// </remarks>
        public Activity GenerateActivityForLocale(string templateName, object data = null, string localeOverride = null)
        {
            if (templateName == null)
            {
                throw new ArgumentNullException(nameof(templateName));
            }

            // By default we use the locale for the current culture, if a locale is provided then we ignore this.
            var locale = localeOverride ?? CultureInfo.CurrentUICulture.Name;

            // Do we have a template engine for this locale?
            if (TemplateEnginesPerLocale.ContainsKey(locale))
            {
                var activity = ActivityFactory.CreateActivity(TemplateEnginesPerLocale[locale].EvaluateTemplate(templateName, data).ToString());

                // Set the inputHint to null when it's acceptingInput so prompt can override it when expectingInput
                if (activity.InputHint == InputHints.AcceptingInput)
                {
                    activity.InputHint = null;
                }

                return activity;
            }
            else
            {
                // We don't have a set of matching responses for this locale so we apply fallback policy to find options.
                languageFallbackPolicy.TryGetValue(locale, out string[] locales);
                {
                    // If no fallback options were found then we fallback to the default and log.
                    if (!languageFallbackPolicy.TryGetValue(localeDefault, out locales))
                    {
                        throw new Exception($"No LG responses found for {locale} or when attempting to fallback to '{localeDefault}'");
                    }
                }

                // Work through the fallback hierarchy to find a response
                foreach (var fallBackLocale in locales)
                {
                    if (TemplateEnginesPerLocale.ContainsKey(fallBackLocale))
                    {
                        var activity = ActivityFactory.CreateActivity(TemplateEnginesPerLocale[fallBackLocale].EvaluateTemplate(templateName, data).ToString());

                        // Set the inputHint to null when it's acceptingInput so prompt can override it when expectingInput
                        if (activity.InputHint == InputHints.AcceptingInput)
                        {
                            activity.InputHint = null;
                        }

                        return activity;
                    }
                }
            }

            throw new Exception($"No LG responses found for {locale} or when attempting to fallback to '{localeDefault}'");
        }
    }
}
