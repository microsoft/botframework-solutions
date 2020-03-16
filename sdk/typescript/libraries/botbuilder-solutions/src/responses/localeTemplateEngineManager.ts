/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TemplateEngine, ActivityFactory } from 'botbuilder-lg';
import { Activity } from 'botbuilder';
import i18next from 'i18next';

/**
 * Multi locale Template Manager for language generation. This template manager will enumerate multi-locale LG files and will select
 * the appropriate template using the current culture to perform template evaluation.
 */
export class LocaleTemplateEngineManager {
    
    /**
     * PENDING - We need the library botbuilder-dialogs-adaptive for this implementation
     * private readonly languageFallbackPolicy: LanguagePolicy;
     */
    private readonly localeDefault: string;
    public templateEnginesPerLocale: Map<string, TemplateEngine> = new Map<string, TemplateEngine>();

    /**
     * Initializes a new instance of the LocaleTemplateEngineManager.
     * @param localeLGFiles - A dictionary of locale and LG file(s).
     * @param fallbackLocale The default fallback locale to use.
     */
    public constructor(localeLGFiles: Map<string, string[]>, fallbackLocale: string) {
        if (localeLGFiles === undefined) { throw new Error ('The parameter localeLGFiles is undefined'); }
        if (fallbackLocale === undefined || fallbackLocale.trim().length === 0) { throw new Error ('The parameter fallbackLocale is undefined'); }

        localeLGFiles.forEach((value: string[], key: string): void => {
            this.templateEnginesPerLocale.set(key, new TemplateEngine());
            const templateEngine: TemplateEngine | undefined = this.templateEnginesPerLocale.get(key);
            if (templateEngine) {
                templateEngine.addFiles(value);
            }
        });

        /**
        * PENDING - We need the library botbuilder-dialogs-adaptive for this implementation
        * this.languageFallbackPolicy = new LanguagePolicy();
        */
        this.localeDefault = fallbackLocale;
    }

    /**
     * Create an activity through Language Generation using the thread culture or provided override.
     * @param templateName - Langauge Generation template.
     * @param data Data for Language Generation to use during response generation.
     * @param localeOverride Optional override for locale.
     * @returns Activity
     */
    public generateActivityForLocale(templateName: string, data: Object = {} , localeOverride: string = ''): Partial<Activity> {
        if (templateName === undefined) { throw new Error('The parameter templateName is undefined'); }
    
        // By default we use the locale for the current culture, if a locale is provided then we ignore this.
        let locale: string = localeOverride.trim().length > 0 ? localeOverride : i18next.language;

        // Do we have a template engine for this locale?
        if (this.templateEnginesPerLocale.has(locale)) {   
            const templateEngine: TemplateEngine | undefined = this.templateEnginesPerLocale.get(locale);
            if (templateEngine) {
                return ActivityFactory.createActivity(templateEngine.evaluateTemplate(templateName, data));
            }
            
        } else {
            // We don't have a set of matching responses for this locale so we apply fallback policy to find options.
            
            /* PENDING - We need the library botbuilder-dialogs-adaptive for this implementation   
            let locales: string[] = this.languageFallbackPolicy.get(locale);
            {
                // If no fallback options were found then we fallback to the default and log.
                if (!this.languageFallbackPolicy.get(this.localeDefault))
                {
                    throw new Error(`No LG responses found for ${locale} or when attempting to fallback`);
                }
            }
            */ 

            // Work through the fallback hierarchy to find a response
            // locales.forEach((fallBackLocale: LanguagePolicy) => {
            //     if (this.templateEnginesPerLocale.has(fallBackLocale)){
            //         return ActivityFactory.createActivity(this.templateEnginesPerLocale.get(fallBackLocale)?.evaluateTemplate(templateName, data));
            //     }
            // });
            
        }

        throw new Error(`No LG responses found for ${ locale } or when attempting to fallback to ${ this.localeDefault }`);
    }
}
