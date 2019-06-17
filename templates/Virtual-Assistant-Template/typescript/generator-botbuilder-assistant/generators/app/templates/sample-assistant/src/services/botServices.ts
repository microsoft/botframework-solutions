/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotTelemetryClient } from 'botbuilder';
import { LuisApplication, LuisPredictionOptions, LuisRecognizer, QnAMaker, QnAMakerEndpoint } from 'botbuilder-ai';
import { ICognitiveModelConfiguration, ICognitiveModelSet } from 'botbuilder-solutions';
import { DispatchService, LuisService, QnaMakerService } from 'botframework-config';
import { IBotSettings } from '../services/botSettings';

export class BotServices {

    public cognitiveModelSets: Map<string, ICognitiveModelSet> = new Map();

    constructor(settings: Partial<IBotSettings>, telemetryClient: BotTelemetryClient) {
        const luisPredictionOptions: LuisPredictionOptions = {
            telemetryClient: telemetryClient,
            logPersonalInformation: true
        };

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
                    dispatchService: new LuisRecognizer(dispatchApp, luisPredictionOptions),
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
                        cognitiveModelSet.luisServices.set(luisService.id, new LuisRecognizer(luisApp, luisPredictionOptions));
                    });
                }

                config.knowledgeBases.forEach((kb: QnaMakerService) => {
                    const qnaEndpoint: QnAMakerEndpoint = {
                        knowledgeBaseId: kb.kbId,
                        endpointKey: kb.endpointKey,
                        host: kb.hostname
                    };
                    cognitiveModelSet.qnaServices.set(kb.id, new QnAMaker(qnaEndpoint, undefined, telemetryClient, true));
                });

                this.cognitiveModelSets.set(language, cognitiveModelSet);
            });
        }
    }
}
