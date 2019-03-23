// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { ActivityExtensions } from 'bot-solution';
import {
    Activity,
    CardFactory,
    InputHints,
    MessageFactory,
    TurnContext } from 'botbuilder';
import {
    ActionTypes,
    Attachment,
    ThumbnailCard } from 'botframework-schema';
import i18next from 'i18next';
import { join } from 'path';
import {
    DictionaryRenderer,
    LanguageTemplateDictionary,
    TemplateFunction,
    TemplateIdMap } from '../templateManager/dictionaryRenderer';
import { TemplateManager } from '../templateManager/templateManager';

export class MainResponses extends TemplateManager {

    // Declare here the type of properties and the prompts
    public static responseIds: {
        cancelled: string;
        completed: string;
        confused: string;
        greeting: string;
        help: string;
        intro: string;
        error: string;
        noActiveDialog: string;
        qna: string;
    } = {
        cancelled: 'cancelled',
        completed: 'completed',
        confused: 'confused',
        greeting: 'greeting',
        help: 'help',
        intro: 'intro',
        error: 'error',
        noActiveDialog: 'noActiveDialog',
        qna: 'qna'
    };

    // Declare the responses map prompts
    private static readonly responseTemplates: LanguageTemplateDictionary = new Map([
        ['default', <TemplateIdMap> new Map([
            [MainResponses.responseIds.cancelled, MainResponses.fromResources('main.cancelled')],
            [MainResponses.responseIds.noActiveDialog, MainResponses.fromResources('main.noActiveDialog')],
            [MainResponses.responseIds.completed, MainResponses.fromResources('main.completed')],
            [MainResponses.responseIds.confused, MainResponses.fromResources('main.confused')],
            [MainResponses.responseIds.greeting, MainResponses.fromResources('main.greeting')],
            [MainResponses.responseIds.error, MainResponses.fromResources('main.error')],
            [MainResponses.responseIds.help,
                // tslint:disable-next-line:no-any
                (context: TurnContext, data: any): Promise<Activity>  => MainResponses.buildHelpCard(context, data)],
                [MainResponses.responseIds.intro,
                    // tslint:disable-next-line:no-any
                    (context: TurnContext, data: any): Promise<Activity>  => MainResponses.buildIntroCard(context, data)],
                    [MainResponses.responseIds.qna,
                        // tslint:disable-next-line:no-any
                        (context: TurnContext, data: any): Promise<Activity>  => MainResponses.buildQnACard(context, data)]
                    ])]
                ]);

    // Initialize the responses class properties
    constructor() {
        super();
        this.register(new DictionaryRenderer(MainResponses.responseTemplates));
    }

    // tslint:disable-next-line:no-any
    public static buildHelpCard(context: TurnContext, data: any): Promise<Activity> {
        const title: string = i18next.t('main.helpTitle');
        const text: string = i18next.t('main.helpText');
        const attachment: Attachment = CardFactory.heroCard(title, text);
        const response: Partial<Activity> = MessageFactory.attachment(attachment, '', text, InputHints.AcceptingInput);

        response.suggestedActions = {
            actions: [
            {
                title: i18next.t('main.calendarSuggestedAction'),
                type: ActionTypes.ImBack,
                value: i18next.t('main.calendarSuggestedAction')
            },
            {
                title: i18next.t('main.emailSuggestedAction'),
                type: ActionTypes.ImBack,
                value: i18next.t('main.emailSuggestedAction')
            },
            {
                title: i18next.t('main.meetingSuggestedAction'),
                type: ActionTypes.ImBack,
                value: i18next.t('main.meetingSuggestedAction')
            },
            {
                title: i18next.t('main.poiSuggestedAction'),
                type: ActionTypes.ImBack,
                value: i18next.t('main.poiSuggestedAction')
            }
            ],
            to: []
        };

        return Promise.resolve(<Activity> response);
    }

    // tslint:disable-next-line:no-any
    public static buildIntroCard(context: TurnContext, data: any): Promise<Activity> {
        const introPath: string = join(__dirname, 'resources', i18next.t('main.introPath'));
        // tslint:disable-next-line:no-any non-literal-require
        const introCard: any = require(introPath);
        const attachment: Attachment = CardFactory.adaptiveCard(introCard);

        return Promise.resolve(<Activity>MessageFactory.attachment(attachment, '', attachment.content.speak, InputHints.AcceptingInput));
    }

    // tslint:disable-next-line:no-any
    public static buildQnACard(context: TurnContext, answer: any): Promise<Activity> {
        const response: Partial<Activity> = ActivityExtensions.createReply(context.activity);

        try {
            const card: ThumbnailCard = <ThumbnailCard> JSON.parse(answer); // JsonConvert.DeserializeObject<ThumbnailCard>(answer);

            response.attachments = [
                CardFactory.thumbnailCard(card.title, card.images, card.buttons, card)
            ];
            response.speak =  card.title ? `"${card.title} "` : '';
            response.speak += card.subtitle ? `"${card.subtitle} "` : '';
            response.speak += card.text ? `"${card.text} "` : '';
        } catch (err) {
            response.text = answer;
        }

        return Promise.resolve(<Activity> response);
    }

    private static fromResources(name: string): TemplateFunction {
        return (): Promise<string> => Promise.resolve(i18next.t(name));
    }
}
