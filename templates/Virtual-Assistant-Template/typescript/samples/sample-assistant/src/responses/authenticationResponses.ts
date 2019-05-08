/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import i18next from 'i18next';
import {
    DictionaryRenderer,
    LanguageTemplateDictionary,
    TemplateFunction,
    TemplateIdMap } from '../services/dictionaryRenderer';
import { TemplateManager } from '../services/templateManager';

export class AuthenticationResponses extends TemplateManager {

    // Declare here the type of properties and the prompts
    public static responseIds: {
        loginPrompt: string;
        succeededMessage: string;
        failedMessage: string;
    } = {
        loginPrompt: 'loginPrompt',
        succeededMessage: 'succeededMessage',
        failedMessage: 'failedMessage'
    };

    // Declare the responses map prompts
    private static readonly responseTemplates: LanguageTemplateDictionary = new Map([
        ['default', new Map([
            [AuthenticationResponses.responseIds.loginPrompt, AuthenticationResponses.fromResources('authentication.prompt')],
            //tslint:disable-next-line: no-any
            [AuthenticationResponses.responseIds.succeededMessage, async (data: any): Promise<string> => {
                const value: string = i18next.t('authentication.succeeded');

                // tslint:disable-next-line: no-unsafe-any
                return value.replace('{0}', data.name);
            }],
            [AuthenticationResponses.responseIds.failedMessage, AuthenticationResponses.fromResources('authentication.failed')]
        ])]
    ]);

    // Initialize the responses class properties
    constructor() {
        super();
        this.register(new DictionaryRenderer(AuthenticationResponses.responseTemplates));
    }

    private static fromResources(name: string): TemplateFunction {
        return (): Promise<string> => Promise.resolve(i18next.t(name));
    }
}
