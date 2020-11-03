/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotTelemetryClient } from 'botbuilder';
import {
    LuisApplication,
    LuisRecognizer,
    LuisRecognizerOptionsV3,
    QnAMakerEndpoint } from 'botbuilder-ai';
import { CognitiveModelConfiguration, ICognitiveModelSet } from 'bot-solutions';
import { LuisService, QnaMakerService } from 'botframework-config';
import { IBotSettings } from '../services/botSettings';

export class BotServices {
    public cognitiveModelSets: Map<string, ICognitiveModelSet> = new Map();

    public constructor(settings: IBotSettings, client: BotTelemetryClient) {
        settings.cognitiveModels.forEach((value: CognitiveModelConfiguration, key: string): void => {
            const language: string = key;
            const config: CognitiveModelConfiguration = value;

            const telemetryClient: BotTelemetryClient = client;

            const luisOptions: LuisRecognizerOptionsV3 = {
                telemetryClient: telemetryClient,
                logPersonalInformation: true,
                apiVersion: 'v3'
            };

            const set: Partial<ICognitiveModelSet> = {
                luisServices: new Map(),
                qnaConfiguration: new Map(),
                qnaServices: new Map()
            };

            if (config.dispatchModel !== undefined) {
                const dispatchModel: LuisService = new LuisService(config.dispatchModel);
                const dispatchApp: LuisApplication = {
                    applicationId: config.dispatchModel.appId,
                    endpointKey: config.dispatchModel.subscriptionKey,
                    endpoint: dispatchModel.getEndpoint()
                };
                set.dispatchService= new LuisRecognizer(dispatchApp, luisOptions);
            }

            if (config.languageModels !== undefined) {
                config.languageModels.forEach((model: LuisService): void => {
                    const luisModel: LuisService = new LuisService(model);
                    const luisApp: LuisApplication  = {
                        applicationId: luisModel.appId,
                        endpointKey: luisModel.subscriptionKey,
                        endpoint: luisModel.getEndpoint()
                    };
                    if (set.luisServices !== undefined) {
                        set.luisServices.set(model.id, new LuisRecognizer(luisApp, luisOptions));
                    }
                });
            }

            config.knowledgeBases.forEach((kb: QnaMakerService): void => {
                const qnaEndpoint: QnAMakerEndpoint = {
                    knowledgeBaseId: kb.kbId,
                    endpointKey: kb.endpointKey,
                    host: kb.hostname
                };
                if (set.qnaConfiguration !== undefined) {
                    set.qnaConfiguration.set(kb.id, qnaEndpoint);
                }
            });

            this.cognitiveModelSets.set(language, set as ICognitiveModelSet);
        });
    }    

    public getCognitiveModels(locale: string): ICognitiveModelSet {
        // Get cognitive models for locale
        let cognitiveModels: ICognitiveModelSet | undefined = this.cognitiveModelSets.get(locale);

        if (cognitiveModels === undefined) {
            const keyFound: string | undefined = Array.from(this.cognitiveModelSets.keys())
                .find((key: string) => { key.substring(0, 2) === locale.substring(0, 2); });
            if (keyFound !== undefined) {
                cognitiveModels = this.cognitiveModelSets.get(keyFound);
            }
        }
        if (cognitiveModels === undefined) {
            throw new Error(`There's no matching locale for '${ locale }' or its root language '${ locale.substring(0, 2) }'.
            Please review your available locales in your cognitivemodels.json file.`);
        }

        return cognitiveModels;
    }
}
