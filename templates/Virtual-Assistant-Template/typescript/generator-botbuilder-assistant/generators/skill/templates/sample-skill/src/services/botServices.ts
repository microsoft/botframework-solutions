/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
*/

import { BotTelemetryClient } from 'botbuilder';
import { LuisApplication, LuisPredictionOptions, LuisRecognizer, QnAMaker, QnAMakerEndpoint } from 'botbuilder-ai';
import {ICognitiveModelConfiguration, ICognitiveModelSet } from 'botbuilder-solutions';
import { DispatchService, LuisService, QnaMakerService } from 'botframework-config';
import { IBotSettings } from './botSettings';

export class BotServices {

    public cognitiveModelSets: Map<string, Partial<ICognitiveModelSet>> = new Map();

    public constructor(settings: Partial<IBotSettings>, telemetryClient: BotTelemetryClient) {
        const luisPredictionOptions: LuisPredictionOptions = {
            telemetryClient: telemetryClient,
            logPersonalInformation: true
        };

        if (settings.cognitiveModels !== undefined) {
            settings.cognitiveModels.forEach((value: ICognitiveModelConfiguration, key: string): void => {

                const language: string = key;
                const config: ICognitiveModelConfiguration = value;
                const cognitiveModelSet: Partial<ICognitiveModelSet> = {
                    luisServices: new Map()
                };

                if (config.dispatchModel !== undefined) {
                    const dispatchModel: DispatchService = new DispatchService(config.dispatchModel);

                    const dispatchApp: LuisApplication = {
                        applicationId: dispatchModel.appId,
                        endpointKey: dispatchModel.subscriptionKey,
                        endpoint: dispatchModel.getEndpoint()
                    };

                    cognitiveModelSet.dispatchService = new LuisRecognizer(dispatchApp, luisPredictionOptions);
                }

                if (config.languageModels !== undefined) {
                    config.languageModels.forEach((model: LuisService): void => {
                        const luisService: LuisService = new LuisService(model);
                        const luisApp: LuisApplication  = {
                            applicationId: luisService.appId,
                            endpointKey: luisService.subscriptionKey,
                            endpoint: luisService.getEndpoint()
                        };
                        if (cognitiveModelSet.luisServices !== undefined) {
                            cognitiveModelSet.luisServices.set(luisService.id, new LuisRecognizer(luisApp, luisPredictionOptions));
                        }
                    });
                }

                if (config.knowledgeBases !== undefined) {
                    config.knowledgeBases.forEach((kb: QnaMakerService): void => {
                        const qnaEndpoint: QnAMakerEndpoint = {
                            knowledgeBaseId: kb.kbId,
                            endpointKey: kb.endpointKey,
                            host: kb.hostname
                        };
                        const qnaMaker: QnAMaker = new QnAMaker(qnaEndpoint, undefined, telemetryClient, true);

                        if (cognitiveModelSet.qnaServices !== undefined) {
                            cognitiveModelSet.qnaServices.set(kb.id, qnaMaker);
                        }
                    });
                }
                this.cognitiveModelSets.set(language, cognitiveModelSet);
            });
        }
    }
}
