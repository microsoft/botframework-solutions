// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { TelemetryClient } from 'applicationinsights';
import { TurnContext } from 'botbuilder';
import {
    QnAMaker,
    QnAMakerEndpoint,
    QnAMakerOptions,
    QnAMakerResult } from 'botbuilder-ai';
import { QnATelemetryConstants } from './qnaTelemetryConstants';
import { TelemetryLoggerMiddleware } from './telemetryLoggerMiddleware';

/**
 * TelemetryQnaRecognizer invokes the Qna Maker and logs some results into Application Insights.
 * Logs the score, and (optionally) questionAlong with Conversation and ActivityID.
 * The Custom Event name this logs is "QnaMessage"
 */
export class TelemetryQnAMaker extends QnAMaker {

    public static readonly QNA_MESSAGE_EVENT: string = 'QnaMessage';
    private readonly LOG_ORIGINAL_MESSAGE: boolean;
    private readonly LOG_USERNAME: boolean;
    private QNA_OPTIONS: { top: number; scoreThreshold: number } = { top: 1, scoreThreshold: 0.3 };
    private ENDPOINT: QnAMakerEndpoint;
    private QNA_TELEMETRY_CONSTANTS: QnATelemetryConstants = new QnATelemetryConstants();
    /**
     * Initializes a new instance of the TelemetryQnAMaker class.
     * @param endpoint The endpoint of the knowledge base to query.
     * @param qnaOptions The options for the QnA Maker knowledge base.
     * @param logUserName The flag to include username in logs.
     * @param logOriginalMessage The flag to include original message in logs.
     */
    constructor(
        endpoint: QnAMakerEndpoint,
        qnaOptions?: QnAMakerOptions,
        logUserName: boolean = false,
        logOriginalMessage: boolean = false) {
        super(endpoint, qnaOptions);

        this.LOG_USERNAME = logUserName;
        this.LOG_ORIGINAL_MESSAGE = logOriginalMessage;
        this.ENDPOINT = endpoint;
        Object.assign(this.QNA_OPTIONS, qnaOptions);
    }

    /**
     * Gets a value indicating whether determines whether to log the User name.
     */
    public get logUserName(): boolean { return this.LOG_USERNAME; }

    /**
     * Gets a value indicating whether determines whether to log the Activity message text that came from the user.
     */
    public get logOriginalMessage(): boolean { return this.LOG_ORIGINAL_MESSAGE; }

    public async getAnswersAsync(context: TurnContext): Promise<QnAMakerResult[]> {
        // Call Qna Maker
        const queryResults: QnAMakerResult[] = await super.generateAnswer(
            context.activity.text,
            this.QNA_OPTIONS.top,
            this.QNA_OPTIONS.scoreThreshold);

        // Find the Application Insights Telemetry Client
        if (queryResults && context.turnState.has(TelemetryLoggerMiddleware.APP_INSIGHTS_SERVICE_KEY)) {
            const telemetryClient: TelemetryClient = context.turnState.get(TelemetryLoggerMiddleware.APP_INSIGHTS_SERVICE_KEY);

            const properties: { [key: string]: string } = {};
            const metrics: { [key: string]: number } = {};

            properties[this.QNA_TELEMETRY_CONSTANTS.KNOWLEDGE_BASE_ID_PROPERTY] = this.ENDPOINT.knowledgeBaseId;
            const conversationId: string = context.activity.conversation.id;
            if (conversationId && conversationId.trim()) {
                properties[this.QNA_TELEMETRY_CONSTANTS.CONVERSATION_ID_PROPERTY] = conversationId;
            }

            // For some customers, logging original text name within Application Insights might be an issue
            const text: string = context.activity.text;
            if (this.logOriginalMessage && text && text.trim()) {
                properties[this.QNA_TELEMETRY_CONSTANTS.ORIGINAL_QUESTION_PROPERTY] = text;
            }

            // For some customers, logging user name within Application Insights might be an issue
            const name: string = context.activity.from.name;
            if (this.logUserName && name && name.trim()) {
                properties[this.QNA_TELEMETRY_CONSTANTS.USERNAME_PROPERTY] = name;
            }

            // Fill in Qna Results (found or not)
            if (queryResults.length > 0) {
                const queryResult: QnAMakerResult = queryResults[0];

                properties[this.QNA_TELEMETRY_CONSTANTS.QUESTION_PROPERTY] = Array.of(queryResult.questions)
                                                                                .join(',');
                properties[this.QNA_TELEMETRY_CONSTANTS.ANSWER_PROPERTY] = queryResult.answer;
                metrics[this.QNA_TELEMETRY_CONSTANTS.SCORE_PROPERTY] = queryResult.score;
                properties[this.QNA_TELEMETRY_CONSTANTS.ARTICLE_FOUND_PROPERTY] = 'true';
            } else {
                properties[this.QNA_TELEMETRY_CONSTANTS.QUESTION_PROPERTY] = 'No Qna Question matched';
                properties[this.QNA_TELEMETRY_CONSTANTS.ANSWER_PROPERTY] = 'No Qna Question matched';
                properties[this.QNA_TELEMETRY_CONSTANTS.ARTICLE_FOUND_PROPERTY] = 'true';
            }

            // Track the event
            telemetryClient.trackEvent({
                measurements: metrics,
                name: TelemetryQnAMaker.QNA_MESSAGE_EVENT,
                properties
            });
        }

        return queryResults;
    }
}
