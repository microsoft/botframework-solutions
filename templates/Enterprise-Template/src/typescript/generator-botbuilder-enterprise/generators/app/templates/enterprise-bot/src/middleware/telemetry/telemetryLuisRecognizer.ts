// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { TelemetryClient } from 'applicationinsights';
import {
    RecognizerResult,
    TurnContext } from 'botbuilder';
import {
    LuisApplication,
    LuisPredictionOptions,
    LuisRecognizer } from 'botbuilder-ai';
import { DialogContext } from 'botbuilder-dialogs';
import { LuisTelemetryConstants } from './luisTelemetryConstants';
import { TelemetryLoggerMiddleware } from './telemetryLoggerMiddleware';

/**
 * TelemetryLuisRecognizer invokes the Luis Recognizer and logs some results into Application Insights.
 * Logs the Top Intent, Sentiment (label/score), (Optionally) Original Text Along with Conversation
 * and ActivityID.
 * The Custom Event name this logs is MyLuisConstants.IntentPrefix + "." + 'found intent name'
 * For example, if intent name was "add_calender": LuisIntent.add_calendar
 */
export class TelemetryLuisRecognizer extends LuisRecognizer {
    private readonly luisApplication: LuisApplication;
    private readonly luisTelemetryConstants: LuisTelemetryConstants = new LuisTelemetryConstants();
    // tslint:disable:variable-name
    private readonly _logOriginalMessage: boolean;
    private readonly _logUsername: boolean;
    // tslint:enable:variable-name

    /**
     * Initializes a new instance of the TelemetryLuisRecognizer class.
     * @param application The LUIS application to use to recognize text.
     * @param predictionOptions The LUIS prediction options to use.
     * @param includeApiResults TRUE to include raw LUIS API response.
     * @param logOriginalMessage TRUE to include original user message.
     * @param logUsername TRUE to include user name.
     */
    constructor(
        application: LuisApplication,
        predictionOptions?: LuisPredictionOptions,
        includeApiResults: boolean = false,
        logOriginalMessage: boolean = false,
        logUsername: boolean = false) {
        super(application, predictionOptions, includeApiResults);
        this._logOriginalMessage = logOriginalMessage;
        this._logUsername = logUsername;
        this.luisApplication = application;
    }
    /**
     * Gets a value indicating whether determines whether to log the Activity message text that came from the user.
     * value - If true, will log the Activity Message text into the AppInsights Custom Event for Luis intents.
     */
    public get logOriginalMessage(): boolean { return this._logOriginalMessage; }

    /**
     * Gets a value indicating whether determines whether to log the User name.
     * value - If true, will log the user name into the AppInsights Custom Event for Luis intents.
     */
    public get logUsername(): boolean { return this._logUsername; }

    /**
     * Return results of the analysis (Suggested actions and intents), passing the dialog id from dialog context to the TelemetryClient.
     * @param dialogContext - Dialog context object containing information for the dialog being executed.
     * @param logOriginalMessage - Determines if the original message is logged into Application Insights.  This is a privacy consideration.
     * @returns The LUIS results of the analysis of the current message text in the current turn's context activity.
     */
    public async recognizeDialog(dialogContext: DialogContext, logOriginalMessage: boolean = true): Promise<RecognizerResult> {
        if (dialogContext === null) {
            throw new Error ('Error');
        }

        return this.recognizeInternal(
            dialogContext.context,
            logOriginalMessage,
            dialogContext.activeDialog ? dialogContext.activeDialog.id : undefined);
    }
    /**
     * Return results of the analysis (Suggested actions and intents), using the turn context.
     * This is missing a dialog id used for telemetry.
     * @param context - Context object containing information for a single turn of conversation with a user.
     * @param logOriginalMessage - Determines if the original message is logged into Application Insights.  This is a privacy consideration.
     * @returns The LUIS results of the analysis of the current message text in the current turn's context activity.
     */
    public async recognizeTurn(context: TurnContext, logOriginalMessage: boolean = true): Promise<RecognizerResult> {
        return this.recognizeInternal(context, logOriginalMessage);
    }

    /**
     * Analyze the current message text and return results of the analysis (Suggested actions and intents).
     * @param context Context object containing information for a single turn of conversation with a user.
     * @param logOriginalMessage Determines if the original message is logged into Application Insights. This is a privacy consideration.
     * @returns The LUIS results of the analysis of the current message text in the current turn's context activity.
     */
    private async recognizeInternal(
        context: TurnContext,
        logOriginalMessage: boolean = false,
        dialogId?: string): Promise<RecognizerResult> {

        if (context === null) {
            throw new Error('context is null');
        }

        // Call Luis Recognizer
        const recognizerResult: RecognizerResult = await super.recognize(context);

        const conversationId: string = context.activity.conversation.id;

        // Find the Telemetry Client
        if (recognizerResult && context.turnState.has(TelemetryLoggerMiddleware.appInsightsServiceKey)) {
            const telemetryClient: TelemetryClient = context.turnState.get(TelemetryLoggerMiddleware.appInsightsServiceKey);
            const topLuisIntent: string = LuisRecognizer.topIntent(recognizerResult);
            const intentScore: number = recognizerResult.intents[topLuisIntent].score;

            // Add the intent score and conversation id properties
            const properties: { [key: string]: string } = {};
            properties[this.luisTelemetryConstants.applicationId] = this.luisApplication.applicationId;
            properties[this.luisTelemetryConstants.intentProperty] = topLuisIntent;
            properties[this.luisTelemetryConstants.intentScoreProperty] = intentScore.toString();

            if (dialogId !== undefined) {
                properties[this.luisTelemetryConstants.dialogId] = dialogId;
            }

            if (recognizerResult.sentiment) {
                if (recognizerResult.sentiment.label) {
                    properties[this.luisTelemetryConstants.sentimentLabelProperty] = recognizerResult.sentiment.label;
                }

                if (recognizerResult.sentiment.score) {
                    properties[this.luisTelemetryConstants.sentimentScoreProperty] = recognizerResult.sentiment.score.toString();
                }
            }

            if (conversationId) {
                properties[this.luisTelemetryConstants.conversationIdProperty] = conversationId;
            }

            //For some customers,
            //logging user name within Application Insights might be an issue so have provided a config setting to disable this feature
            if (logOriginalMessage && context.activity.text) {
                properties[this.luisTelemetryConstants.questionProperty] = context.activity.text;
            }

            // Track the event
            telemetryClient.trackEvent({
                name: `${this.luisTelemetryConstants.intentPrefix}.${topLuisIntent}`,
                properties
            });
        }

        return recognizerResult;
    }
}
