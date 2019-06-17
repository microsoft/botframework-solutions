/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotTelemetryClient } from 'botbuilder';
import { LuisApplication, LuisPredictionOptions, LuisRecognizer } from 'botbuilder-ai';
import {
    ICognitiveModelConfiguration,
    ICognitiveModelSet } from 'botbuilder-solutions';
import { LuisService } from 'botframework-config';
import { IBotSettings } from './botSettings';

export class BotServices {

    public cognitiveModelSets: Map<string, Partial<ICognitiveModelSet>> = new Map();

    constructor(settings: Partial<IBotSettings>, telemetryClient: BotTelemetryClient) {
        const luisPredictionOptions: LuisPredictionOptions = {
            telemetryClient: telemetryClient,
            logPersonalInformation: true
        };

        try {
            if (settings.cognitiveModels !== undefined) {

                settings.cognitiveModels.forEach((value: ICognitiveModelConfiguration, key: string) => {
                    const language: string = key;
                    const config: ICognitiveModelConfiguration = value;
                    const cognitiveModelSet: Partial<ICognitiveModelSet> = {
                        luisServices: new Map()
                    };

                    config.languageModels.forEach((model: LuisService) => {
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

                    this.cognitiveModelSets.set(language, cognitiveModelSet);
                });
            }
        } catch (err) {
            throw new Error(err);
        }
    }
}
