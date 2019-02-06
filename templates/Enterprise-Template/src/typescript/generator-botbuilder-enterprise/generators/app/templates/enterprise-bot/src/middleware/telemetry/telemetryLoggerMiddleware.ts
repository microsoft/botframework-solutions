// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { TelemetryClient } from 'applicationinsights';
import { Activity,
        ActivityTypes,
        ConversationReference,
        Middleware,
        ResourceResponse,
        TurnContext } from 'botbuilder';
import { TelemetryConstants } from './telemetryConstants';

/**
 * Middleware for logging incoming, outgoing, updated or deleted Activity messages into Application Insights.
 * In addition, registers the telemetry client in the context so other Application Insights
 * components can log telemetry.
 * If this Middleware is removed, all the other sample components don't log (but still operate).
 */
export class TelemetryLoggerMiddleware implements Middleware {
    public static readonly APP_INSIGHTS_SERVICE_KEY: string = 'TelemetryLoggerMiddleware.AppInsightsContext';

    /**
     * Application Insights Custom Event name, logged when new message is received from the user
     */
    public static readonly BOT_MSG_RECEIVE_EVENT: string = 'BotMessageReceived';

    /**
     * Application Insights Custom Event name, logged when a message is sent out from the bot
     */
    public static readonly BOT_MSG_SEND_EVENT: string = 'BotMessageSend';

    /**
     * Application Insights Custom Event name, logged when a message is updated by the bot (rare case)
     */
    public static readonly BOT_MSG_UPDATE_EVENT: string = 'BotMessageUpdate';

    /**
     * Application Insights Custom Event name, logged when a message is deleted by the bot (rare case)
     */
    public static readonly BOT_MSG_DELETE_EVENT: string = 'BotMessageDelete';

    private readonly TELEMETRY_CLIENT: TelemetryClient;
    private readonly LOG_USERNAME: boolean;
    private readonly LOG_ORIGINAL_MESSAGE: boolean;
    private readonly TELEMETRY_CONSTANTS: TelemetryConstants = new TelemetryConstants();

    /**
     * Initializes a new instance of the TelemetryLoggerMiddleware class.
     * @param instrumentationKey The Application Insights instrumentation key.  See Application Insights for more information.
     * @param logUserName (Optional) Enable/Disable logging user name within Application Insights.
     * @param logOriginalMessage (Optional) Enable/Disable logging original message name within Application Insights.
     */
    constructor(telemetryClient: TelemetryClient, logUserName: boolean = false, logOriginalMessage: boolean = false) {
        if (!telemetryClient) {
            throw new Error('Error not found');
        }
        this.TELEMETRY_CLIENT = telemetryClient;
        this.LOG_USERNAME = logUserName;
        this.LOG_ORIGINAL_MESSAGE = logOriginalMessage;
    }

    /**
     * Gets a value indicating whether indicates whether to log the original message into the BotMessageReceived event.
     */
    public get logUserName(): boolean { return this.LOG_USERNAME; }

    /**
     * Gets a value indicating whether indicates whether to log the user name into the BotMessageReceived event.
     */
    public get logOriginalMessage(): boolean { return this.LOG_ORIGINAL_MESSAGE; }

