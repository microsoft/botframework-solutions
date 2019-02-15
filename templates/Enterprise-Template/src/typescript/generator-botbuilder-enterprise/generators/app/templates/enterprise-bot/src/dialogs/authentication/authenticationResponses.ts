// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import * as i18n from 'i18n';
import {
    DictionaryRenderer,
    LanguageTemplateDictionary,
    TemplateFunction } from '../templateManager/dictionaryRenderer';
import { TemplateManager } from '../templateManager/templateManager';

export class AuthenticationResponses extends TemplateManager {

    // Fields
    public static responseIds: {
        LoginPrompt: string;
        SucceededMessage: string;
        FailedMessage: string;
    } = {
        LoginPrompt:  'loginPrompt',
        SucceededMessage: 'succeededMessage',
        FailedMessage: 'failedMessage'
    };

    private static readonly responseTemplates: LanguageTemplateDictionary = new Map([
        ['default', new Map([
            [AuthenticationResponses.responseIds.LoginPrompt, AuthenticationResponses.fromResources('authentication.prompt')],
            // tslint:disable-next-line:no-any
            [AuthenticationResponses.responseIds.SucceededMessage, async (data: any): Promise<string> => {
                const value: string = i18n.__('authentication.succeeded');

                return value.replace('{0}', data.name);
            }],
            [AuthenticationResponses.responseIds.FailedMessage, AuthenticationResponses.fromResources('authentication.failed')]
        ])]
    ]);

    constructor() {
        super();
        this.register(new DictionaryRenderer(AuthenticationResponses.responseTemplates));
    }

    private static fromResources(name: string): TemplateFunction {
        return (): Promise<string> => Promise.resolve(i18n.__(name));
    }
}
