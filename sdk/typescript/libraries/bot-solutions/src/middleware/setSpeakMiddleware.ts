/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Middleware, SendActivitiesHandler, TurnContext } from 'botbuilder';
import { Activity, ActivityTypes, ResourceResponse, Channels } from 'botframework-schema';
import { Element, js2xml, xml2js } from 'xml-js';

/**
 * Set Speech Synthesis Markup Language (SSML) on an Activity's Speak property with locale and voice input.
 */
export class SetSpeakMiddleware implements Middleware {
    private static readonly defaultLocale: string = 'en-us';
    private static readonly defaultVoiceFonts: Map<string, string> = new Map([
        ['de-de', 'Microsoft Server Speech Text to Speech Voice (de-DE, Hedda)'],
        ['en-us', 'Microsoft Server Speech Text to Speech Voice (en-US, Jessa24kRUS)'],
        ['es-es', 'Microsoft Server Speech Text to Speech Voice (es-ES, Laura, Apollo)'],
        ['fr-fr', 'Microsoft Server Speech Text to Speech Voice (fr-FR, Julie, Apollo)'],
        ['it-it', 'Microsoft Server Speech Text to Speech Voice (it-IT, Cosimo, Apollo)'],
        ['zh-cn', 'Microsoft Server Speech Text to Speech Voice (zh-CN, HuihuiRUS)']
    ]);
    private static readonly defaultChannels: Set<string> = new Set([Channels.DirectlineSpeech, Channels.Emulator]);
    private locale: string;
    private voiceFonts: Map<string, string>;
    private channels: Set<string>;
    private static readonly namespaceURI = 'https://www.w3.org/2001/10/synthesis';