    /**
     * Records incoming and outgoing activities to the Application Insights store.
     * @param context The context object for this turn.
     * @param next The delegate to call to continue the bot middleware pipeline
     */
    public async onTurn(context: TurnContext, next: () => Promise<void>): Promise<void> {
        if (context === null) {
            throw new Error('context is null');
        }

        context.turnState.set(TelemetryLoggerMiddleware.APP_INSIGHTS_SERVICE_KEY, this.TELEMETRY_CLIENT);

        // log incoming activity at beginning of turn
        if (context.activity !== null) {

            const activity: Activity = context.activity;

            // Log the Application Insights Bot Message Received
            this.TELEMETRY_CLIENT.trackEvent({
                name: TelemetryLoggerMiddleware.BOT_MSG_RECEIVE_EVENT,
                properties: this.fillReceiveEventProperties(activity)
            });
        }

        // hook up onSend pipeline
        context.onSendActivities(async (ctx: TurnContext,
                                        activities: Partial<Activity>[],
                                        nextSend: () => Promise<ResourceResponse[]>): Promise<ResourceResponse[]> => {
            // run full pipeline
            const responses: ResourceResponse[] = await nextSend();

            activities.forEach((activity: Partial<Activity>) => this.TELEMETRY_CLIENT.trackEvent({
                name: TelemetryLoggerMiddleware.BOT_MSG_SEND_EVENT,
                properties: this.fillSendEventProperties(<Activity> activity)
            }));

            return responses;
        });

        // hook up update activity pipeline
        context.onUpdateActivity(async (ctx: TurnContext,
                                        activity: Partial<Activity>,
                                        nextUpdate: () => Promise<void>) => {
            // run full pipeline
            const response: void = await nextUpdate();

            this.TELEMETRY_CLIENT.trackEvent({
                name: TelemetryLoggerMiddleware.BOT_MSG_SEND_EVENT,
                properties: this.fillUpdateEventProperties(<Activity> activity)
            });

            return response;
        });

        // hook up delete activity pipeline
        context.onDeleteActivity(async (ctx: TurnContext,
                                        reference: Partial<ConversationReference>,
                                        nextDelete: () => Promise<void>) => {
            // run full pipeline
            await nextDelete();

            const deletedActivity: Partial<Activity> = TurnContext.applyConversationReference(
                {
                    type: ActivityTypes.MessageDelete,
                    id: reference.activityId
                },
                reference,
                false);

            this.TELEMETRY_CLIENT.trackEvent({
                name: TelemetryLoggerMiddleware.BOT_MSG_SEND_EVENT,
                properties: this.fillDeleteEventProperties(<Activity> deletedActivity)
            });
        });

        if (next !== null) {
            await next();
        }
    }

    /**
     * Fills the Application Insights Custom Event properties for BotMessageReceived.
     * These properties are logged in the custom event when a new message is received from the user.
     * @param activity - Last activity sent from user.
     * @returns A dictionary that is sent as "Properties" to Application Insights TrackEvent method for the BotMessageReceived Message.
     */
    private fillReceiveEventProperties(activity: Activity): { [key: string]: string } {
        const properties: { [key: string]: string } = {};

        properties[this.TELEMETRY_CONSTANTS.ACTIVITY_ID_PROPERTY] = activity.id || '';
        properties[this.TELEMETRY_CONSTANTS.CHANNEL_ID_PROPERTY] = activity.channelId;
        properties[this.TELEMETRY_CONSTANTS.FROM_ID_PROPERTY] = activity.from.id || '';
        properties[this.TELEMETRY_CONSTANTS.LOCALE_PROPERTY] = activity.locale || '';
        properties[this.TELEMETRY_CONSTANTS.RECIPIENT_ID_PROPERTY] = activity.recipient.id;
        properties[this.TELEMETRY_CONSTANTS.RECIPIENT_NAME_PROPERTY] = activity.recipient.name;

        // For some customers,
        // logging user name within Application Insights might be an issue so have provided a config setting to disable this feature
        if (this.logUserName && activity.from.name && activity.from.name.trim()) {
            properties[this.TELEMETRY_CONSTANTS.FROM_NAME_PROPERTY] = activity.from.name;
        }

        // For some customers,
        // logging the utterances within Application Insights might be an so have provided a config setting to disable this feature
        if (this.logOriginalMessage && activity.text && activity.text.trim()) {
            properties[this.TELEMETRY_CONSTANTS.TEXT_PROPERTY] = activity.text;
        }

        return properties;
    }

