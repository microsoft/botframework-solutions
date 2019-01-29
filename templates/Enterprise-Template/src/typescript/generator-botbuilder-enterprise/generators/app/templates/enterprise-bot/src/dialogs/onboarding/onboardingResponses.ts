// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { TurnContext } from 'botbuilder';
import * as i18n from 'i18n';
import {
    DictionaryRenderer,
    LanguageTemplateDictionary,
    TemplateFunction } from '../templateManager/dictionaryRenderer';
import { TemplateManager } from '../templateManager/templateManager';

export class OnboardingResponses extends TemplateManager {

   // Fields
   public static RESPONSE_IDS: {
    EmailPrompt: string;
    HaveEmailMessage: string;
    HaveNameMessage: string;
    HaveLocationMessage: string;
    LocationPrompt: string;
    NamePrompt: string;
    } = {
        EmailPrompt:  'emailPrompt',
        HaveEmailMessage: 'haveEmail',
        HaveNameMessage: 'haveName',
        HaveLocationMessage: 'haveLocation',
        LocationPrompt: 'locationPrompt',
        NamePrompt: 'namePrompt'
    };

    private static readonly RESPONSE_TEMPLATES: LanguageTemplateDictionary = new Map([
        ['default', new Map([
            [OnboardingResponses.RESPONSE_IDS.NamePrompt, OnboardingResponses.fromResources('onBoarding.namePrompt')],
            [OnboardingResponses.RESPONSE_IDS.HaveNameMessage, async (context: TurnContext, data: any): Promise<string> => {
                const value: string = i18n.__('onBoarding.haveName');

                return value.replace('{0}', data.name);
            }],
            [OnboardingResponses.RESPONSE_IDS.EmailPrompt, OnboardingResponses.fromResources('onBoarding.emailPrompt')],
            [OnboardingResponses.RESPONSE_IDS.HaveEmailMessage, async (context: TurnContext, data: any): Promise<string> => {
                const value: string = i18n.__('onBoarding.haveEmail');

                return value.replace('{0}', data.email);
            }],
            [OnboardingResponses.RESPONSE_IDS.LocationPrompt, OnboardingResponses.fromResources('onBoarding.locationPrompt')],
            [OnboardingResponses.RESPONSE_IDS.HaveLocationMessage, async (context: TurnContext, data: any): Promise<string> => {
                const value: string = i18n.__('onBoarding.haveLocation');

                return value.replace('{0}', data.name)
                            .replace('{1}', data.location);
            }]
        ])]
    ]);

    constructor() {
        super();
        this.register(new DictionaryRenderer(OnboardingResponses.RESPONSE_TEMPLATES));
    }

    private static fromResources(name: string): TemplateFunction {
        return (): Promise<string> => Promise.resolve(i18n.__(name));
    }
}
