import { Activity, ActivityTypes, BotTelemetryClient, ConversationReference, Middleware, ResourceResponse, TurnContext } from 'botbuilder';
import { TelemetryExtensions } from './telemetryExtensions';

export namespace TelemetryConstants {
    export const activeDialogIdProperty: string = 'activeDialogId';
    export const activityIDProperty: string = 'activityId';
    export const channelIdProperty: string = 'channelId';
    export const conversationIdProperty: string = 'conversationId';
    export const conversationNameProperty: string = 'conversationName';
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

export class TelemetryLoggerMiddleware implements Middleware {
    public static readonly appInsightsServiceKey: string = `${TelemetryLoggerMiddleware.name}.AppInsightsContext`;
    public static readonly botMsgReceiveEvent: string = 'BotMessageReceive';
    public static readonly botMsgSendEvent: string = 'BotMessageSend';
    public static readonly botMsgUpdateEvent: string = 'BotMessageUpdate';
    public static readonly botMsgDeleteEvent: string = 'BotMessageDelete';

    private readonly telemetryClient: BotTelemetryClient;
    public readonly logUserName: boolean;
    public readonly logOriginalMessage: boolean;

    constructor(telemetryClient: BotTelemetryClient, logUserName: boolean = false, logOriginalMessage: boolean = false) {
        this.telemetryClient = telemetryClient;
        this.logUserName = logUserName;
        this.logOriginalMessage = logOriginalMessage;
    }

    public async onTurn(context: TurnContext, next: () => Promise<void>): Promise<void> {
        if (!context) {
            throw new Error('Context not found.');
        }

        context.turnState.set(TelemetryLoggerMiddleware.appInsightsServiceKey, this.telemetryClient);

        // log incoming activity at beginning of turn
        if (context.activity) {
            const activity: Activity = context.activity;

            TelemetryExtensions.trackEventEx(
                this.telemetryClient, TelemetryLoggerMiddleware.botMsgReceiveEvent,
                activity, undefined, this.fillReceiveEventProperties(activity));
        }

        // hook up onSend pipeline
        context.onSendActivities(async (ctx: TurnContext, activities: Partial<Activity>[],
                                        nextSend: () => Promise<ResourceResponse[]>): Promise<ResourceResponse[]> => {
            // run full pipeline
            const responses: ResourceResponse[] = await nextSend();

            activities.forEach((activity: Partial<Activity>) => {
                TelemetryExtensions.trackEventEx(
                    this.telemetryClient, TelemetryLoggerMiddleware.botMsgSendEvent,
                    activity, undefined, this.fillSendEventProperties(activity));
            });

            return responses;
        });

        // hook up update activity pipeline
        context.onUpdateActivity(async (ctx: TurnContext, activity: Partial<Activity>,
                                        nextUpdate: () => Promise<void>): Promise<void> => {
            // run full pipeline
            await nextUpdate();

            TelemetryExtensions.trackEventEx(
                this.telemetryClient, TelemetryLoggerMiddleware.botMsgUpdateEvent,
                activity, undefined, this.fillUpdateEventProperties(activity));
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
                this.telemetryClient, TelemetryLoggerMiddleware.botMsgDeleteEvent,
                deleteActivity, undefined, this.fillDeleteEventProperties(deleteActivity));
        });

        if (next) {
            await next();
        }
    }

    private fillReceiveEventProperties(activity: Activity): { [key: string]: string } | undefined {
        const properties: { [key: string]: string } = {};

        properties[TelemetryConstants.channelIdProperty] = activity.channelId;
        properties[TelemetryConstants.fromIdProperty] = activity.from.id;
        properties[TelemetryConstants.conversationNameProperty] = activity.conversation.name;
        properties[TelemetryConstants.localeProperty] = activity.locale || '';
        properties[TelemetryConstants.recipientIdProperty] = activity.recipient.id;
        properties[TelemetryConstants.recipientNameProperty] = activity.recipient.name;

        // For some customers, logging user name within Application Insights might be an issue
        // so have provided a config setting to disable this feature
        if (this.logUserName && activity.from.name) {
            properties[TelemetryConstants.fromNameProperty] = activity.from.name;
        }

        // For some customers, logging the utterances within Application Insights might be an issue
        // so have provided a config setting to disable this feature
        if (this.logOriginalMessage && activity.text) {
            properties[TelemetryConstants.textProperty] = activity.text;
        }

        return properties;
    }

    private fillSendEventProperties(activity: Partial<Activity>): { [key: string]: string } | undefined {
        const properties: { [key: string]: string } = {};

        properties[TelemetryConstants.channelIdProperty] = activity.channelId || '';
        properties[TelemetryConstants.replyActivityIDProperty] = activity.replyToId || '';
        properties[TelemetryConstants.recipientIdProperty] = activity.recipient ? activity.recipient.id : '';
        properties[TelemetryConstants.conversationNameProperty] = activity.conversation ? activity.conversation.name : '';
        properties[TelemetryConstants.localeProperty] = activity.locale || '';

        // For some customers, logging user name within Application Insights might be an issue
        // so have provided a config setting to disable this feature
        if (this.logUserName && activity.recipient) {
            properties[TelemetryConstants.recipientNameProperty] = activity.recipient.name;
        }

        // For some customers, logging the utterances within Application Insights might be an issue
        // so have provided a config setting to disable this feature
        if (this.logOriginalMessage && activity.text) {
            properties[TelemetryConstants.textProperty] = activity.text;
        }

        return properties;
    }

    private fillUpdateEventProperties(activity: Partial<Activity>): { [key: string]: string } | undefined {
        const properties: { [key: string]: string } = {};

        properties[TelemetryConstants.channelIdProperty] = activity.channelId || '';
        properties[TelemetryConstants.recipientIdProperty] = activity.recipient ? activity.recipient.id : '';
        properties[TelemetryConstants.conversationIdProperty] = activity.conversation ? activity.conversation.id : '';
        properties[TelemetryConstants.conversationNameProperty] = activity.conversation ? activity.conversation.name : '';
        properties[TelemetryConstants.localeProperty] = activity.locale || '';

        // For some customers, logging the utterances within Application Insights might be an issue
        // so have provided a config setting to disable this feature
        if (this.logOriginalMessage && activity.text) {
            properties[TelemetryConstants.textProperty] = activity.text;
        }

        return properties;
    }

    private fillDeleteEventProperties(activity: Partial<Activity>): { [key: string]: string } | undefined {
        const properties: { [key: string]: string } = {};

        properties[TelemetryConstants.channelIdProperty] = activity.channelId || '';
        properties[TelemetryConstants.recipientIdProperty] = activity.recipient ? activity.recipient.id : '';
        properties[TelemetryConstants.conversationIdProperty] = activity.conversation ? activity.conversation.id : '';
        properties[TelemetryConstants.conversationNameProperty] = activity.conversation ? activity.conversation.name : '';

        return properties;
    }
}
