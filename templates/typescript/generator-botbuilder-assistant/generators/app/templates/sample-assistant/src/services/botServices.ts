/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotTelemetryClient } from 'botbuilder';
import { LuisApplication, LuisPredictionOptions, LuisRecognizer, QnAMaker, QnAMakerEndpoint } from 'botbuilder-ai';
import { ICognitiveModelConfiguration, ICognitiveModelSet } from 'botbuilder-solutions';
import { DispatchService, LuisService, QnaMakerService } from 'botframework-config';
import i18next from 'i18next';
import { IBotSettings } from '../services/botSettings';

export class BotServices {

    public cognitiveModelSets: Map<string, ICognitiveModelSet> = new Map();

    public constructor(settings: Partial<IBotSettings>, telemetryClient: BotTelemetryClient) {
        const luisPredictionOptions: LuisPredictionOptions = {
            telemetryClient: telemetryClient,
            logPersonalInformation: true
        };

        if (settings.cognitiveModels !== undefined) {
            settings.cognitiveModels.forEach((value: ICognitiveModelConfiguration, key: string): void => {

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
                    config.languageModels.forEach((model: LuisService): void => {
                        const luisService: LuisService = new LuisService(model);
                        const luisApp: LuisApplication  = {
                            applicationId: luisService.appId,
                            endpointKey: luisService.subscriptionKey,
                            endpoint: luisService.getEndpoint()
                        };
                        cognitiveModelSet.luisServices.set(luisService.id, new LuisRecognizer(luisApp, luisPredictionOptions));
                    });
                }

                config.knowledgeBases.forEach((kb: QnaMakerService): void => {
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

    public getCognitiveModel(): ICognitiveModelSet {
        // get current activity locale
        const locale: string = i18next.language;
        let cognitiveModels: ICognitiveModelSet | undefined = this.cognitiveModelSets.get(locale);

        if (cognitiveModels === undefined) {
            const keyFound: string | undefined = Array.from(this.cognitiveModelSets.keys())
            // eslint-disable-next-line @typescript-eslint/explicit-function-return-type
                .find((key: string) => {
                    if (key.substring(0, 2) === locale.substring(0, 2)) {
                        return key;
                    }
                });
            if (keyFound !== undefined) {
                cognitiveModels = this.cognitiveModelSets.get(keyFound);
            }
        }
        if (cognitiveModels === undefined) {
            throw new Error('There is no value in cognitiveModels');
        }

        return cognitiveModels;
    }
}
