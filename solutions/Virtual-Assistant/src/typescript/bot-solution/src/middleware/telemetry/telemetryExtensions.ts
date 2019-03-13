import { Activity, BotTelemetryClient, Severity } from 'botbuilder';
import { TelemetryConstants } from './telemetryLoggerMiddleware';

export namespace TelemetryExtensions {
    const noActivityId: string = 'no activity id';
    const noChannelId: string = 'no channel id';
    const noConversationId: string = 'no conversation id';
    const noDialogId: string = 'no dialog id';
    const noUserId: string = 'no user id';

    export function trackEventEx(client: BotTelemetryClient, eventName: string, activity: Partial<Activity>, dialogId?: string,
                                 properties?: { [key: string]: string }, metrics?: { [key: string]: number }): void {
        client.trackEvent({
            name: eventName,
            properties: getFinalProperties(activity, dialogId, properties),
            metrics: metrics
        });
    }

    export function trackExceptionEx(client: BotTelemetryClient, exception: Error, activity: Partial<Activity>, dialogId?: string,
                                     properties?: { [key: string]: string }, metrics?: { [key: string]: number }): void {
        client.trackException({
            exception: exception,
            properties: getFinalProperties(activity, dialogId, properties),
            measurements: metrics
        });
    }

    export function trackTraceEx(client: BotTelemetryClient, message: string, severity: Severity, activity: Partial<Activity>,
                                 dialogId?: string, properties?: { [key: string]: string }): void {
        client.trackTrace({
            message: message,
            properties: getFinalProperties(activity, dialogId, properties),
            severityLevel: severity
        });
    }

    function getFinalProperties(activity: Partial<Activity>, dialogId?: string,
                                properties?: { [key: string]: string }): { [key: string]: string } {
        const finalProps: { [key: string]: string } = {};

        finalProps[TelemetryConstants.activeDialogIdProperty] = dialogId || noDialogId;

        finalProps[TelemetryConstants.activityIDProperty] = activity.id || noActivityId;
        finalProps[TelemetryConstants.channelIdProperty] = activity.channelId || noChannelId;
        finalProps[TelemetryConstants.conversationIdProperty] = activity.conversation ? activity.conversation.id : noConversationId;
        finalProps[TelemetryConstants.userIdProperty] = activity.from ? activity.from.id : noUserId;

        if (properties) {
            Object.assign(finalProps, properties);
        }

        return finalProps;
    }
}
