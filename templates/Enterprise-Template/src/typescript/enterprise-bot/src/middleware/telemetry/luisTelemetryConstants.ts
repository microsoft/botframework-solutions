// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

/**
 * The Application Insights property names that we're logging.
 */
export class LuisTelemetryConstants {
    public readonly APPLICATION_ID: string = 'applicationId';
    public readonly INTENT_PREFIX: string = 'luisIntent';  // Application Insights Custom Event name (with Intent)
    public readonly INTENT_PROPERTY: string = 'intent';
    public readonly INTENT_SCORE_PROPERTY: string = 'intentScore';
    public readonly CONVERSATION_ID_PROPERTY: string = 'conversationId';
    public readonly QUESTION_PROPERTY: string = 'question';
    public readonly ACTIVITY_ID_PROPERTY: string = 'activityId';
    public readonly SENTIMENT_LABEL_PROPERTY: string = 'sentimentLabel';
    public readonly SENTIMENT_SCORE_PROPERTY: string = 'sentimentScore';
    public readonly DIALOG_ID: string = 'dialogId';
}
