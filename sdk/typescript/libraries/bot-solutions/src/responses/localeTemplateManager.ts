/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { MultiLanguageLG } from 'botbuilder-lg';
import { Activity, ActivityFactory } from 'botbuilder';

/**
 * Multi locale Template Manager for language generation. This template manager will enumerate multi-locale LG files and will select
 * the appropriate template using the current culture to perform template evaluation.
 */
export class LocaleTemplateManager extends MultiLanguageLG {
    
    private readonly fallbackLocale: string | undefined;

    /**
     * Initializes a new instance of the LocaleTemplateEngineManager.
     * @param localeTemplateFiles - A dictionary of locale and LG file.
     * @param fallbackLocale The default fallback locale to use.
     */
    public constructor(localeTemplateFiles: Map<string, string>, fallbackLocale: string | undefined) {
        super(undefined, fallbackLocale === undefined ? localeTemplateFiles : new Map<string, string>([...localeTemplateFiles, ['', localeTemplateFiles.get(fallbackLocale) || '' ]]));
        // only throw when fallbackLocale is empty string
        if (fallbackLocale !== undefined && fallbackLocale.trim().length === 0) {
            throw new Error(`'fallbackLocale' shouldn't be empty string. If you don't want to set it, please set it to undefined.`);
        }

        this.fallbackLocale = fallbackLocale;
    }

    /**
     * Create an activity through Language Generation using the thread culture or provided override.
     * @param templateName - Langauge Generation template.
     * @param data Data for Language Generation to use during response generation.
     * @param localeOverride Optional override for locale.
     * @returns Activity
     */
    public generateActivityForLocale(templateName: string, locale: string | undefined, data: Object = {}): Partial<Activity> {

        if (templateName === undefined) {
            throw new Error(`Argument 'templateName' cannot be undefined.`);
        }

        locale = locale || this.fallbackLocale;

        if (locale === undefined || locale.trim().length === 0) {
            throw new Error(`Argument 'locale' cannot be undefined or empty.`);
        }
        
        return ActivityFactory.fromObject(this.generate(templateName, data, locale));
    }
}
