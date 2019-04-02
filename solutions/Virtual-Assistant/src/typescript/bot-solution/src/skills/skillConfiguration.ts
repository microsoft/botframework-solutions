import { LuisApplication, QnAMakerEndpoint } from 'botbuilder-ai';
import { CosmosDbStorageSettings } from 'botbuilder-azure';
import { BotConfiguration, DispatchService, GenericService, IConnectedService,
    LuisService, QnaMakerService, ServiceTypes } from 'botframework-config';

import { ITelemetryLuisRecognizer, ITelemetryQnAMaker, TelemetryLuisRecognizer, TelemetryQnAMaker } from '../middleware/telemetry';
import { LocaleConfiguration } from './localeConfiguration';
import { SkillConfigurationBase } from './skillConfigurationBase';

export class SkillConfiguration extends SkillConfigurationBase {
    public authenticationConnections: { [key: string]: string } = {};
    public cosmosDbOptions!: CosmosDbStorageSettings;
    public localeConfigurations: Map<string, LocaleConfiguration> = new Map();
    public properties: { [key: string]: Object|undefined } = {};

    constructor(botConfiguration?: BotConfiguration,
                languagesModels?: Map<string, { botFilePath: string; botFileSecret: string }>,
                supportedProviders?: string[],
                parameters?: string[],
                configuration?: Map<string, Object>) {
        super();

        if (!botConfiguration || !languagesModels) {
            return;
        }

        if (supportedProviders && supportedProviders.length > 0) {
            this.isAuthenticatedSkill = true;
        }

        botConfiguration.services.forEach((service: IConnectedService) => {
            if (service.type === ServiceTypes.Generic && service.name === 'Authentication' && supportedProviders) {
                const auth: GenericService = <GenericService> service;

                supportedProviders.forEach((provider: string) => {
                    const matches: [string, string][] = Object.entries(auth.configuration)
                        .filter((val: [string, string]) => val[1] === provider);

                    matches.forEach((val: [string, string]) => {
                        this.authenticationConnections[val[0]] = val[1];
                    });
                });
            }
        });

        languagesModels.forEach((value: { botFilePath: string; botFileSecret: string }, key: string) => {
            const localeConfig: LocaleConfiguration = new LocaleConfiguration();
            localeConfig.locale = key;

            const config: BotConfiguration = BotConfiguration.loadSync(value.botFilePath, value.botFileSecret);

            config.services.forEach((service: IConnectedService) => {
                switch (service.type) {
                    case ServiceTypes.Dispatch: {
                        const dispatch: DispatchService = <DispatchService> service;

                        const dispatchApp: LuisApplication = {
                            applicationId: dispatch.appId,
                            endpointKey: dispatch.subscriptionKey,
                            endpoint: dispatch.getEndpoint()
                        };

                        localeConfig.dispatchRecognizer = new TelemetryLuisRecognizer(dispatchApp);
                        break;
                    }
                    case ServiceTypes.Luis: {
                        const luis: LuisService = <LuisService> service;

                        const luisApp: LuisApplication = {
                            applicationId: luis.appId,
                            endpointKey: luis.subscriptionKey,
                            endpoint: luis.getEndpoint()
                        };

                        const luisRecognizer: ITelemetryLuisRecognizer = new TelemetryLuisRecognizer(luisApp);
                        localeConfig.luisServices.set(luis.id, luisRecognizer);

                        break;
                    }
                    case ServiceTypes.QnA: {
                        const qna: QnaMakerService = <QnaMakerService> service;

                        const qnaEndpoint: QnAMakerEndpoint = {
                            knowledgeBaseId: qna.kbId,
                            endpointKey: qna.endpointKey,
                            host: qna.hostname
                        };

                        const qnaMaker: ITelemetryQnAMaker = new TelemetryQnAMaker(qnaEndpoint);
                        localeConfig.qnaServices.set(qna.id, qnaMaker);

                        break;
                    }
                    default:
                }
            });

            this.localeConfigurations.set(key, localeConfig);
        });

        if (parameters) {
            // Add the parameters the skill needs
            // Initialize each parameter to null. Needs to be set later by the bot.
            parameters.forEach((val: string) => this.properties[val] = {});
        }

        if (configuration) {
            // add the additional keys the skill needs
            configuration.forEach((val: Object, key: string) => this.properties[key] = val);
        }
    }
}
