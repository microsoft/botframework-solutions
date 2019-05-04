/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotTelemetryClient, TurnContext } from 'botbuilder';
import { QnAMaker, QnAMakerEndpoint, QnAMakerOptions, QnAMakerResult } from 'botbuilder-ai';

import { TelemetryExtensions } from './telemetryExtensions';
import { TelemetryLoggerMiddleware } from './telemetryLoggerMiddleware';

export interface ITelemetryQnAMaker {
    logPersonalInformation: boolean;
    getAnswers(context: TurnContext): Promise<QnAMakerResult[]>;
}

export namespace QnATelemetryConstants {
    export const knowledgeBaseIdProperty: string = 'knowledgeBaseId';
    export const answerProperty: string = 'answer';
    export const articleFoundProperty: string = 'articleFound';
    export const channelIdProperty: string = 'channelId';
    export const originalQuestionProperty: string = 'originalQuestion';
    export const questionProperty: string = 'question';
    export const questionIdProperty: string = 'questionId';
    export const scoreProperty: string = 'score';
    export const usernameProperty: string = 'username';
}

export class TelemetryQnAMaker extends QnAMaker implements ITelemetryQnAMaker {
    public readonly qnaMsgEvent: string = 'QnaMessage';
    public readonly logPersonalInformation: boolean;
    private innerEndpoint: QnAMakerEndpoint;

    constructor(endpoint: QnAMakerEndpoint, options?: QnAMakerOptions, logPersonalInformation: boolean = false) {
        super(endpoint, options);
        this.logPersonalInformation = logPersonalInformation;
        this.innerEndpoint = endpoint;
    }

    public async getAnswers(context: TurnContext): Promise<QnAMakerResult[]> {
        // Call Qna Maker
        const queryResults: QnAMakerResult[] = await super.getAnswers(context);

        // Find the Application Insights Telemetry Client
        if (queryResults && context.turnState.has(TelemetryLoggerMiddleware.appInsightsServiceKey)) {
            const telemetryClient: BotTelemetryClient = <BotTelemetryClient>
                context.turnState.get(TelemetryLoggerMiddleware.appInsightsServiceKey);
            const telemetryProps: Map<string, string> = new Map();
            const telemetryMetrics: { [key: string]: number } = {};

            telemetryProps.set(QnATelemetryConstants.knowledgeBaseIdProperty, this.innerEndpoint.knowledgeBaseId);

            const text: string = context.activity.text;
            const userName: string = context.activity.from.name;

            // Use the LogPersonalInformation flag to toggle logging PII data, text and user name are common examples
            if (this.logPersonalInformation) {
                if (text) {
                    telemetryProps.set(QnATelemetryConstants.originalQuestionProperty, text);
                }

                if (userName) {
                    telemetryProps.set(QnATelemetryConstants.usernameProperty, userName);
                }
            }

            // Fill in Qna Results (found or not)
            if (queryResults.length > 0) {
                const queryResult: QnAMakerResult = queryResults[0];
                telemetryProps.set(QnATelemetryConstants.questionProperty, JSON.stringify(queryResult.questions));
                telemetryProps.set(QnATelemetryConstants.questionIdProperty, queryResult.id ? queryResult.id.toString() : '');
                telemetryProps.set(QnATelemetryConstants.answerProperty, queryResult.answer);
                telemetryProps.set(QnATelemetryConstants.articleFoundProperty, 'true');
                telemetryMetrics[QnATelemetryConstants.scoreProperty] = queryResult.score;
            } else {
                telemetryProps.set(QnATelemetryConstants.questionProperty, 'No QnA Question matched');
                telemetryProps.set(QnATelemetryConstants.questionIdProperty, 'No QnA Question Id matched');
                telemetryProps.set(QnATelemetryConstants.answerProperty, 'No QnA Answer matched');
                telemetryProps.set(QnATelemetryConstants.articleFoundProperty, 'false');
            }

            TelemetryExtensions.trackEventEx(
                telemetryClient, this.qnaMsgEvent, context.activity, undefined, telemetryProps, telemetryMetrics);
        }

        return queryResults;
    }
}
