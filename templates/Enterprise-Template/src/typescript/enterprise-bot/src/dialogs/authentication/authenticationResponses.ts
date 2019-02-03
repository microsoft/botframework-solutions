// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { TurnContext } from 'botbuilder';
import * as i18n from 'i18n';
import {
    DictionaryRenderer,
    LanguageTemplateDictionary,
    TemplateFunction } from '../templateManager/dictionaryRenderer';
import { TemplateManager } from '../templateManager/templateManager';

export class AuthenticationResponses extends TemplateManager {

    // Fields
    public static RESPONSE_IDS: {
        LoginPrompt: string;
        SucceededMessage: string;
        FailedMessage: string;
    } = {
        LoginPrompt:  'loginPrompt',
        SucceededMessage: 'succeededMessage',
        FailedMessage: 'failedMessage'
    };

    private static readonly RESPONSE_TEMPLATES: LanguageTemplateDictionary = new Map([
        ['default', new Map([
            [AuthenticationResponses.RESPONSE_IDS.LoginPrompt, AuthenticationResponses.fromResources('authentication.prompt')],
            // tslint:disable-next-line:no-any
            [AuthenticationResponses.RESPONSE_IDS.SucceededMessage, async (context: TurnContext, data: any): Promise<string> => {
                const value: string = i18n.__('authentication.succeeded');
                const firstName: string = data.name.substring(data.name.indexOf(',') + 1);

                return value.replace('{0}', firstName);
            }],
            [AuthenticationResponses.RESPONSE_IDS.FailedMessage, AuthenticationResponses.fromResources('authentication.failed')]
        ])]
    ]);

    constructor() {
        super();
        this.register(new DictionaryRenderer(AuthenticationResponses.RESPONSE_TEMPLATES));
    }

    private static fromResources(name: string): TemplateFunction {
        return (): Promise<string> => Promise.resolve(i18n.__(name));
    }
}
