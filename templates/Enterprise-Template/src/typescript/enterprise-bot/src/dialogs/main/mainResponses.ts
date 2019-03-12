// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import {
    ActionTypes,
    Activity,
    Attachment,
    CardFactory,
    InputHints,
    MessageFactory,
    TurnContext } from 'botbuilder';
import * as i18n from 'i18n';
import {
    DictionaryRenderer,
    LanguageTemplateDictionary,
    TemplateFunction } from '../templateManager/dictionaryRenderer';
import { TemplateManager } from '../templateManager/templateManager';

export class MainResponses extends TemplateManager {

    // Fields
    public static responseIds: {
        Cancelled: string;
        Completed: string;
        Confused: string;
        Greeting: string;
        Help: string;
        Intro: string;
    } = {
        Cancelled: 'cancelled',
        Completed: 'completed',
        Confused: 'confused',
        Greeting:  'greeting',
        Help:  'help',
        Intro:  'intro'
    };
    private static readonly responseTemplates: LanguageTemplateDictionary = new Map([
        ['default', new Map([
            [MainResponses.responseIds.Cancelled, MainResponses.fromResources('main.cancelled')],
            [MainResponses.responseIds.Completed, MainResponses.fromResources('main.completed')],
            [MainResponses.responseIds.Confused, MainResponses.fromResources('main.confused')],
            [MainResponses.responseIds.Greeting, MainResponses.fromResources('main.greeting')],
            [MainResponses.responseIds.Help,
                // tslint:disable-next-line:no-any
                (context: TurnContext, data: any): Promise<Activity> => MainResponses.buildHelpCard(context, data)],
            [MainResponses.responseIds.Intro,
                // tslint:disable-next-line:no-any
                (context: TurnContext, data: any): Promise<Activity> => MainResponses.buildIntroCard(context, data)]
        ])]
    ]);

    constructor() {
        super();
        this.register(new DictionaryRenderer(MainResponses.responseTemplates));
    }

    // tslint:disable-next-line:no-any
    public static buildIntroCard(turnContext: TurnContext, data: any): Promise<Activity> {
        const introPath: string = i18n.__('main.introPath');
        // tslint:disable-next-line:no-any non-literal-require
        const introCard: any = require(introPath);
        const attachment: Attachment = CardFactory.adaptiveCard(introCard);
        const response: Partial<Activity> = MessageFactory.attachment(attachment, '', attachment.content.speak, InputHints.AcceptingInput);

        response.suggestedActions = {
            actions: [
                {
                    title: i18n.__('main.helpBtnText1'),
                    type: ActionTypes.ImBack,
                    value: i18n.__('main.helpBtnValue1')
                },
                {
                    title: i18n.__('main.helpBtnText2'),
                    type: ActionTypes.ImBack,
                    value: i18n.__('main.helpBtnValue2')
                },
                {
                    title: i18n.__('main.helpBtnText3'),
                    type: ActionTypes.OpenUrl,
                    value: i18n.__('main.helpBtnValue3')
                }
            ],
            to: []
        };

        return Promise.resolve(<Activity> response);
    }

    // tslint:disable-next-line:no-any
    public static async buildHelpCard(turnContext: TurnContext, data: any): Promise<Activity> {
        const title: string = i18n.__('main.helpTitle');
        const text: string = i18n.__('main.helpText');
        const attachment: Attachment = CardFactory.heroCard(title, text);
        const response: Partial<Activity> = MessageFactory.attachment(attachment, text, InputHints.AcceptingInput);

        response.suggestedActions = {
            actions: [
            {
                title: i18n.__('main.helpBtnText1'),
                type: ActionTypes.ImBack,
                value: i18n.__('main.helpBtnValue1')
            },
            {
                title: i18n.__('main.helpBtnText2'),
                type: ActionTypes.ImBack,
                value: i18n.__('main.helpBtnValue2')
            },
            {
                title: i18n.__('main.helpBtnText3'),
                type: ActionTypes.OpenUrl,
                value: i18n.__('main.helpBtnValue3')
            }
            ],
            to: []
        };

        return Promise.resolve(<Activity> response);
    }

    private static fromResources(name: string): TemplateFunction {
        return (): Promise<string> => Promise.resolve(i18n.__(name));
    }
}
