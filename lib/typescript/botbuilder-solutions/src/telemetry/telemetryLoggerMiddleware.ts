/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Activity, ActivityTypes, BotTelemetryClient, ConversationReference,
    IMessageDeleteActivity, Middleware, ResourceResponse, TurnContext } from 'botbuilder';
import { TelemetryExtensions } from './telemetryExtensions';

export namespace TelemetryConstants {
    export const channelIdProperty: string = 'channelId';
    export const conversationIdProperty: string = 'conversationId';
    export const conversationNameProperty: string = 'conversationName';
    export const dialogIdProperty: string = 'DialogId';
    export const fromIdProperty: string = 'fromId';
    export const fromNameProperty: string = 'fromName';
    export const localeProperty: string = 'locale';
    export const recipientIdProperty: string = 'recipientId';
    export const recipientNameProperty: string = 'recipientName';
    export const replyActivityIDProperty: string = 'replyActivityId';
    export const textProperty: string = 'text';
    export const speakProperty: string = 'speak';
    export const userIdProperty: string = 'userId';
}

/**
 * The Application Insights property names that we're logging.
 */
export namespace TelemetryLoggerConstants {
    // Application Insights Custom Event name, logged when new message is received from the user
    export const botMsgReceiveEvent: string = 'BotMessageReceived';
    // Application Insights Custom Event name, logged when a message is sent out from the bot
    export const botMsgSendEvent: string = 'BotMessageSend';
    // Application Insights Custom Event name, logged when a message is updated by the bot (rare case)
    export const botMsgUpdateEvent: string = 'BotMessageUpdate';
    // Application Insights Custom Event name, logged when a message is deleted by the bot (rare case)
    export const botMsgDeleteEvent: string = 'BotMessageDelete';
}

export interface ITelemetryLoggerMiddleware {
    logPersonalInformation: boolean;

    /**
     * Fills the Application Insights Custom Event properties for BotMessageReceived.
     * These properties are logged in the custom event when a new message is received from the user.
     * @param activity Last activity sent from user.
     * @returns A map that is sent as "Properties" to Application Insights trackEvent method for the BotMessageReceived Message.
     */
    fillReceiveEventProperties(activity: Partial<Activity>): Map<string, string>;

    /**
     * Fills the Application Insights Custom Event properties for BotMessageSend.
     * These properties are logged in the custom event when a response message is sent by the Bot to the user.
     * @param activity Last activity sent from user.
     * @returns A dictionary that is sent as "Properties" to Application Insights TrackEvent method for the BotMessageSend Message.
     */
    fillSendEventProperties(activity: Partial<Activity>): Map<string, string>;

    /**
     * Fills the Application Insights Custom Event properties for BotMessageUpdate.
     * These properties are logged in the custom event when an activity message is updated by the Bot.
     * For example, if a card is interacted with by the use, and the card needs to be updated to reflect
     * some interaction.
     * @param activity Last activity sent from user.
     * @returns A dictionary that is sent as "Properties" to Application Insights TrackEvent method for the BotMessageUpdate Message.
     */
    fillUpdateEventProperties(activity: Partial<Activity>): Map<string, string>;

    /**
     * Fills the Application Insights Custom Event properties for BotMessageDelete.
     * These properties are logged in the custom event when an activity message is deleted by the Bot.  This is a relatively rare case.
     * @param activity Last activity sent from user.
     * @returns A dictionary that is sent as "Properties" to Application Insights TrackEvent method for the BotMessageDelete Message.
     */
    fillDeleteEventProperties(activity: Partial<IMessageDeleteActivity>): Map<string, string>;
}

/**
 * Middleware for logging incoming, outgoing, updated or deleted Activity messages into Application Insights.
 * In addition, registers the telemetry client in the context so other Application Insights components can log
 * telemetry.
 * If this Middleware is removed, all the other sample components don't log (but still operate).
 */
export class TelemetryLoggerMiddleware implements Middleware, ITelemetryLoggerMiddleware {
    public static readonly appInsightsServiceKey: string = `${TelemetryLoggerMiddleware.name}.AppInsightsContext`;

    private readonly telemetryClient: BotTelemetryClient;
    public readonly logPersonalInformation: boolean;

    constructor(telemetryClient: BotTelemetryClient, logPersonalInformation: boolean = false) {
        this.telemetryClient = telemetryClient;
        this.logPersonalInformation = logPersonalInformation;
    }

