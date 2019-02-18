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
    public static readonly appInsightsServiceKey: string = 'TelemetryLoggerMiddleware.AppInsightsContext';

    /**
     * Application Insights Custom Event name, logged when new message is received from the user
     */
    public static readonly botMsgReceiveEvent: string = 'BotMessageReceived';

    /**
     * Application Insights Custom Event name, logged when a message is sent out from the bot
     */
    public static readonly botMsgSendEvent: string = 'BotMessageSend';

    /**
     * Application Insights Custom Event name, logged when a message is updated by the bot (rare case)
     */
    public static readonly botMsgUpdateEvent: string = 'BotMessageUpdate';

    /**
     * Application Insights Custom Event name, logged when a message is deleted by the bot (rare case)
     */
    public static readonly botMsgDeleteEvent: string = 'BotMessageDelete';

    private readonly telemetryClient: TelemetryClient;
    private readonly telemetryConstants: TelemetryConstants = new TelemetryConstants();
    // tslint:disable:variable-name
    private readonly _logUsername: boolean;
    private readonly _logOriginalMessage: boolean;
    // tslint:enable:variable-name

    /**
     * Initializes a new instance of the TelemetryLoggerMiddleware class.
     * @param instrumentationKey The Application Insights instrumentation key.  See Application Insights for more information.
     * @param logUsername (Optional) Enable/Disable logging user name within Application Insights.
     * @param logOriginalMessage (Optional) Enable/Disable logging original message name within Application Insights.
     */
    constructor(telemetryClient: TelemetryClient, logUsername: boolean = false, logOriginalMessage: boolean = false) {
        if (!telemetryClient) {
            throw new Error('Error not found');
        }
        this.telemetryClient = telemetryClient;
        this._logUsername = logUsername;
        this._logOriginalMessage = logOriginalMessage;
    }

    /**
     * Gets a value indicating whether indicates whether to log the original message into the BotMessageReceived event.
     */
    public get logUsername(): boolean { return this._logUsername; }

    /**
     * Gets a value indicating whether indicates whether to log the user name into the BotMessageReceived event.
     */
    public get logOriginalMessage(): boolean { return this._logOriginalMessage; }

    /**
     * Records incoming and outgoing activities to the Application Insights store.
     * @param context The context object for this turn.
     * @param next The delegate to call to continue the bot middleware pipeline
     */
    public async onTurn(context: TurnContext, next: () => Promise<void>): Promise<void> {
        if (context === null) {
            throw new Error('context is null');
        }

        context.turnState.set(TelemetryLoggerMiddleware.appInsightsServiceKey, this.telemetryClient);

        // log incoming activity at beginning of turn
        if (context.activity !== null) {

            const activity: Activity = context.activity;

            // Log the Application Insights Bot Message Received
            this.telemetryClient.trackEvent({
                name: TelemetryLoggerMiddleware.botMsgReceiveEvent,
                properties: this.fillReceiveEventProperties(activity)
            });
        }

        // hook up onSend pipeline
        context.onSendActivities(async (ctx: TurnContext,
                                        activities: Partial<Activity>[],
                                        nextSend: () => Promise<ResourceResponse[]>): Promise<ResourceResponse[]> => {
            // run full pipeline
            const responses: ResourceResponse[] = await nextSend();

            activities.forEach((activity: Partial<Activity>) => this.telemetryClient.trackEvent({
                name: TelemetryLoggerMiddleware.botMsgSendEvent,
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

            this.telemetryClient.trackEvent({
                name: TelemetryLoggerMiddleware.botMsgSendEvent,
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

            this.telemetryClient.trackEvent({
                name: TelemetryLoggerMiddleware.botMsgSendEvent,
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

        properties[this.telemetryConstants.activityIdProperty] = activity.id || '';
        properties[this.telemetryConstants.channelIdProperty] = activity.channelId;
        properties[this.telemetryConstants.fromIdProperty] = activity.from.id || '';
        properties[this.telemetryConstants.localeProperty] = activity.locale || '';
        properties[this.telemetryConstants.recipientIdProperty] = activity.recipient.id;
        properties[this.telemetryConstants.recipientNameProperty] = activity.recipient.name;

        // For some customers,
        // logging user name within Application Insights might be an issue so have provided a config setting to disable this feature
        if (this._logUsername && activity.from.name && activity.from.name.trim()) {
            properties[this.telemetryConstants.fromNameProperty] = activity.from.name;
        }

        // For some customers,
        // logging the utterances within Application Insights might be an so have provided a config setting to disable this feature
        if (this._logOriginalMessage && activity.text && activity.text.trim()) {
            properties[this.telemetryConstants.textProperty] = activity.text;
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

        properties[this.telemetryConstants.activityIdProperty] = activity.id || '';
        properties[this.telemetryConstants.channelIdProperty] = activity.channelId;
        properties[this.telemetryConstants.replyActivityIdProperty] = activity.replyToId || '';
        properties[this.telemetryConstants.recipientIdProperty] = activity.recipient.id;
        properties[this.telemetryConstants.conversationNameProperty] = activity.conversation.name;
        properties[this.telemetryConstants.localeProperty] = activity.locale || '';
        properties[this.telemetryConstants.recipientNameProperty] = activity.recipient.name;

        // For some customers,
        // logging the utterances within Application Insights might be an so have provided a config setting to disable this feature
        if (this._logUsername && activity.recipient.name && activity.recipient.name.trim()) {
            properties[this.telemetryConstants.recipientNameProperty] = activity.recipient.name;
        }

        // For some customers,
        // logging the utterances within Application Insights might be an so have provided a config setting to disable this feature
        if (this._logOriginalMessage && activity.text && activity.text.trim()) {
            properties[this.telemetryConstants.textProperty] = activity.text;
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
        properties[this.telemetryConstants.channelIdProperty] = activity.channelId;
        properties[this.telemetryConstants.recipientIdProperty] = activity.recipient.id;
        properties[this.telemetryConstants.conversationIdProperty] = activity.conversation.id;
        properties[this.telemetryConstants.conversationNameProperty] = activity.conversation.name;
        properties[this.telemetryConstants.localeProperty] = activity.locale || '';

        // For some customers,
        // logging the utterances within Application Insights might be an so have provided a config setting to disable this feature
        if (this._logOriginalMessage && activity.text && activity.text.trim()) {
            properties[this.telemetryConstants.textProperty] = activity.text;
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
        properties[this.telemetryConstants.channelIdProperty] = activity.channelId;
        properties[this.telemetryConstants.recipientIdProperty] = activity.recipient.id;
        properties[this.telemetryConstants.conversationIdProperty] = activity.conversation.id;
        properties[this.telemetryConstants.conversationNameProperty] = activity.conversation.name;

        return properties;
    }
}
