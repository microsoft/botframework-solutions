// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

/**
 * The Application Insights property names that we're logging.
 */
export class LuisTelemetryConstants {
    public readonly applicationId: string = 'applicationId';
    public readonly intentPrefix: string = 'luisIntent';  // Application Insights Custom Event name (with Intent)
    public readonly intentProperty: string = 'intent';
    public readonly intentScoreProperty: string = 'intentScore';
    public readonly conversationIdProperty: string = 'conversationId';
    public readonly questionProperty: string = 'question';
    public readonly activityIdProperty: string = 'activityId';
    public readonly sentimentLabelProperty: string = 'sentimentLabel';
    public readonly sentimentScoreProperty: string = 'sentimentScore';
    public readonly dialogId: string = 'dialogId';
}
