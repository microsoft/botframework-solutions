/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { LuisRecognizerTelemetryClient, LuisRecognizer, QnAMaker, QnAMakerEndpoint } from 'botbuilder-ai';

export interface ICognitiveModelSet {
    dispatchService: LuisRecognizerTelemetryClient;
    luisServices: Map<string, LuisRecognizer>; 
    //OBSOLETE: Please update your Virtual Assistant to use the new QnAMakerDialog with Multi Turn and Active Learning support instead. For more information, refer to https://aka.ms/bfvaqnamakerupdate.
    qnaServices: Map<string, QnAMaker>;
    qnaConfiguration: Map<string, QnAMakerEndpoint>;
}