    /**
     * Fills the Application Insights Custom Event properties for BotMessageSend.
     * These properties are logged in the custom event when a response message is sent by the Bot to the user.
     * @param activity - Last activity sent from user.
     * @returns A dictionary that is sent as "Properties" to Application Insights TrackEvent method for the BotMessageSend Message.
     */
    private fillSendEventProperties(activity: Activity): { [key: string]: string } {
        const properties: { [key: string]: string } = {};

        properties[this.TELEMETRY_CONSTANTS.ACTIVITY_ID_PROPERTY] = activity.id || '';
        properties[this.TELEMETRY_CONSTANTS.CHANNEL_ID_PROPERTY] = activity.channelId;
        properties[this.TELEMETRY_CONSTANTS.REPLY_ACTIVITY_ID_PROPERTY] = activity.replyToId || '';
        properties[this.TELEMETRY_CONSTANTS.RECIPIENT_ID_PROPERTY] = activity.recipient.id;
        properties[this.TELEMETRY_CONSTANTS.CONVERSATION_NAME_PROPERTY] = activity.conversation.name;
        properties[this.TELEMETRY_CONSTANTS.LOCALE_PROPERTY] = activity.locale || '';
        properties[this.TELEMETRY_CONSTANTS.RECIPIENT_NAME_PROPERTY] = activity.recipient.name;

        // For some customers,
        // logging the utterances within Application Insights might be an so have provided a config setting to disable this feature
        if (this.LOG_USERNAME && activity.recipient.name && activity.recipient.name.trim()) {
            properties[this.TELEMETRY_CONSTANTS.RECIPIENT_NAME_PROPERTY] = activity.recipient.name;
        }

        // For some customers,
        // logging the utterances within Application Insights might be an so have provided a config setting to disable this feature
        if (this.logOriginalMessage && activity.text && activity.text.trim()) {
            properties[this.TELEMETRY_CONSTANTS.TEXT_PROPERTY] = activity.text;
        }

        return properties;
    }

    /**
     * Fills the Application Insights Custom Event properties for BotMessageUpdate.
     * These properties are logged in the custom event when an activity message is updated by the Bot.
     * For example, if a card is interacted with by the use, and the card needs to be updated to reflect
     * some interaction.
     * @param activity - Last activity sent from user.
     * @returns A dictionary that is sent as "Properties" to Application Insights TrackEvent method for the BotMessageUpdate Message.
     */
    private fillUpdateEventProperties(activity: Activity): { [key: string]: string } {
        const properties: { [key: string]: string } = {};
        properties[this.TELEMETRY_CONSTANTS.CHANNEL_ID_PROPERTY] = activity.channelId;
        properties[this.TELEMETRY_CONSTANTS.RECIPIENT_ID_PROPERTY] = activity.recipient.id;
        properties[this.TELEMETRY_CONSTANTS.CONVERSATION_ID_PROPERTY] = activity.conversation.id;
        properties[this.TELEMETRY_CONSTANTS.CONVERSATION_NAME_PROPERTY] = activity.conversation.name;
        properties[this.TELEMETRY_CONSTANTS.LOCALE_PROPERTY] = activity.locale || '';

        // For some customers,
        // logging the utterances within Application Insights might be an so have provided a config setting to disable this feature
        if (this.logOriginalMessage && activity.text && activity.text.trim()) {
            properties[this.TELEMETRY_CONSTANTS.TEXT_PROPERTY] = activity.text;
        }

        return properties;
    }

    /**
     * Fills the Application Insights Custom Event properties for BotMessageDelete.
     * These properties are logged in the custom event when an activity message is deleted by the Bot.  This is a relatively rare case.
     * @param activity - Last activity sent from user.
     * @returns A dictionary that is sent as "Properties" to Application Insights TrackEvent method for the BotMessageDelete Message.
     */
    private fillDeleteEventProperties(activity: Activity): { [key: string]: string } {
        const properties: { [key: string]: string } = {};
        properties[this.TELEMETRY_CONSTANTS.CHANNEL_ID_PROPERTY] = activity.channelId;
        properties[this.TELEMETRY_CONSTANTS.RECIPIENT_ID_PROPERTY] = activity.recipient.id;
        properties[this.TELEMETRY_CONSTANTS.CONVERSATION_ID_PROPERTY] = activity.conversation.id;
        properties[this.TELEMETRY_CONSTANTS.CONVERSATION_NAME_PROPERTY] = activity.conversation.name;

        return properties;
    }
}
