/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    Activity,
    Attachment,
    CardFactory,
    InputHints,
    MessageFactory,
    TurnContext } from 'botbuilder';
import { ActionTypes } from 'botframework-schema';
import i18next from 'i18next';
import { join } from 'path';
import {
    DictionaryRenderer,
    LanguageTemplateDictionary,
    TemplateFunction } from '../services/dictionaryRenderer';
import { TemplateManager } from '../services/templateManager';

export class MainResponses extends TemplateManager {

    // Declare here the type of properties and the prompts
    public static responseIds: {
        cancelled: string;
        completed: string;
        confused: string;
        greeting: string;
        help: string;
        newUserGreeting: string;
        returningUserGreeting: string;
    } = {
        cancelled: 'cancelled',
        completed: 'completed',
        confused: 'confused',
        greeting: 'greeting',
        help: 'help',
        newUserGreeting: 'newUser',
        returningUserGreeting: 'returningUser'
    };

    // Declare the responses map prompts
    private static readonly responseTemplates: LanguageTemplateDictionary = new Map([
        ['default', new Map([
            [MainResponses.responseIds.cancelled, MainResponses.fromResources('main.cancelled')],
            [MainResponses.responseIds.completed, MainResponses.fromResources('main.completed')],
            [MainResponses.responseIds.confused, MainResponses.fromResources('main.confused')],
            [MainResponses.responseIds.greeting, MainResponses.fromResources('main.greeting')],
            [MainResponses.responseIds.help, MainResponses.buildHelpCard],
            [MainResponses.responseIds.newUserGreeting, MainResponses.buildNewUserGreetingCard],
            [MainResponses.responseIds.returningUserGreeting, MainResponses.buildReturningUserGreetingCard]
        ])]
    ]);

    // Initialize the responses class properties
    constructor() {
        super();
        this.register(new DictionaryRenderer(MainResponses.responseTemplates));
    }

    // tslint:disable-next-line:no-any
    public static buildNewUserGreetingCard(turnContext: TurnContext, data: any): Promise<any> {
        const introFileName: string = i18next.t('main.introGreetingFile');
        const introPath: string = join(__dirname, '..', 'content', introFileName);
        // tslint:disable-next-line:no-any non-literal-require
        const introCard: any = require(introPath);
        const attachment: Attachment = CardFactory.adaptiveCard(introCard);
        const response: Partial<Activity> = MessageFactory.attachment(attachment, '', attachment.content.speak, InputHints.AcceptingInput);

        response.suggestedActions = {
            actions: [
                {
                    title: i18next.t('main.helpBtnText1'),
                    type: ActionTypes.ImBack,
                    value: i18next.t('main.helpBtnValue1')
                },
                {
                    title: i18next.t('main.helpBtnText2'),
                    type: ActionTypes.ImBack,
                    value: i18next.t('main.helpBtnValue2')
                },
                {
                    title: i18next.t('main.helpBtnText3'),
                    type: ActionTypes.OpenUrl,
                    value: i18next.t('main.helpBtnValue3')
                }
            ],
            to: []
        };

        return Promise.resolve(response);
    }

    // tslint:disable-next-line:no-any
    public static buildReturningUserGreetingCard(turnContext: TurnContext, data: any): Promise<any> {
        const introFileName: string = i18next.t('main.introReturningFile');
        const introPath: string = join(__dirname, '..', 'content', introFileName);
        // tslint:disable-next-line:no-any non-literal-require
        const introCard: any = require(introPath);
        const attachment: Attachment = CardFactory.adaptiveCard(introCard);
        const response: Partial<Activity> = MessageFactory.attachment(attachment, '', attachment.content.speak, InputHints.AcceptingInput);

        response.suggestedActions = {
            actions: [
                {
                    title: i18next.t('main.helpBtnText1'),
                    type: ActionTypes.ImBack,
                    value: i18next.t('main.helpBtnValue1')
                },
                {
                    title: i18next.t('main.helpBtnText2'),
                    type: ActionTypes.ImBack,
                    value: i18next.t('main.helpBtnValue2')
                },
                {
                    title: i18next.t('main.helpBtnText3'),
                    type: ActionTypes.OpenUrl,
                    value: i18next.t('main.helpBtnValue3')
                }
            ],
            to: []
        };

        return Promise.resolve(response);
    }

    // tslint:disable-next-line:no-any
    public static buildHelpCard(turnContext: TurnContext, data: any): Promise<any> {
        const title: string = i18next.t('main.helpTitle');
        const text: string = i18next.t('main.helpText');
        const attachment: Attachment = CardFactory.heroCard(title, text);
        const response: Partial<Activity> = MessageFactory.attachment(attachment, text, InputHints.AcceptingInput);

        response.suggestedActions = {
            actions: [
            {
                title: i18next.t('main.helpBtnText1'),
                type: ActionTypes.ImBack,
                value: i18next.t('main.helpBtnValue1')
            },
            {
                title: i18next.t('main.helpBtnText2'),
                type: ActionTypes.ImBack,
                value: i18next.t('main.helpBtnValue2')
            },
            {
                title: i18next.t('main.helpBtnText3'),
                type: ActionTypes.OpenUrl,
                value: i18next.t('main.helpBtnValue3')
            }
            ],
            to: []
        };

        return Promise.resolve(response);
    }

    private static fromResources(name: string): TemplateFunction {
        return (): Promise<string> => Promise.resolve(i18next.t(name));
    }
}
