// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import {
    Activity,
    Attachment,
    CardFactory,
    CardImage,
    InputHints,
    MessageFactory,
    TurnContext } from 'botbuilder';
import { __ } from 'i18n';
import {
    DictionaryRenderer,
    LanguageTemplateDictionary,
    TemplateFunction,
    TemplateIdMap} from '../templateManager/dictionaryRenderer';
import { TemplateManager } from '../templateManager/templateManager';

export class OnboardingResponses extends TemplateManager {
    public static responseIds: {
        namePrompt: string;
        locationPrompt: string;
        haveLocation: string;
        addLinkedAccountsMessage: string;
    } = {
        namePrompt: 'namePrompt',
        locationPrompt: 'locationPrompt',
        haveLocation: 'haveLocation',
        addLinkedAccountsMessage: 'linkedAccountsInfo'
    };

    private static readonly responseTemplates: LanguageTemplateDictionary = new Map([
        ['default', <TemplateIdMap> new Map([
            [OnboardingResponses.responseIds.namePrompt, OnboardingResponses.fromResources('onboarding.namePrompt')],
            [OnboardingResponses.responseIds.locationPrompt, OnboardingResponses.fromResources('onboarding.locationPrompt')],
            [OnboardingResponses.responseIds.haveLocation, OnboardingResponses.fromResources('onboarding.haveLocation')],
            [OnboardingResponses.responseIds.addLinkedAccountsMessage,
                // tslint:disable-next-line:no-any
                (context: TurnContext, data: any): Promise<Activity> => OnboardingResponses.buildLinkedAccountsCard(context, data)]
        ])]
   ]);

   // Constructor
    constructor() {
        super();
        this.register(new DictionaryRenderer(OnboardingResponses.responseTemplates));
    }

    // tslint:disable-next-line:no-any
    public static async buildLinkedAccountsCard(turnContext: TurnContext, data: any): Promise<Activity> {
        const title: string = __('onboarding.linkedAccountsInfoTitle');
        const text: string = __('onboarding.linkedAccountsInfoBody');
        const images: (CardImage | string)[] = [
            {
                url: __('onboarding.linkedAccountsInfoUrl'),
                alt: __('onboarding.linkedAccountsInfoAlt')
            }
        ];
        const attachment: Attachment = CardFactory.heroCard(title, text, images);
        const response: Partial<Activity> = MessageFactory.attachment(attachment, title, InputHints.AcceptingInput);

        return Promise.resolve(<Activity> response);
    }

    private static fromResources(name: string): TemplateFunction {
        return (): Promise<string> => Promise.resolve(__(name));
    }

}
