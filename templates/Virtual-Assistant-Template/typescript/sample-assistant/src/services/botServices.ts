/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    LuisApplication,
    QnAMakerEndpoint } from 'botbuilder-ai';
import {
    ICognitiveModelConfiguration,
    ICognitiveModelSet,
    TelemetryLuisRecognizer,
    TelemetryQnAMaker } from 'botbuilder-solutions';
import {
    DispatchService,
    LuisService,
    QnaMakerService } from 'botframework-config';
import { IBotSettings } from '../services/botSettings';

export class BotServices {

    public cognitiveModelSets: Map<string, ICognitiveModelSet> = new Map();

    constructor(settings: Partial<IBotSettings>) {
        if (settings.cognitiveModels !== undefined) {
            settings.cognitiveModels.forEach((value: ICognitiveModelConfiguration, key: string) => {

                const language: string = key;
                const config: ICognitiveModelConfiguration = value;

                const dispatchModel: DispatchService = new DispatchService(config.dispatchModel);
                const dispatchApp: LuisApplication = {
                    applicationId: dispatchModel.appId,
                    endpointKey: dispatchModel.subscriptionKey,
                    endpoint: dispatchModel.getEndpoint()
                };

                const cognitiveModelSet: ICognitiveModelSet = {
                    dispatchService: new TelemetryLuisRecognizer(dispatchApp),
                    luisServices: new Map(),
                    qnaServices: new Map()
                };

                if (config.languageModels !== undefined) {
                    config.languageModels.forEach((model: LuisService) => {
                        const luisService: LuisService = new LuisService(model);
                        const luisApp: LuisApplication  = {
                            applicationId: luisService.appId,
                            endpointKey: luisService.subscriptionKey,
                            endpoint: luisService.getEndpoint()
                        };
                        cognitiveModelSet.luisServices.set(luisService.id, new TelemetryLuisRecognizer(luisApp));
                    });
                }

                config.knowledgeBases.forEach((kb: QnaMakerService) => {
                    const qnaEndpoint: QnAMakerEndpoint = {
                        knowledgeBaseId: kb.kbId,
                        endpointKey: kb.endpointKey,
                        host: kb.hostname
                    };
                    const qnaMaker: TelemetryQnAMaker = new TelemetryQnAMaker(qnaEndpoint);
                    cognitiveModelSet.qnaServices.set(kb.id, qnaMaker);
                });

                this.cognitiveModelSets.set(language, cognitiveModelSet);
            });
        }
    }
}
