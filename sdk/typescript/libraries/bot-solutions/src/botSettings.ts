/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { CosmosDbPartitionedStorageOptions } from 'botbuilder-azure';
import { DispatchService, LuisService, QnaMakerService } from 'botframework-config';
import { IOAuthConnection } from './authentication';

/**
 * Base interface representing the configuration for a bot.
 */
export interface IBotSettingsBase {
    /**
     * Gets or sets the Microsoft Application Id.
     */
    microsoftAppId: string;

    /**
     * Gets or sets the Microsoft Application Password.
     */
    microsoftAppPassword: string;

    /**
     * Gets or sets the default locale of the bot.
     */
    defaultLocale: string;

    /**
     * Gets or sets the voiceFont for the bot.
     */
    voiceFont: string;

    /**
     * Gets or sets the OAuth Connections for the bot.
     */
    oauthConnections: IOAuthConnection[];

    /**
     * Gets or sets the CosmosDB Configuration for the bot.
     */
    cosmosDb: CosmosDbPartitionedStorageOptions;

    /**
     * Gets or sets the Application Insights configuration for the bot.
     */
    appInsights: ITelemetryConfiguration;

    /**
     * Gets or sets the Azure Blob Storage configuration for the bot.
     */
    blobStorage: IBlobStorageConfiguration;

    /**
     * Gets or sets the Azure Content Moderator configuration for the bot.
     */
    contentModerator: IContentModeratorConfiguration;

    /**
     * Gets or sets the dictionary of cognitive model configurations by locale for the bot.
     */
    cognitiveModels: Map<string, ICognitiveModelConfiguration>;

    /**
     * Gets or sets the Properties dictionary.
     */
    properties: Map<string, string>;
}

export interface ITelemetryConfiguration {
    instrumentationKey: string;
}

export interface IBlobStorageConfiguration {
    /**
     * Gets or sets the connection string for the Azure Blob Storage service.
     */
    connectionString: string;
    /**
     * Gets or sets the blob container for the Azure Blob Storage service.
     */
    container: string;
}

export interface IContentModeratorConfiguration {
    /**
     * Gets or sets the subscription key for the Content Moderator service.
     */
    key: string;
}

export interface ICosmosDbConfiguration {
    authKey: string;
    collectionId: string;
    cosmosDBEndpoint: string;
    databaseId: string;
}

export interface ICognitiveModelConfiguration {
    /**
     * Gets or sets the Dispatch service for the set of cognitive models.
     */
    dispatchModel: DispatchService;
    /**
     * Gets or sets the collection of LUIS models.
     */
    languageModels: LuisService[];
    /**
     * Gets or sets the collection of QnA Maker knowledge bases.
     */
    knowledgeBases: QnaMakerService[];
}
