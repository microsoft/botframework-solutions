/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { IAdaptiveCard, ICardElement } from 'adaptivecards';
// tslint:disable-next-line: no-submodule-imports
import { IContainer } from 'adaptivecards/lib/schema';
import { ActivityTypes } from 'botbuilder';
import { Activity, CardFactory, MessageFactory } from 'botbuilder-core';
import { ActionTypes, Attachment } from 'botframework-schema';
import { readFileSync } from 'fs';
import { join } from 'path';
import { Card } from './card';
import { ICardData } from './cardData';
import { IReply } from './reply';
import { IResponseIdCollection } from './responseIdCollection';
import { ResponseTemplate } from './responseTemplate';
import { ResponsesUtil } from '../util/';

export class ResponseManager {
    private readonly defaultLocaleKey: string = 'default';
    private static readonly simpleTokensRegex: RegExp = /\{(\w+)\}/g;
    private static readonly complexTokensRegex: RegExp = /\{[^{\}]+(?=})\}/g;

    public constructor(locales: string[], responseTemplates: IResponseIdCollection[]) {
        this.jsonResponses = new Map();

        responseTemplates.forEach((responseTemplate: IResponseIdCollection): void => {
            const resourceName: string = responseTemplate.name;
            const resource: string = responseTemplate.pathToResource || join(__dirname, '..', 'resources');
            this.loadResponses(resourceName, resource);
            locales.forEach((locale: string): void => {
                try {
                    this.loadResponses(resourceName, resource, locale);
                } catch {
                    // If satellite assembly doesn't exist, bot will fall back to default.
                }
            });
        });
    }

    public jsonResponses: Map<string, Map<string, ResponseTemplate>>;

    /**
     * Gets a simple response from template with Text, Speak, InputHint, and SuggestedActions set.
     * @param templateId The name of the response template.
     * @param tokens string map of tokens to replace in the response.
     * @returns An Activity.
     */
    public getResponse(templateId: string, locale: string, tokens?: Map<string, string>): Partial<Activity> {
        const template: ResponseTemplate = this.getResponseTemplate(templateId, locale);

        // create the response the data items
        return this.parseResponse(template, tokens);
    }

    /**
     * Gets a simple response from template with Text, Speak, InputHint, and SuggestedActions set.
     * @param templateId The name of the response template.
     * @param locale The locale for the response template.
     * @param tokens string map of tokens to replace in the response.
     * @returns An Activity.
     */
    public getLocalizedResponse(templateId: string, locale: string, tokens?: Map<string, string>): Partial<Activity> {
        const template: ResponseTemplate = this.getResponseTemplate(templateId, locale);

        // create the response the data items
        return this.parseResponse(template, tokens);
    }

    /**
     * Gets the Text of a response.
     * @param templateId The name of the response template.
     * @param tokens string map of tokens to replace in the response.
     * @returns The response text.
     */
    public getResponseText(templateId: string, locale: string, tokens?: Map<string, string>): string {
        const text: string | undefined = this.getResponse(templateId, locale, tokens).text;

        return text !== undefined ? text : '';
    }

    /**
     * Get a response with an Adaptive Card attachment.
     * @param cards The card(s) to add to the response.
     * @returns An Activity.
     */
    public getCardResponse(cards: Card | Card[], locale: string): Partial<Activity> {
        const resourcePath: string = join(__dirname, '..', 'resources', 'cards');

        if (cards instanceof Card) {
            const json: string = this.loadCardJson(cards.name, locale, resourcePath);
            const attachment: Attachment = this.buildCardAttachment(json, cards.data);

            return MessageFactory.attachment(attachment);
        } else {
            const attachments: Attachment[] = [];

            cards.forEach((card: Card): void => {
                const json: string = this.loadCardJson(card.name, locale, resourcePath);
                attachments.push(this.buildCardAttachment(json, card.data));
            });

            return MessageFactory.carousel(attachments);
        }
    }

    /**
     * Get a response from template with Text, Speak, InputHint, SuggestedActions, and an Adaptive Card attachment.
     * @param templateId The name of the response template.
     * @param cards The card(s) object to add to the response.
     * @param tokens Optional string map of tokens to replace in the response.
     */
    public getCardResponseWithTemplateId(templateId: string, cards: Card | Card[], locale: string, tokens?: Map<string, string>): Partial<Activity> {
        const response: Partial<Activity> = this.getResponse(templateId, locale, tokens);
        const resourcePath: string = join(__dirname, '..', 'resources', 'cards');

        if (cards instanceof Card) {
            const json: string = this.loadCardJson(cards.name, locale, resourcePath);
            const attachment: Attachment = this.buildCardAttachment(json, cards.data);

            return MessageFactory.attachment(attachment, response.text, response.speak, response.inputHint);
        } else {
            const attachments: Attachment[] = [];

            cards.forEach((card: Card): void => {
                const json: string = this.loadCardJson(card.name, locale, resourcePath);
                attachments.push(this.buildCardAttachment(json, card.data));
            });

            return MessageFactory.carousel(attachments, response.text, response.speak, response.inputHint);
        }
    }

    /**
     * Get a response from template with Text, Speak, InputHint, SuggestedActions, and a Card attachments with list items inside.
     * @param templateId The name of the response template.
     * @param card The main card container contains list.
     * @param tokens Optional string map of tokens to replace in the response.
     * @param containerName Target container.
     * @param containerItems Card list which will be injected to target container.
     * @returns An Activity.
     */
    public getCardResponseWithContainer(
        templateId: string,
        card: Card, 
        locale: string,
        tokens?: Map<string, string>,
        containerName?: string,
        containerItems?: Card[]): Partial<Activity> {
        const resourcePath: string = join(__dirname, '..', 'resources', 'cards');
        const json: string = this.loadCardJson(card.name, locale, resourcePath);

        const mainCard: IAdaptiveCard = this.buildCard(json, card.data);
        if (containerName && mainCard.body) {
            const itemContainer: ICardElement | undefined = mainCard.body.find((item: ICardElement): boolean => {
                return item.type === 'Container' && item.id === containerName;
            });
            const itemsAdaptiveContainer: IContainer = itemContainer as IContainer;
            if (itemsAdaptiveContainer !== undefined) {
                if (containerItems !== undefined) {
                    containerItems.forEach((cardItem: Card): void => {
                        const itemJson: string = this.loadCardJson(cardItem.name, locale, resourcePath);
                        const itemCard: IAdaptiveCard = this.buildCard(itemJson, cardItem.data);
                        if (itemCard.body !== undefined) {
                            itemCard.body.forEach((body: any): void => {
                                if (itemsAdaptiveContainer.items !== undefined) {
                                    itemsAdaptiveContainer.items.push(body);
                                }
                            });
                        }
                    });
                }
            }
        }

        const attachment: Attachment = CardFactory.adaptiveCard(mainCard);
        if (templateId) {
            const response: Partial<Activity> = this.getResponse(templateId, locale, tokens);

            return MessageFactory.attachment(attachment, response.text, response.speak, response.inputHint);
        }

        return MessageFactory.attachment(attachment);
    }

    public getResponseTemplate(templateId: string, locale: string): ResponseTemplate {
        let localeKey: string = locale;

        // warm up the JsonResponses loading to see if it actually exist.
        // If not, throw with the loading time exception that's actually helpful
        let key: string | undefined = this.getJsonResponseKeyForLocale(templateId, localeKey);

        // if no matching json file found for locale, try parent language
        if (key === undefined) {
            localeKey = localeKey.split('-')[0]
                .toLowerCase();
            key = this.getJsonResponseKeyForLocale(templateId, localeKey);

            // fall back to default
            if (key === undefined) {
                localeKey = this.defaultLocaleKey;
                key = this.getJsonResponseKeyForLocale(templateId, localeKey);
            }
        }

        if (key === undefined) {
            throw new Error();
        }

        // Get the bot response from the .json file
        const responseLocale: Map<string, ResponseTemplate> | undefined = this.jsonResponses.get(localeKey);
        if (!responseLocale || !responseLocale.has(key)) {
            throw new Error(`Unable to find response ${ templateId }`);
        }

        const response: ResponseTemplate | undefined = responseLocale.get(key);
        if (response === undefined) {
            throw new Error();
        }

        return response;
    }

    public format(messageTemplate: string, tokens?: Map<string, string>): string {
        let result: string = messageTemplate;

        ResponseManager.complexTokensRegex.lastIndex = 0;
        let match: RegExpExecArray | null = ResponseManager.complexTokensRegex.exec(result);
        while (match) {
            const bindingJson: string = match[0];

            const tokenKey: string = bindingJson
                .replace('{', '')
                .replace('}', '');

            if (tokens && tokens.has(tokenKey)) {
                result = result.replace(bindingJson, tokens.get(tokenKey) || '');
            }

            match = ResponseManager.complexTokensRegex.exec(result);
        }

        return result;
    }

    private loadResponses(resourceName: string, resourcePath: string, locale?: string): void {
        // if locale is not set, add resources under the default key.
        const localeKey: string = (locale !== undefined) ? locale : this.defaultLocaleKey;
        const jsonPath: string = ResponsesUtil.getResourcePath(resourceName, resourcePath, localeKey);

        try {
            const content: { [key: string]: Object } = JSON.parse(this.jsonFromFile(jsonPath));

            const localeResponses: Map<string, ResponseTemplate> = this.jsonResponses.get(localeKey) || new Map<string, ResponseTemplate>();

            Object.entries(content)
                .forEach((val: [string, Object]): void => {
                    const key: string = val[0];
                    const value: ITemplate = val[1] as ITemplate;
                    const template: ResponseTemplate = Object.assign(new ResponseTemplate(), value);
                    localeResponses.set(key, template);
                });

            this.jsonResponses.set(localeKey, localeResponses);
        } catch (err) {
            throw new Error(`Error deserializing ${ jsonPath }`);
        }
    }

    private getJsonResponseKeyForLocale(responseId: string, locale: string): string | undefined {
        if (this.jsonResponses.has(locale)) {
            const localeResponses: Map<string, ResponseTemplate> | undefined = this.jsonResponses.get(locale);
            if (localeResponses) {
                return localeResponses.has(responseId) ? responseId : undefined;
            }
        }

        return undefined;
    }

    private parseResponse(template: ResponseTemplate, data?: Map<string, string>): Partial<Activity> {
        const reply: IReply | undefined = template.reply;
        if (!reply) {
            throw new Error('There is no reply in the ResponseTemplate');
        }

        if (reply.text) {
            reply.text = this.format(reply.text, data);
        }

        if (reply.speak) {
            reply.speak = this.format(reply.speak, data);
        }

        const activity: Partial<Activity> = {
            type: ActivityTypes.Message,
            text: reply.text,
            speak: reply.speak,
            inputHint: template.inputHint
        };

        if (template.suggestedActions !== undefined && template.suggestedActions.length > 0) {
            activity.suggestedActions = {
                actions: [],
                to: []
            };

            template.suggestedActions.forEach((action: string): void => {
                if (activity.suggestedActions) {
                    activity.suggestedActions.actions.push({
                        type: ActionTypes.ImBack,
                        title: action,
                        value: action
                    });
                }
            });
        }

        activity.attachments = [];

        return activity;
    }

    private loadCardJson(cardName: string, locale: string, resourcePath: string): string {
        let jsonFile: string = join(resourcePath, `${ cardName }.${ locale }.json`);

        try {
            require.resolve(jsonFile);
        } catch (errLocale) {
            try {
                jsonFile = join(resourcePath, `${ cardName }.json`);
                require.resolve(jsonFile);
            } catch (error) {
                throw new Error(`Could not file Adaptive Card resource ${ jsonFile }`);
            }
        }

        return this.jsonFromFile(jsonFile);
    }

    private buildCardAttachment(json: string, data?: ICardData): Attachment {
        const card: IAdaptiveCard = this.buildCard(json, data);

        return CardFactory.adaptiveCard(card);
    }

    private buildCard(json: string, data?: ICardData): IAdaptiveCard {
        let jsonOut: string = json;
        // If cardData was provided
        if (data !== undefined) {
            // add all properties to the list
            const tokens: Map<string, string> = new Map<string, string>();
            // get property names from cardData
            Object.entries(data)
                .forEach((entry: [string, string]): void => {
                    if (!tokens.has(entry[0])) {
                        tokens.set(entry[0], entry[1]);
                    }
                });

            // replace tokens in json
            ResponseManager.simpleTokensRegex.lastIndex = 0;
            let match: RegExpExecArray | null = ResponseManager.simpleTokensRegex.exec(jsonOut);
            while (match) {
                if (tokens.has(match[0])) {
                    jsonOut = jsonOut.replace(match[0], tokens.get(match[0]) || '');
                }

                match = ResponseManager.simpleTokensRegex.exec(jsonOut);
            }
        }

        // Deserialize/Serialize logic is needed to prevent JSON exception in prompts
        return JSON.parse(jsonOut);
    }

    private jsonFromFile(filePath: string): string {
        return readFileSync(filePath, 'utf8');
    }
}

interface ITemplate {
    replies: IReply[];
    suggestedActions: string[];
    inputHint: string;
}
