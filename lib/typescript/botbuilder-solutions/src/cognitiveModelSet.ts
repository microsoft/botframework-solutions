/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { LuisRecognizerTelemetryClient, QnAMakerTelemetryClient } from 'botbuilder-ai';

export interface ICognitiveModelSet {
    dispatchService: LuisRecognizerTelemetryClient;
    luisServices: Map<string, LuisRecognizerTelemetryClient>;
    qnaServices: Map<string, QnAMakerTelemetryClient>;
}
