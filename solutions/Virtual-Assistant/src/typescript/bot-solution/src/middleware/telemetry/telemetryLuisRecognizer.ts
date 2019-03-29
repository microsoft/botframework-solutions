/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotTelemetryClient, RecognizerResult, TurnContext } from 'botbuilder';
import { LuisApplication, LuisPredictionOptions, LuisRecognizer } from 'botbuilder-ai';
import { DialogContext } from 'botbuilder-dialogs';

import { TelemetryExtensions } from './telemetryExtensions';
import { TelemetryLoggerMiddleware } from './telemetryLoggerMiddleware';

export interface ITelemetryLuisRecognizer {
    logOriginalMessage: boolean;
    logUserName: boolean;
    recognize(context: TurnContext | DialogContext, logOriginalMessage?: boolean): Promise<RecognizerResult>;
}

export namespace LuisTelemetryConstants {
    export const applicationIdProperty: string = 'applicationId';
    export const intentPrefix: string = 'luisIntent';  // Application Insights Custom Event name (with Intent)
    export const intentProperty: string = 'intent';
    export const intentScoreProperty: string = 'intentScore';
    export const conversationIdProperty: string = 'conversationId';
    export const questionProperty: string = 'question';
    export const activityIdProperty: string = 'activityId';
    export const sentimentLabelProperty: string = 'sentimentLabel';
    export const sentimentScoreProperty: string = 'sentimentScore';
    export const dialogId: string = 'dialogId';
}

export class TelemetryLuisRecognizer extends LuisRecognizer implements ITelemetryLuisRecognizer {
    private readonly luisApplication: LuisApplication;

    public readonly logOriginalMessage: boolean;
    public readonly logUserName: boolean;

    constructor(luisApplication: LuisApplication, predictionOptions?: LuisPredictionOptions,
                includeApiResults: boolean = false, logUserName: boolean = false, logOriginalMessage: boolean = false) {
        super(luisApplication, predictionOptions, includeApiResults);
        this.luisApplication = luisApplication;
        this.logUserName = logUserName;
        this.logOriginalMessage = logOriginalMessage;
    }

    public recognize(context: TurnContext | DialogContext, logOriginalMessage?: boolean): Promise<RecognizerResult> {
        const internalContext: TurnContext = context instanceof DialogContext ? context.context : context;
        let dialogId: string|undefined;

        if (context instanceof DialogContext && context.activeDialog) {
            dialogId = context.activeDialog.id;
        }

        return this.recognizeInternal(internalContext, logOriginalMessage || false, dialogId);
    }

    private async recognizeInternal(context: TurnContext, logOriginalMessage: boolean, dialogId?: string): Promise<RecognizerResult> {
        if (!context) {
            throw new Error('No context.');
        }

        // Call Luis Recognizer
        const recognizerResult: RecognizerResult = await super.recognize(context);

        // Find the Telemetry Client
        if (context.turnState.has(TelemetryLoggerMiddleware.appInsightsServiceKey) && recognizerResult) {
            const telemetryClient: BotTelemetryClient = <BotTelemetryClient>
                context.turnState.get(TelemetryLoggerMiddleware.appInsightsServiceKey);
            const topLuisIntent: string = LuisRecognizer.topIntent(recognizerResult);
            const topIntentScore: number = recognizerResult.intents[topLuisIntent].score;

            // Add the intent score and conversation id properties
            const telemetryProperties: { [key: string]: string} = {};
            telemetryProperties[LuisTelemetryConstants.applicationIdProperty] = this.luisApplication.applicationId;
            telemetryProperties[LuisTelemetryConstants.intentProperty] = topLuisIntent;
            telemetryProperties[LuisTelemetryConstants.intentScoreProperty] = topIntentScore.toString();

            if (dialogId) {
                telemetryProperties[LuisTelemetryConstants.dialogId] = dialogId;
            }

            if (recognizerResult.sentiment) {
                const label: string = recognizerResult.sentiment.label;
                const score: string = recognizerResult.sentiment.score;

                telemetryProperties[LuisTelemetryConstants.sentimentLabelProperty] = label;
                telemetryProperties[LuisTelemetryConstants.sentimentScoreProperty] = score;
            }

            // For some customers, logging user name within Application Insights might be an issue
            // so have provided a config setting to disable this feature
            if (logOriginalMessage && context.activity.text) {
                telemetryProperties[LuisTelemetryConstants.questionProperty] = context.activity.text;
            }

            // Track the event
            TelemetryExtensions.trackEventEx(
                telemetryClient, `${LuisTelemetryConstants.intentPrefix}.${topLuisIntent}`,
                context.activity, undefined, telemetryProperties);
        }

        return recognizerResult;
    }
}
