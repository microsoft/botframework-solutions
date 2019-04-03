/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TelemetryClient } from 'applicationinsights';
import { ITelemetryLuisRecognizer, ITelemetryQnAMaker, LocaleConfiguration, SkillConfiguration, SkillConfigurationBase, SkillDefinition,
    SkillEvent, TelemetryLuisRecognizer, TelemetryQnAMaker } from 'bot-solution';
import { LuisApplication, QnAMakerEndpoint } from 'botbuilder-ai';
import { CosmosDbStorageSettings } from 'botbuilder-azure';
import { AppInsightsService, BotConfiguration, CosmosDbService, DispatchService,
    GenericService, IConnectedService, LuisService, QnaMakerService, ServiceTypes } from 'botframework-config';
import { existsSync } from 'fs';
import { join } from 'path';

/**
 * Represents references to external services.
 * For example, LUIS services are kept here as a singleton. This external service is configured
 * using the BotConfiguration class.
 */
export class BotServices {

    public readonly telemetryClient!: TelemetryClient;

    public readonly cosmosDbStorageSettings!: CosmosDbStorageSettings;

    public authenticationConnections: { [key: string]: string } = {};

    public localeConfigurations: Map<string, LocaleConfiguration> = new Map();

    public skillDefinitions: SkillDefinition[] = [];

    public skillConfigurations: Map<string, SkillConfigurationBase> = new Map();

    /**
     * Gets or sets skill events that's loaded from skillEvents.json file.
     * @description The mapping between skill and events defined in skillEvents.json file that specifies what happens
     * when different events are received.
     */
    public skillEvents?: Map<string, SkillEvent> = new Map();

    constructor(
        botConfiguration: BotConfiguration,
        languageModels: Map<string, { botFilePath: string; botFileSecret: string }>,
        skills: SkillDefinition[],
        skillEventsConfig: SkillEvent[]) {
        // Create service clients for each service in the .bot file.
        let telemetryClient: TelemetryClient|undefined;
        let cosmosDbStorageSettings: CosmosDbStorageSettings|undefined;

        botConfiguration.services.forEach((service: IConnectedService) => {
            switch (service.type) {
                case ServiceTypes.AppInsights: {
                    const appInsights: AppInsightsService = <AppInsightsService> service;
                    if (!appInsights) {
                        throw new Error('The Application Insights is not configured correctly in your \'.bot\' file.');
                    }

                    if (!appInsights.instrumentationKey) {
                        throw new Error('The Application Insights Instrumentation Key (\'instrumentationKey\')' +
                        ' is required to run this sample.  Please update your \'.bot\' file.');
                    }

                    telemetryClient = new TelemetryClient(appInsights.instrumentationKey);
                    break;
                }
                case ServiceTypes.CosmosDB: {
                    const cosmos: CosmosDbService = <CosmosDbService> service;

                    cosmosDbStorageSettings = {
                        authKey: cosmos.key,
                        collectionId: cosmos.collection,
                        databaseId: cosmos.database,
                        serviceEndpoint: cosmos.endpoint,
                        databaseCreationRequestOptions: {},
                        documentCollectionRequestOptions: {}
                    };

                    break;
                }
                case ServiceTypes.Generic: {
                    if (service.name === 'Authentication') {
                        const authentication: GenericService = <GenericService> service;

                        this.authenticationConnections = authentication.configuration;
                    }

                    break;
                }
                default:
            }
        });

        if (!telemetryClient) {
            throw new Error('The Application Insights is not configured correctly in your \'.bot\' file.');
        }

        if (!cosmosDbStorageSettings) {
            throw new Error('The CosmosDB endpoint is not configured correctly in your \'.bot\' file.');
        }

        this.telemetryClient = telemetryClient;
        this.cosmosDbStorageSettings = cosmosDbStorageSettings;

        // Create locale configuration object for each language config in appsettings.json
        languageModels.forEach((value: { botFilePath: string; botFileSecret: string }, key: string) => {
            const botAbsolutePath: string = join(__dirname, value.botFilePath);
            if (value.botFilePath && existsSync(botAbsolutePath)) {
                const localeConfig: LocaleConfiguration = this.getLocaleConfig(key, botAbsolutePath, value.botFileSecret);
                this.localeConfigurations.set(key, localeConfig);
            }
        });

        // Create a skill configurations for each skill in appsettings.json
        skills.forEach((skill: SkillDefinition) => {
            const skillConfig: SkillConfiguration = this.getSkillConfig(skill);

            this.skillDefinitions.push(skill);
            this.skillConfigurations.set(skill.id, skillConfig);
            if (skillEventsConfig) {
                this.skillEvents = skillEventsConfig.reduce(
                    (previous: Map<string, SkillEvent>, current: SkillEvent) => {
                        previous.set(current.event, current);

                        return previous;
                    },
                    new Map<string, SkillEvent>());
            } else {
                this.skillEvents = undefined;
            }
        });
    }

