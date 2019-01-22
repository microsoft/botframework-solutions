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
    private readonly LUIS_APPLICATION: LuisApplication;
    private readonly LOG_ORIGINAL_MESSAGE: boolean;
    private readonly LOG_USERNAME: boolean;
    private readonly LUIS_TELEMETRY_CONSTANTS: LuisTelemetryConstants = new LuisTelemetryConstants();
    /**
     * Initializes a new instance of the TelemetryLuisRecognizer class.
     * @param application The LUIS application to use to recognize text.
     * @param predictionOptions The LUIS prediction options to use.
     * @param includeApiResults TRUE to include raw LUIS API response.
     * @param logOriginalMessage TRUE to include original user message.
     * @param logUserName TRUE to include user name.
     */
    constructor(
        application: LuisApplication,
        predictionOptions?: LuisPredictionOptions,
        includeApiResults: boolean = false,
        logOriginalMessage: boolean = false,
        logUserName: boolean = false) {
        super(application, predictionOptions, includeApiResults);
        this.LOG_ORIGINAL_MESSAGE = logOriginalMessage;
        this.LOG_USERNAME = logUserName;
        this.LUIS_APPLICATION = application;
    }
    /**
     * Gets a value indicating whether determines whether to log the Activity message text that came from the user.
     */
    public get logOriginalMessage(): boolean { return this.LOG_ORIGINAL_MESSAGE; }

    /**
     * Gets a value indicating whether determines whether to log the User name.
     */
    public get logUsername(): boolean { return this.LOG_USERNAME; }

    /**
     * Analyze the current message text and return results of the analysis (Suggested actions and intents).
     * @param context Context object containing information for a single turn of conversation with a user.
     * @param logOriginalMessage Determines if the original message is logged into Application Insights. This is a privacy consideration.
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

    public async recognizeTurn(context: TurnContext, logOriginalMessage: boolean = true): Promise<RecognizerResult> {
        return this.recognizeInternal(context, logOriginalMessage);
    }

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
        if (recognizerResult && context.turnState.has(TelemetryLoggerMiddleware.APP_INSIGHTS_SERVICE_KEY)) {
            const telemetryClient: TelemetryClient = context.turnState.get(TelemetryLoggerMiddleware.APP_INSIGHTS_SERVICE_KEY);
            const topLuisIntent: string = LuisRecognizer.topIntent(recognizerResult);
            const intentScore: number = recognizerResult.intents[topLuisIntent].score;

            // Add the intent score and conversation id properties
            const properties: { [key: string]: string } = {};
            properties[this.LUIS_TELEMETRY_CONSTANTS.APPLICATION_ID] = this.LUIS_APPLICATION.applicationId;
            properties[this.LUIS_TELEMETRY_CONSTANTS.INTENT_PROPERTY] = topLuisIntent;
            properties[this.LUIS_TELEMETRY_CONSTANTS.INTENT_SCORE_PROPERTY] = intentScore.toString();

            if (dialogId !== undefined) {
                properties[this.LUIS_TELEMETRY_CONSTANTS.DIALOG_ID] = dialogId;
            }

            if (recognizerResult.sentiment) {
                if (recognizerResult.sentiment.label) {
                    properties[this.LUIS_TELEMETRY_CONSTANTS.SENTIMENT_LABEL_PROPERTY] = recognizerResult.sentiment.label;
                }

                if (recognizerResult.sentiment.score) {
                    properties[this.LUIS_TELEMETRY_CONSTANTS.SENTIMENT_SCORE_PROPERTY] = recognizerResult.sentiment.score.toString();
                }
            }

            if (conversationId) {
                properties[this.LUIS_TELEMETRY_CONSTANTS.CONVERSATION_ID_PROPERTY] = conversationId;
            }

            //For some customers,
            //logging user name within Application Insights might be an issue so have provided a config setting to disable this feature
            if (logOriginalMessage && context.activity.text) {
                properties[this.LUIS_TELEMETRY_CONSTANTS.QUESTION_PROPERTY] = context.activity.text;
            }

            // Track the event
            telemetryClient.trackEvent({
                name: `${this.LUIS_TELEMETRY_CONSTANTS.INTENT_PREFIX}.${topLuisIntent}`,
                properties
            });
        }

        return recognizerResult;
    }
}
