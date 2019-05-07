/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotTelemetryClient, RecognizerResult, TurnContext } from 'botbuilder';
import { LuisApplication, LuisPredictionOptions, LuisRecognizer } from 'botbuilder-ai';
import { DialogContext } from 'botbuilder-dialogs';

import { TelemetryExtensions } from './telemetryExtensions';
import { TelemetryConstants, TelemetryLoggerMiddleware } from './telemetryLoggerMiddleware';

export interface ITelemetryLuisRecognizer {
    recognize(context: TurnContext | DialogContext, logOriginalMessage?: boolean): Promise<RecognizerResult>;
}

export namespace LuisTelemetryConstants {
    export const applicationIdProperty: string = 'applicationId';
    export const intentPrefix: string = 'LuisResult';
    export const intentProperty: string = 'intent';
    export const intentScoreProperty: string = 'intentScore';
    export const entitiesProperty: string = 'entities';
    export const questionProperty: string = 'question';
    export const activityIdProperty: string = 'activityId';
    export const sentimentLabelProperty: string = 'sentimentLabel';
    export const sentimentScoreProperty: string = 'sentimentScore';
    export const fromIdProperty: string = 'fromId';
}

export class TelemetryLuisRecognizer extends LuisRecognizer implements ITelemetryLuisRecognizer {
    private readonly luisApplication: LuisApplication;

    constructor(luisApplication: LuisApplication, predictionOptions?: LuisPredictionOptions,
                includeApiResults: boolean = false) {
        super(luisApplication, predictionOptions, includeApiResults);
        this.luisApplication = luisApplication;
    }

    public recognize(context: TurnContext | DialogContext): Promise<RecognizerResult> {
        const internalContext: TurnContext = context instanceof DialogContext ? context.context : context;
        let dialogId: string|undefined;

        if (context instanceof DialogContext && context.activeDialog) {
            dialogId = context.activeDialog.id;
        }

        return this.recognizeInternal(internalContext, dialogId);
    }

    private async recognizeInternal(context: TurnContext, dialogId?: string): Promise<RecognizerResult> {
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
            const telemetryProperties: Map<string, string> = new Map();
            telemetryProperties.set(LuisTelemetryConstants.applicationIdProperty, this.luisApplication.applicationId);
            telemetryProperties.set(LuisTelemetryConstants.intentProperty, topLuisIntent);
            telemetryProperties.set(LuisTelemetryConstants.intentScoreProperty, topIntentScore.toString());
            telemetryProperties.set(LuisTelemetryConstants.fromIdProperty, context.activity.from.id);

            if (dialogId) {
                telemetryProperties.set(TelemetryConstants.dialogIdProperty, dialogId);
            }

            if (recognizerResult.sentiment) {
                const label: string = recognizerResult.sentiment.label;
                const score: string = recognizerResult.sentiment.score;

                telemetryProperties.set(LuisTelemetryConstants.sentimentLabelProperty, label);
                telemetryProperties.set(LuisTelemetryConstants.sentimentScoreProperty, score);
            }

            const entities: string = JSON.stringify(recognizerResult.entities);
            telemetryProperties.set(LuisTelemetryConstants.entitiesProperty, entities);

            // Use the LogPersonalInformation flag to toggle logging PII data, text is a common example
            if (this.logPersonalInformation && context.activity.text) {
                telemetryProperties.set(LuisTelemetryConstants.questionProperty, context.activity.text);
            }

            // Track the event
            TelemetryExtensions.trackEventEx(
                telemetryClient, LuisTelemetryConstants.intentPrefix, context.activity, undefined, telemetryProperties);
        }

        return recognizerResult;
    }
}
