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

    public static readonly qnaMessageEvent: string = 'QnaMessage';
    // tslint:disable:variable-name
    private readonly _logOriginalMessage: boolean;
    private readonly _logUsername: boolean;
    // tslint:enable:variable-name
    private qnaOptions: { top: number; scoreThreshold: number } = { top: 1, scoreThreshold: 0.3 };
    private qnaMakerEndpoint: QnAMakerEndpoint;
    private qnaTelemetryConstants: QnATelemetryConstants = new QnATelemetryConstants();
    /**
     * Initializes a new instance of the TelemetryQnAMaker class.
     * @param endpoint The endpoint of the knowledge base to query.
     * @param qnaOptions The options for the QnA Maker knowledge base.
     * @param logUsername The flag to include username in logs.
     * @param logOriginalMessage The flag to include original message in logs.
     */
    constructor(
        endpoint: QnAMakerEndpoint,
        qnaOptions?: QnAMakerOptions,
        logUsername: boolean = false,
        logOriginalMessage: boolean = false) {
        super(endpoint, qnaOptions);

        this._logUsername = logUsername;
        this._logOriginalMessage = logOriginalMessage;
        this.qnaMakerEndpoint = endpoint;
        Object.assign(this.qnaOptions, qnaOptions);
    }

    /**
     * Gets a value indicating whether determines whether to log the User name.
     */
    public get logUsername(): boolean { return this._logUsername; }

    /**
     * Gets a value indicating whether determines whether to log the Activity message text that came from the user.
     */
    public get logOriginalMessage(): boolean { return this._logOriginalMessage; }

    public async getAnswersAsync(context: TurnContext): Promise<QnAMakerResult[]> {
        // Call Qna Maker
        const queryResults: QnAMakerResult[] = await super.generateAnswer(
            context.activity.text,
            this.qnaOptions.top,
            this.qnaOptions.scoreThreshold);

        // Find the Application Insights Telemetry Client
        if (queryResults && context.turnState.has(TelemetryLoggerMiddleware.appInsightsServiceKey)) {
            const telemetryClient: TelemetryClient = context.turnState.get(TelemetryLoggerMiddleware.appInsightsServiceKey);

            const properties: { [key: string]: string } = {};
            const metrics: { [key: string]: number } = {};

            properties[this.qnaTelemetryConstants.knowledgeBaseIdProperty] = this.qnaMakerEndpoint.knowledgeBaseId;
            const conversationId: string = context.activity.conversation.id;
            if (conversationId && conversationId.trim()) {
                properties[this.qnaTelemetryConstants.conversationIdProperty] = conversationId;
            }

            // For some customers, logging original text name within Application Insights might be an issue
            const text: string = context.activity.text;
            if (this._logOriginalMessage && text && text.trim()) {
                properties[this.qnaTelemetryConstants.originalQuestionProperty] = text;
            }

            // For some customers, logging user name within Application Insights might be an issue
            const name: string = context.activity.from.name;
            if (this._logUsername && name && name.trim()) {
                properties[this.qnaTelemetryConstants.usernameProperty] = name;
            }

            // Fill in Qna Results (found or not)
            if (queryResults.length > 0) {
                const queryResult: QnAMakerResult = queryResults[0];

                properties[this.qnaTelemetryConstants.questionProperty] = Array.of(queryResult.questions)
                                                                                .join(',');
                properties[this.qnaTelemetryConstants.answerProperty] = queryResult.answer;
                metrics[this.qnaTelemetryConstants.scoreProperty] = queryResult.score;
                properties[this.qnaTelemetryConstants.articleFoundProperty] = 'true';
            } else {
                properties[this.qnaTelemetryConstants.questionProperty] = 'No Qna Question matched';
                properties[this.qnaTelemetryConstants.answerProperty] = 'No Qna Question matched';
                properties[this.qnaTelemetryConstants.articleFoundProperty] = 'true';
            }

            // Track the event
            telemetryClient.trackEvent({
                measurements: metrics,
                name: TelemetryQnAMaker.qnaMessageEvent,
                properties
            });
        }

        return queryResults;
    }
}
