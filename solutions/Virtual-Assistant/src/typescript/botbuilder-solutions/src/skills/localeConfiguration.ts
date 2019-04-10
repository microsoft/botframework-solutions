/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ITelemetryLuisRecognizer, ITelemetryQnAMaker } from '../middleware';

export class LocaleConfiguration {

    public locale!: string;

    public dispatchRecognizer!: ITelemetryLuisRecognizer;

    public luisServices: Map<string, ITelemetryLuisRecognizer> = new Map();

    public qnaServices: Map<string, ITelemetryQnAMaker> = new Map();
}
