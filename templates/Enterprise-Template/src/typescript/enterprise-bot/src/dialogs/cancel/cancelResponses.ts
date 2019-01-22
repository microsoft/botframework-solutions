// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import * as i18n from 'i18n';
import {
    DictionaryRenderer,
    LanguageTemplateDictionary,
    TemplateFunction } from '../templateManager/dictionaryRenderer';
import { TemplateManager } from '../templateManager/templateManager';

export class CancelResponses extends TemplateManager {

    // Fields
    public static RESPONSE_IDS: {
        CancelPrompt: string;
        CancelConfirmedMessage: string;
        CancelDeniedMessage: string;
    }  = {
        CancelPrompt:  'cancelPrompt',
        CancelConfirmedMessage: 'cancelConfirmed',
        CancelDeniedMessage: 'cancelDenied'
    };

    private static readonly RESPONSE_TEMPLATES: LanguageTemplateDictionary = new Map([
        ['default', new Map([
            [CancelResponses.RESPONSE_IDS.CancelPrompt, CancelResponses.fromResources('cancel.prompt')],
            [CancelResponses.RESPONSE_IDS.CancelConfirmedMessage, CancelResponses.fromResources('cancel.confirmed')],
            [CancelResponses.RESPONSE_IDS.CancelDeniedMessage, CancelResponses.fromResources('cancel.denied')]
        ])]
    ]);

    constructor() {
        super();
        this.register(new DictionaryRenderer(CancelResponses.RESPONSE_TEMPLATES));
    }

    private static fromResources(name: string): TemplateFunction {
        return (): Promise<string> => Promise.resolve(i18n.__(name));
    }
}
