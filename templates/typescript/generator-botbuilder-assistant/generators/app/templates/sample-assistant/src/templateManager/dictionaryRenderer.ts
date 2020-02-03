/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TurnContext } from 'botbuilder-core';
import { ITemplateRenderer } from './templateRenderer';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export declare type TemplateFunction = (turnContext: TurnContext, data: any) => Promise<any>;

/**
 * Map of Template Ids-> Template Function()
 */
export declare type TemplateIdMap = Map<string, TemplateFunction>;

/**
 * Map of language -> template functions
 */
export declare type LanguageTemplateDictionary = Map<string, TemplateIdMap | undefined>;

/**
 * This is a simple template engine which has a resource map of template functions
 * let myTemplates  = {
 *      "en" : {
 *        "templateId": (context, data) => $"your name  is {data.name}",
 *        "templateId": (context, data) => { return new Activity(); }
 *    }`
 * }
 * }
 *  To use, simply register with templateManager
 *  templateManager.register(new DictionaryRenderer(myTemplates))
 */
export class DictionaryRenderer implements ITemplateRenderer {
    private readonly languages: LanguageTemplateDictionary;

    public constructor(templates: LanguageTemplateDictionary) {
        this.languages = templates;
    }

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    public async renderTemplate(turnContext: TurnContext, language: string, templateId: string, data: any): Promise<any> {
        const templates: TemplateIdMap | undefined = this.languages.get(language);
        if (templates !== undefined) {
            const template: TemplateFunction | undefined = templates.get(templateId);
            if (template !== undefined) {
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
                const result: Promise<any> = template(turnContext, data);
                if (result !== undefined) {
                    return result;
                }
            }
        }

        return Promise.resolve(undefined);
    }
}
