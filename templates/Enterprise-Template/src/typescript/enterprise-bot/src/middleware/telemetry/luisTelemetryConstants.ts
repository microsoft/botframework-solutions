// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

/**
 * The Application Insights property names that we're logging.
 */
export class LuisTelemetryConstants {
    public static readonly ApplicationId: string = "applicationId";
    public static readonly IntentPrefix: string = "luisIntent";  // Application Insights Custom Event name (with Intent)
    public static readonly IntentProperty: string = "intent";
    public static readonly IntentScoreProperty: string = "intentScore";
    public static readonly ConversationIdProperty: string = "conversationId";
    public static readonly QuestionProperty: string = "question";
    public static readonly ActivityIdProperty: string = "activityId";
    public static readonly SentimentLabelProperty: string = "sentimentLabel";
    public static readonly SentimentScoreProperty: string = "sentimentScore";
    public static readonly DialogId: string = "dialogId";
}
