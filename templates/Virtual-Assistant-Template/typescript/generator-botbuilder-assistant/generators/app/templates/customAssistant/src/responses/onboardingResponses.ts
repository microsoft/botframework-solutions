/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TurnContext } from 'botbuilder';
import i18next from 'i18next';
import {
    DictionaryRenderer,
    LanguageTemplateDictionary,
    TemplateFunction } from '../templateManager/dictionaryRenderer';
import { TemplateManager } from '../templateManager/templateManager';

export class OnboardingResponses extends TemplateManager {

    // Declare here the type of properties and the prompts
    public static responseIds: {
        emailPrompt : string;
        haveEmailMessage : string;
        haveNameMessage : string;
        haveLocationMessage: string;
        locationPrompt: string;
        namePrompt: string;
    } = {
        emailPrompt : 'emailPrompt',
        haveEmailMessage : 'haveEmail',
        haveNameMessage : 'haveName',
        haveLocationMessage: 'haveLocation',
        locationPrompt: 'locationPrompt',
        namePrompt: 'namePrompt'
    };

    // Declare the responses map prompts
    private static readonly responseTemplates: LanguageTemplateDictionary = new Map([
        ['default', new Map([
            [OnboardingResponses.responseIds.emailPrompt, OnboardingResponses.fromResources('onboarding.emailPrompt')],
            // tslint:disable-next-line: no-any
            [OnboardingResponses.responseIds.haveEmailMessage, async (context: TurnContext, data: any): Promise<string> => {
                const value: string = i18next.t('onboarding.haveEmail');

                // tslint:disable-next-line: no-unsafe-any
                return value.replace('{0}', data.email);
            }],
            // tslint:disable-next-line: no-any
            [OnboardingResponses.responseIds.haveLocationMessage, async (context: TurnContext, data: any): Promise<string> => {
                const value: string = i18next.t('onboarding.haveLocation');

                // tslint:disable-next-line: no-unsafe-any
                return value.replace('{0}', data.name)
                // tslint:disable-next-line: no-unsafe-any
                .replace('{1}', data.location);
            }],
            //tslint:disable-next-line: no-any
            [OnboardingResponses.responseIds.haveNameMessage, async (context: TurnContext, data: any): Promise<string> => {
                const value: string = i18next.t('onboarding.haveName');

                // tslint:disable-next-line: no-unsafe-any
                return value.replace('{0}', data.name);
            }],
            [OnboardingResponses.responseIds.namePrompt, OnboardingResponses.fromResources('onboarding.namePrompt')],
            [OnboardingResponses.responseIds.locationPrompt, OnboardingResponses.fromResources('onboarding.locationPrompt')]
        ])]
    ]);

    // Initialize the responses class properties
    constructor() {
        super();
        this.register(new DictionaryRenderer(OnboardingResponses.responseTemplates));
    }

    private static fromResources(name: string): TemplateFunction {
        return (): Promise<string> => Promise.resolve(i18next.t(name));
    }
}
