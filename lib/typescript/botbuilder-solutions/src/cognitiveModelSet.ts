/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ITelemetryLuisRecognizer, ITelemetryQnAMaker } from './telemetry';

export interface ICognitiveModelSet {
    dispatchService: ITelemetryLuisRecognizer;
    luisServices: Map<string, ITelemetryLuisRecognizer>;
    qnaServices: Map<string, ITelemetryQnAMaker>;
}
