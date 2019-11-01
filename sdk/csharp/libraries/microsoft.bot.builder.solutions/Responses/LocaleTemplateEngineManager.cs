// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Schema;
using ActivityBuilder = Microsoft.Bot.Builder.Dialogs.Adaptive.Generators.ActivityGenerator;

namespace Microsoft.Bot.Builder.Solutions.Responses
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
                TemplateEnginesPerLocale[filesPerLocale.Key] = new TemplateEngine();
                TemplateEnginesPerLocale[filesPerLocale.Key].AddFiles(filesPerLocale.Value);
            }

            languageFallbackPolicy = new LanguagePolicy();
            localeDefault = fallbackLocale;
        }

        public Dictionary<string, TemplateEngine> TemplateEnginesPerLocale { get; set; } = new Dictionary<string, TemplateEngine>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Create an activity through Language Generation using the thread culture or provided override.
        /// </summary>
        /// <param name="templateName">Langauge Generation template.</param>
        /// <param name="data">Data for Language Generation to use during response generation.</param>
        /// <param name="localeOverride">Optional override for locale.</param>
        /// <returns>Activity.</returns>
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
                return ActivityBuilder.GenerateFromLG(TemplateEnginesPerLocale[locale].EvaluateTemplate(templateName, data));
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
                        return ActivityBuilder.GenerateFromLG(TemplateEnginesPerLocale[fallBackLocale].EvaluateTemplate(templateName, data));
                    }
                }
            }

            throw new Exception($"No LG responses found for {locale} or when attempting to fallback to '{localeDefault}'");
        }
    }
}
