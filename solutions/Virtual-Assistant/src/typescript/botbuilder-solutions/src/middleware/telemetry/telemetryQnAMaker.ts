/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotTelemetryClient, TurnContext } from 'botbuilder';
import { QnAMaker, QnAMakerEndpoint, QnAMakerOptions, QnAMakerResult } from 'botbuilder-ai';

import { TelemetryExtensions } from './telemetryExtensions';
import { TelemetryLoggerMiddleware } from './telemetryLoggerMiddleware';

export interface ITelemetryQnAMaker {
    logOriginalMessage: boolean;
    logUserName: boolean;
    getAnswers(context: TurnContext): Promise<QnAMakerResult[]>;
}

export namespace QnATelemetryConstants {
    export const knowledgeBaseIdProperty: string = 'knowledgeBaseId';
    export const activityIdProperty: string = 'activityId';
    export const answerProperty: string = 'answer';
    export const articleFoundProperty: string = 'articleFound';
    export const channelIdProperty: string = 'channelId';
    export const conversationIdProperty: string = 'conversationId';
    export const originalQuestionProperty: string = 'originalQuestion';
    export const questionProperty: string = 'question';
    export const scoreProperty: string = 'score';
    export const usernameProperty: string = 'username';
}

export class TelemetryQnAMaker extends QnAMaker implements ITelemetryQnAMaker {
    public readonly logOriginalMessage: boolean;
    public readonly logUserName: boolean;

    public readonly qnaMsgEvent: string = 'QnaMessage';
    private innerEndpoint: QnAMakerEndpoint;

    constructor(endpoint: QnAMakerEndpoint, options?: QnAMakerOptions,
                logUserName: boolean = false, logOriginalMessages: boolean = false) {
        super(endpoint, options);
        this.logUserName = logUserName;
        this.logOriginalMessage = logOriginalMessages;

        this.innerEndpoint = endpoint;
    }

    public async getAnswers(context: TurnContext): Promise<QnAMakerResult[]> {
        // Call Qna Maker
        const queryResults: QnAMakerResult[] = await super.getAnswers(context);

        // Find the Application Insights Telemetry Client
        if (queryResults && context.turnState.has(TelemetryLoggerMiddleware.appInsightsServiceKey)) {
            const telemetryClient: BotTelemetryClient = <BotTelemetryClient>
                context.turnState.get(TelemetryLoggerMiddleware.appInsightsServiceKey);
            const telemetryProps: { [key: string]: string } = {};
            const telemetryMetrics: { [key: string]: number } = {};

            telemetryProps[QnATelemetryConstants.knowledgeBaseIdProperty] = this.innerEndpoint.knowledgeBaseId;

            // Make it so we can correlate our reports with Activity or Conversation
            telemetryProps[QnATelemetryConstants.activityIdProperty] = context.activity.id || '';
            if (context.activity.conversation.id) {
                telemetryProps[QnATelemetryConstants.conversationIdProperty] = context.activity.conversation.id;
            }

            // For some customers, logging original text name within Application Insights might be an issue
            if (this.logUserName && context.activity.text) {
                telemetryProps[QnATelemetryConstants.originalQuestionProperty] = context.activity.text;
            }

            // Fill in Qna Results (found or not)
            if (queryResults.length > 0) {
                const queryResult: QnAMakerResult = queryResults[0];
                telemetryProps[QnATelemetryConstants.questionProperty] = JSON.stringify(queryResult.questions);
                telemetryProps[QnATelemetryConstants.answerProperty] = queryResult.answer;
                telemetryProps[QnATelemetryConstants.articleFoundProperty] = 'true';
                telemetryMetrics[QnATelemetryConstants.scoreProperty] = queryResult.score;
            } else {
                telemetryProps[QnATelemetryConstants.questionProperty] = 'No Qna Question matched';
                telemetryProps[QnATelemetryConstants.answerProperty] = 'No Qna Answer matched';
                telemetryProps[QnATelemetryConstants.articleFoundProperty] = 'true';
            }

            TelemetryExtensions.trackEventEx(
                telemetryClient, this.qnaMsgEvent, context.activity, undefined, telemetryProps, telemetryMetrics);
        }

        return queryResults;
    }

}
