/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import i18next from 'i18next';
import {
    DictionaryRenderer,
    LanguageTemplateDictionary,
    TemplateFunction } from '../templateManager/dictionaryRenderer';
import { TemplateManager } from '../templateManager/templateManager';

export class CancelResponses extends TemplateManager {

    // Declare here the type of properties and the prompts
    public static responseIds: {
        cancelPrompt: string;
        cancelConfirmedMessage: string;
        cancelDeniedMessage: string;
        nothingToCancelMessage: string;
    } = {
        cancelPrompt: 'cancelPrompt',
        cancelConfirmedMessage: 'cancelConfirmed',
        cancelDeniedMessage: 'cancelDenied',
        nothingToCancelMessage: 'nothingToCancel'
    };

    // Declare the responses map prompts
    private static readonly responseTemplates: LanguageTemplateDictionary = new Map([
        ['default', new Map([
            [CancelResponses.responseIds.cancelConfirmedMessage, CancelResponses.fromResources('cancel.cancelConfirmed')],
            [CancelResponses.responseIds.cancelDeniedMessage, CancelResponses.fromResources('cancel.cancelDenied')],
            [CancelResponses.responseIds.cancelPrompt, CancelResponses.fromResources('cancel.cancelPrompt')],
            [CancelResponses.responseIds.nothingToCancelMessage, CancelResponses.fromResources('cancel.nothingToCancel')]
        ])]
    ]);

    // Initialize the responses class properties
    constructor() {
        super();
        this.register(new DictionaryRenderer(CancelResponses.responseTemplates));
    }

    private static fromResources(name: string): TemplateFunction {
        return (): Promise<string> => Promise.resolve(i18next.t(name));
    }
}