    private getLocaleConfig(language: string, botFilePath: string, botFileSecret: string): LocaleConfiguration {
        const localeConfig: LocaleConfiguration = new LocaleConfiguration();
        localeConfig.locale = language;

        const config: BotConfiguration = BotConfiguration.loadSync(botFilePath, botFileSecret);

        config.services.forEach((service: IConnectedService) => {
            switch (service.type) {
                case ServiceTypes.Dispatch: {
                    const dispatch: DispatchService = <DispatchService> service;

                    if (!dispatch) {
                        throw new Error('The Dispatch service is not configured correctly in your \'.bot\' file.');
                    }

                    if (!dispatch.appId) {
                        throw new Error('The Dispatch Luis Model Application Id (\'appId\') is required to run this sample. ' +
                            'Please update your \'.bot\' file.');
                    }

                    if (!dispatch.subscriptionKey) {
                        throw new Error('The Subscription Key (\'subscriptionKey\') is required to run this sample. ' +
                            'Please update your \'.bot\' file.');
                    }

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

                    if (!luis) {
                        throw new Error('The Luis service is not configured correctly in your \'.bot\' file.');
                    }

                    if (!luis.appId) {
                        throw new Error('The Luis Model Application Id (\'appId\') is required to run this sample. ' +
                            'Please update your \'.bot\' file.');
                    }

                    if (!luis.authoringKey) {
                        throw new Error('The Luis Authoring Key (\'authoringKey\') is required to run this sample. ' +
                            'Please update your \'.bot\' file.');
                    }

                    if (!luis.subscriptionKey) {
                        throw new Error('The Subscription Key (\'subscriptionKey\') is required to run this sample. ' +
                            'Please update your \'.bot\' file.');
                    }

                    if (!luis.region) {
                        throw new Error('The Region (\'region\') is required to run this sample. ' +
                            'Please update your \'.bot\' file.');
                    }

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

        return localeConfig;
    }

    private getSkillConfig(skill: SkillDefinition): SkillConfiguration {
        const skillConfig: SkillConfiguration = new SkillConfiguration();
        skillConfig.cosmosDbOptions = this.cosmosDbStorageSettings;

        this.localeConfigurations.forEach((localeConfig: LocaleConfiguration, key: string) => {
            const lConfig: LocaleConfiguration = new LocaleConfiguration();
            const filteredEntries: [string, ITelemetryLuisRecognizer][] = Array.from(localeConfig.luisServices.entries())
                .filter((l: [string, ITelemetryLuisRecognizer]) => skill.luisServiceIds.includes(l[0]));
            lConfig.luisServices = new Map(filteredEntries);
            skillConfig.localeConfigurations.set(key, lConfig);
        });

        if (skill.supportedProviders) {
            skill.supportedProviders.forEach((provider: string) => {
                const matches: [string, string][] = Object.entries(this.authenticationConnections)
                    .filter((x: [string, string]) => x[1] === provider);

                matches.forEach((value: [string, string]) => {
                    skillConfig.authenticationConnections[value[0]] = value[1];
                });
            });
        }

        skill.configuration.forEach((value: string, key: string) => {
            skillConfig.properties[key] = value;
        });

        return skillConfig;
    }
}
