/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Activity, BotTelemetryClient, Severity } from 'botbuilder';
import { TelemetryConstants } from './telemetryLoggerMiddleware';

export namespace TelemetryExtensions {

    export function trackEventEx(client: BotTelemetryClient, eventName: string, activity: Partial<Activity>, dialogId?: string,
                                 properties?: Map<string, string>, metrics?: { [key: string]: number }): void {
        client.trackEvent({
            name: eventName,
            properties: getFinalProperties(activity, dialogId, properties),
            metrics: metrics
        });
    }

    export function trackExceptionEx(client: BotTelemetryClient, exception: Error, activity: Partial<Activity>, dialogId?: string,
                                     properties?: Map<string, string>, metrics?: { [key: string]: number }): void {
        client.trackException({
            exception: exception,
            properties: getFinalProperties(activity, dialogId, properties),
            measurements: metrics
        });
    }

    export function trackTraceEx(client: BotTelemetryClient, message: string, severity: Severity, activity: Partial<Activity>,
                                 dialogId?: string, properties?: Map<string, string>): void {
        client.trackTrace({
            message: message,
            properties: getFinalProperties(activity, dialogId, properties),
            severityLevel: severity
        });
    }

    function getFinalProperties(activity: Partial<Activity>, dialogId?: string, props?: Map<string, string>): { [key: string]: string } {
        const finalProps: { [key: string]: string } = {};

        if (dialogId) {
            finalProps[TelemetryConstants.dialogIdProperty] = dialogId;
        }

        if (props) {
            props.forEach((value: string, key: string) => {
                finalProps[key] = value;
            });
        }

        return finalProps;
    }
}