    public async onTurn(context: TurnContext, next: () => Promise<void>): Promise<void> {
        context.turnState.set(TelemetryLoggerMiddleware.appInsightsServiceKey, this.telemetryClient);

        // log incoming activity at beginning of turn
        if (context.activity !== undefined) {
            const activity: Activity = context.activity;

            // Log the Application Insights Bot Message Received
            TelemetryExtensions.trackEventEx(
                this.telemetryClient,
                TelemetryLoggerConstants.botMsgReceiveEvent,
                activity,
                undefined,
                this.fillReceiveEventProperties(activity));
        }

        // hook up onSend pipeline
        context.onSendActivities(async (ctx: TurnContext, activities: Partial<Activity>[],
                                        nextSend: () => Promise<ResourceResponse[]>): Promise<ResourceResponse[]> => {
            // run full pipeline
            const response: ResourceResponse[] = await nextSend();

            activities.forEach((activity: Partial<Activity>) => {
                TelemetryExtensions.trackEventEx(
                    this.telemetryClient,
                    TelemetryLoggerConstants.botMsgSendEvent,
                    activity,
                    undefined,
                    this.fillReceiveEventProperties(activity));
            });

            return response;
        });

        // hook up update activity pipeline
        context.onUpdateActivity(async (ctx: TurnContext, activity: Partial<Activity>,
                                        nextUpdate: () => Promise<void>): Promise<void> => {
            // run full pipeline
            await nextUpdate();

            TelemetryExtensions.trackEventEx(
                this.telemetryClient,
                TelemetryLoggerConstants.botMsgSendEvent,
                activity,
                undefined,
                this.fillUpdateEventProperties(activity));
        });

        // hook up delete activity pipeline
        context.onDeleteActivity(async (ctx: TurnContext, reference: Partial<ConversationReference>,
                                        nextDelete: () => Promise<void>): Promise<void> => {
            // run full pipeline
            await nextDelete();

            const deleteActivity: Partial<Activity> = TurnContext.applyConversationReference(
                {
                    type: ActivityTypes.MessageDelete,
                    id: reference.activityId
                },
                reference, false);

            TelemetryExtensions.trackEventEx(
                this.telemetryClient,
                TelemetryLoggerConstants.botMsgDeleteEvent,
                deleteActivity,
                undefined,
                this.fillDeleteEventProperties(deleteActivity));
        });

        if (next) {
            await next();
        }
    }

    public fillReceiveEventProperties(activity: Partial<Activity>): Map<string, string> {
        const properties: Map<string, string> = new Map();
        properties.set(TelemetryConstants.fromIdProperty, activity.from ? activity.from.id : '');
        properties.set(TelemetryConstants.conversationNameProperty, activity.conversation ? activity.conversation.name : '');
        properties.set(TelemetryConstants.localeProperty, activity.locale || '');
        properties.set(TelemetryConstants.recipientIdProperty, activity.recipient ? activity.recipient.id : '');
        properties.set(TelemetryConstants.recipientNameProperty, activity.recipient ? activity.recipient.name : '');

        // Use the LogPersonalInformation flag to toggle logging PII data, text and user name are common examples
        if (this.logPersonalInformation) {
            if (activity.from && activity.from.name) {
                properties.set(TelemetryConstants.fromNameProperty, activity.from.name);
            }

            if (activity.text) {
                properties.set(TelemetryConstants.textProperty, activity.text);
            }

            if (activity.speak) {
                properties.set(TelemetryConstants.speakProperty, activity.speak);
            }
        }

        return properties;
    }

    public fillSendEventProperties(activity: Partial<Activity>): Map<string, string> {
        const properties: Map<string, string> = new Map();
        properties.set(TelemetryConstants.replyActivityIDProperty, activity.replyToId || '');
        properties.set(TelemetryConstants.recipientIdProperty, activity.recipient ? activity.recipient.id : '');
        properties.set(TelemetryConstants.conversationNameProperty, activity.conversation ? activity.conversation.name : '');
        properties.set(TelemetryConstants.localeProperty, activity.locale || '');

        // Use the LogPersonalInformation flag to toggle logging PII data, text and user name are common examples
        if (this.logPersonalInformation) {
            if (activity.recipient && activity.recipient.name) {
                properties.set(TelemetryConstants.recipientNameProperty, activity.recipient.name);
            }

            if (activity.text) {
                properties.set(TelemetryConstants.textProperty, activity.text);
            }

            if (activity.speak) {
                properties.set(TelemetryConstants.speakProperty, activity.speak);
            }
        }

        return properties;
    }

    public fillUpdateEventProperties(activity: Partial<Activity>): Map<string, string> {
        const properties: Map<string, string> = new Map();
        properties.set(TelemetryConstants.recipientIdProperty, activity.recipient ? activity.recipient.id : '');
        properties.set(TelemetryConstants.conversationIdProperty, activity.conversation ? activity.conversation.id : '');
        properties.set(TelemetryConstants.conversationNameProperty, activity.conversation ? activity.conversation.name : '');
        properties.set(TelemetryConstants.localeProperty, activity.locale || '');

        // Use the LogPersonalInformation flag to toggle logging PII data, text and user name are common examples
        if (this.logPersonalInformation && activity.text) {
            properties.set(TelemetryConstants.textProperty, activity.text);
        }

        return properties;
    }

    public fillDeleteEventProperties(activity: Partial<IMessageDeleteActivity>): Map<string, string> {
        const properties: Map<string, string> = new Map();
        properties.set(TelemetryConstants.recipientIdProperty, activity.recipient ? activity.recipient.id : '');
        properties.set(TelemetryConstants.conversationIdProperty, activity.conversation ? activity.conversation.id : '');
        properties.set(TelemetryConstants.conversationNameProperty, activity.conversation ? activity.conversation.name : '');

        return properties;
    }
}
