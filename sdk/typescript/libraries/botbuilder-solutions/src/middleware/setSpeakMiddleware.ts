/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Middleware, SendActivitiesHandler, TurnContext } from 'botbuilder';
import { Activity, ActivityTypes, ResourceResponse } from 'botframework-schema';
import { Element, js2xml, xml2js } from 'xml-js';

const DEFAULT_LOCALE = 'en-US';
const DEFAULT_VOICE_FONT = 'Microsoft Server Speech Text to Speech Voice (en-US, JessaNeural)';

/**
 * Set Speech Synthesis Markup Language (SSML) on an Activity's Speak property with locale and voice input.
 */
export class SetSpeakMiddleware implements Middleware {
    private readonly locale: string;
    private readonly voiceFont: string;
    private readonly namespaceURI: string = 'https://www.w3.org/2001/10/synthesis';

    public constructor(locale: string = DEFAULT_LOCALE, voiceName: string = DEFAULT_VOICE_FONT) {
        this.locale = locale;
        this.voiceFont = voiceName;
    }

    /**
     * If outgoing Activities are messages and using the Direct Line Speech channel,
     * decorate the Speak property with an SSML formatted string.
     * @param context The Bot Context object.
     * @param next The next middleware component to run.
     */
    public async onTurn(context: TurnContext, next: () => Promise<void>): Promise<void> {
        const handler: SendActivitiesHandler = async (
            ctx: TurnContext, activities: Partial<Activity>[], nextSend: () => Promise<ResourceResponse[]>
        ): Promise<ResourceResponse[]> => {
            activities.forEach((activity: Partial<Activity>): void => {
                switch (activity.type) {
                    case ActivityTypes.Message: {
                        if (activity.speak === undefined) {
                            activity.speak = activity.text;
                        }

                        // PENDING: Use Microsoft.Bot.Connector.Channels comparison when "directlinespeech" is available
                        if (activity.channelId === 'directlinespeech') {
                            activity.speak = this.decodeSSML(activity);
                        }

                        break;
                    }
                    default:
                }
            });

            return nextSend();
        };

        context.onSendActivities(handler);

        return next();
    }

    private decodeSSML(activity: Partial<Activity>): string {
        if (activity.speak === undefined || activity.speak.trim() === '') {
            return '';
        }

        let rootElement: Element | undefined = this.elementParse(activity.speak);

        if (rootElement === undefined || this.getLocalName(rootElement) !== 'speak') {
            // If the text is not valid XML, or if it's not a <speak> node, treat it as plain text.
            rootElement = this.createBaseElement(activity.speak);
        }

        this.addAttributeIfMissing(rootElement, 'version', '1.0');
        this.addAttributeIfMissing(rootElement, 'xml:lang', `lang${ this.locale }`);
        this.addAttributeIfMissing(rootElement, 'xmlns:mstts', 'https://www.w3.org/2001/mstts');

        // Fix issue with 'number_digit' interpreter
        if (rootElement.elements !== undefined && rootElement.elements[0].elements !== undefined) {
            const sayAsElements: Element[] = rootElement.elements[0].elements.filter((e: Element): boolean => e.name === 'say-as');
            sayAsElements.forEach((e: Element): void => {
                this.updateAttributeIfPresent(e, 'interpret-as', 'digits', 'number_digit');
            });
        }

        // add voice element if absent
        this.addVoiceElementIfMissing(rootElement, this.voiceFont);

        return js2xml(rootElement, { compact: false });
    }

    private elementParse(value: string): Element | undefined {
        try {
            return xml2js(value, { compact: false }) as Element;
        } catch (error) {
            return undefined;
        }
    }

    private getLocalName(element: Element): string | undefined {
        if (element.elements !== undefined && element.elements.length === 1) {
            return element.elements[0].name;
        }

        return undefined;
    }

    private createBaseElement(value: string): Element {
        // creating simple element
        return {
            elements: [
                {
                    type: 'element',
                    name: 'speak',
                    elements: [
                        {
                            type: 'text',
                            text: value
                        }
                    ],
                    attributes: {
                        xmlns: this.namespaceURI
                    }
                }
            ]};
    }

    private addAttributeIfMissing(element: Element, attName: string, attValue: string): void {
        if (element.elements !== undefined && element.elements[0] !== undefined) {
            if (element.elements[0].attributes === undefined) {
                element.elements[0].attributes = {
                    attName: attValue
                };
            } else {
                if (element.elements[0].attributes[attName] === undefined) {
                    element.elements[0].attributes[attName] = attValue;
                }
            }
        }
    }

    private updateAttributeIfPresent(element: Element, attName: string, attOld: string, attNew: string): void {
        if (element.attributes !== undefined && element.attributes[attName] === attOld) {
            element.attributes[attName] = attNew;
        }
    }

    private addVoiceElementIfMissing(element: Element, attValue: string): void {
        try {
            if (element.elements === undefined || element.elements[0].elements === undefined) {
                throw new Error('rootElement undefined');
            }
            const existingVoiceElement: Element | undefined = element.elements[0].elements.find((e: Element): boolean => e.name === 'voice');

            // If an existing voice element is undefined (absent), then add it. Otherwise, assume the author has set it correctly.
            if (existingVoiceElement === undefined) {
                const oldElements: Element[] = JSON.parse(JSON.stringify(element.elements[0].elements));
                element.elements[0].elements = [
                    {
                        type: 'element',
                        name: 'voice',
                        attributes: {
                            name: attValue
                        },
                        elements: oldElements
                    }
                ];
            } else {
                if (existingVoiceElement.attributes !== undefined) {
                    existingVoiceElement.attributes['name'] = attValue;
                }
            }
        } catch (error) {
            throw new Error(`Could not add voice element to speak property: ${ error.message }`);
        }
    }
}

