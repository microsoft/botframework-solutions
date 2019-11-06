/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    Activity,
    CardFactory,
    TurnContext } from 'botbuilder';
import { ActivityExtensions } from 'botbuilder-solutions';
import { ActionTypes } from 'botframework-schema';
import i18next from 'i18next';
import {
    DictionaryRenderer,
    LanguageTemplateDictionary,
    TemplateIdMap } from '../templateManager/dictionaryRenderer';
import { TemplateManager } from '../templateManager/templateManager';

export class EscalateResponses extends TemplateManager {

    // Declare here the type of properties and the prompts
    public static responseIds: {
        sendPhoneMessage: string;
    } = {
        sendPhoneMessage: 'sendPhoneMessage'
    };

    // Declare the responses map prompts
    private static readonly responseTemplates: LanguageTemplateDictionary = new Map([
        ['default', new Map([
            [
                EscalateResponses.responseIds.sendPhoneMessage,
                // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/tslint/config
                (context: TurnContext, data: any): Promise<Activity> => EscalateResponses.buildEscalateCard(context, data)
            ]
        ]) as TemplateIdMap]
    ]);

    // Initialize the responses class properties
    public constructor() {
        super();
        this.register(new DictionaryRenderer(EscalateResponses.responseTemplates));
    }

    // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/tslint/config
    public static async buildEscalateCard(turnContext: TurnContext, data: any): Promise<Activity> {

        const response: Activity = ActivityExtensions.createReply(turnContext.activity);
        const text: string = i18next.t('escalate.phoneInfo');
        response.attachments = [CardFactory.heroCard(
            text,
            undefined,
            [
                {
                    title: i18next.t('escalate.btnText1'),
                    type: ActionTypes.OpenUrl,
                    value: i18next.t('escalate.btnValue1')
                },
                {
                    title: i18next.t('escalate.btnText2'),
                    type: ActionTypes.OpenUrl,
                    value: i18next.t('escalate.btnValue2')
                }
            ]
        )];

        return Promise.resolve(response);
    }
}
