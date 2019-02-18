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
   public static responseIds: {
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

    private static readonly responseTemplates: LanguageTemplateDictionary = new Map([
        ['default', new Map([
            [OnboardingResponses.responseIds.NamePrompt, OnboardingResponses.fromResources('onBoarding.namePrompt')],
            // tslint:disable-next-line:no-any
            [OnboardingResponses.responseIds.HaveNameMessage, async (context: TurnContext, data: any): Promise<string> => {
                const value: string = i18n.__('onBoarding.haveName');

                return value.replace('{0}', data.name);
            }],
            [OnboardingResponses.responseIds.EmailPrompt, OnboardingResponses.fromResources('onBoarding.emailPrompt')],
            // tslint:disable-next-line:no-any
            [OnboardingResponses.responseIds.HaveEmailMessage, async (context: TurnContext, data: any): Promise<string> => {
                const value: string = i18n.__('onBoarding.haveEmail');

                return value.replace('{0}', data.email);
            }],
            [OnboardingResponses.responseIds.LocationPrompt, OnboardingResponses.fromResources('onBoarding.locationPrompt')],
            // tslint:disable-next-line:no-any
            [OnboardingResponses.responseIds.HaveLocationMessage, async (context: TurnContext, data: any): Promise<string> => {
                const value: string = i18n.__('onBoarding.haveLocation');

                return value.replace('{0}', data.name)
                            .replace('{1}', data.location);
            }]
        ])]
    ]);

    constructor() {
        super();
        this.register(new DictionaryRenderer(OnboardingResponses.responseTemplates));
    }

    private static fromResources(name: string): TemplateFunction {
        return (): Promise<string> => Promise.resolve(i18n.__(name));
    }
}