    /**
     * Initializes a new instance of the SetSpeakMiddleware class.
     * @param locale If null, use en-US.
     * @param voiceFonts Map voice font for locale like en-US to "Microsoft Server Speech Text to Speech Voice (en-us, Jessa24kRUS).
     * @param channels Set SSML for these channels. If null, use DirectlineSpeech and Emulator.
     */
    public constructor(locale: string = '', voiceFonts: Map<string, string> = new Map<string, string>(), channels: Set<string> = new Set()) {
        this.locale = locale || SetSpeakMiddleware.defaultLocale;
        this.voiceFonts = voiceFonts.size > 0 ? voiceFonts : SetSpeakMiddleware.defaultVoiceFonts;
        this.channels = channels.size > 0 ? channels : SetSpeakMiddleware.defaultChannels;
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
                        activity.speak = this.getActivitySpeakText(activity);

                        if (activity.channelId !== undefined) {
                            if (this.channels.has(activity.channelId)) {
                                activity.speak = this.decorateSSML(activity);
                            }
                        }
                        break;
                    }
                    default:
                }
            });

            return await nextSend();
        };

        context.onSendActivities(handler);

        return next();
    }

    /**
     * Gets the speak text for the activity.
     * @param activity Outgoing bot Activity.
     * @returns speech text string value.
     */
    private getActivitySpeakText(activity: Partial<Activity>): string {
        // return speak or text value if they already exist in the activity
        const result: string | undefined = activity.speak || activity.text;
        if (result !== undefined) {
            return result;
        }

        // return speak value of first attachment if an attachment exists and has a speak value
        if (activity.attachments !== undefined && activity.attachments.length > 0) {
            const attachmentContent = activity.attachments[0].content;
            if (attachmentContent !== undefined && attachmentContent instanceof Object) {
                const contentObj = JSON.parse(attachmentContent);
                return contentObj['speak'].toString();
            }
        }

        return '';
    }

    /**
     * Formats an existing string to be formatted for Speech Synthesis Markup Language with a voice font.
     * @param activity Outgoing bot Activity.
     * @returns SSML-formatted string to be used with synthetic speech.
     */
    private decorateSSML(activity: Partial<Activity>): string {
        if (activity.speak === undefined || activity.speak.trim().length === 0) {
            return '';
        }

        let rootElement: Element | undefined = undefined;
        try {
            rootElement = xml2js(activity.speak, { compact: false }) as Element;
        } catch(err){
            // Ignore any exceptions. This is effectively a "TryParse", except that XElement doesn't
            // have a TryParse method.
        }

        if (rootElement === undefined || this.getLocalName(rootElement) !== 'speak') {
            // If the text is not valid XML, or if it's not a <speak> node, treat it as plain text.
            rootElement = this.createBaseElement(activity.speak);
        }

        let locale = this.locale;
        if (activity.locale !== undefined && activity.locale.trim().length > 0) {
            try {
                const normalizedLocale: string = activity.locale.toLowerCase();
                if (this.voiceFonts.has(normalizedLocale)){
                    locale = normalizedLocale;
                }
            } catch(err) {
            }
        }

        this.addAttributeIfMissing(rootElement, 'version', '1.0');
        this.addAttributeIfMissing(rootElement, 'xml:lang', `${ locale }`);
        this.addAttributeIfMissing(rootElement, 'xmlns:mstts', 'https://www.w3.org/2001/mstts');

        // Fix issue with 'number_digit' interpreter
        if (rootElement.elements !== undefined && rootElement.elements[0].elements !== undefined) {
            const sayAsElements: Element[] = rootElement.elements[0].elements.filter((e: Element): boolean => e.name === 'say-as');
            sayAsElements.forEach((e: Element): void => {
                this.updateAttributeIfPresent(e, 'interpret-as', 'digits', 'number_digit');
            });
        }

        // add voice element if absent
        const voiceFontOfLocale: string | undefined = this.voiceFonts.get(locale);
        if (voiceFontOfLocale !== undefined && voiceFontOfLocale.trim().length > 0) {
            this.addVoiceElementIfMissing(rootElement, voiceFontOfLocale);
        }

        return js2xml(rootElement, { compact: false });
    }

    /**
     * Add a new attribute to an XML element.
     * @param element The XML element to update.
     * @param attributeName The XML attribute name to add.
     * @param attributeValue The XML attribute value to add.
     */
    private addAttributeIfMissing(element: Element, attributeName: string, attributeValue: string): void {
        if (element.elements !== undefined && element.elements[0] !== undefined) {
            if (element.elements[0].attributes === undefined) {
                element.elements[0].attributes = {
                    attName: attributeValue
                };
            } else {
                if (element.elements[0].attributes[attributeName] === undefined) {
                    element.elements[0].attributes[attributeName] = attributeValue;
                }
            }
        }
    }

    /**
     * Add a new attribute with a voice property to the parent XML element.
     * @param parentElement The XML element to update.
     * @param attributeValue The XML attribute value to add.
     */
    private addVoiceElementIfMissing(parentElement: Element, attributeValue: string): void {
        try {
            if (parentElement.elements === undefined || parentElement.elements[0].elements === undefined) {
                throw new Error('rootElement undefined');
            }
            const existingVoiceElement: Element | undefined = parentElement.elements[0].elements.find((e: Element): boolean => e.name === 'voice');

            // If an existing voice element is undefined (absent), then add it. Otherwise, assume the author has set it correctly.
            if (existingVoiceElement === undefined) {
                const oldElements: Element[] = JSON.parse(JSON.stringify(parentElement.elements[0].elements));
                parentElement.elements[0].elements = [
                    {
                        type: 'element',
                        name: 'voice',
                        attributes: {
                            name: attributeValue
                        },
                        elements: oldElements
                    }
                ];
            } else {
                if (existingVoiceElement.attributes !== undefined) {
                    existingVoiceElement.attributes['name'] = attributeValue;
                }
            }
        } catch (error) {
            throw new Error(`Could not add voice element to speak property: ${ error.message }`);
        }
    }

    /**
     * Update an XML attribute if it already exists.
     * @param element The XML element to update.
     * @param attributeName The XML attribute name to update.
     * @param currentAttributeValue The XML attribute name to update.
     * @param newAttributeValue The XML attribute name to update.
     */
    private updateAttributeIfPresent(element: Element, attributeName: string, currentAttributeValue: string, newAttributeValue: string): void {
        if (element.attributes !== undefined && element.attributes[attributeName] === currentAttributeValue) {
            element.attributes[attributeName] = newAttributeValue;
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
                        xmlns: SetSpeakMiddleware.namespaceURI
                    }
                }
            ]
        };
    }
}
