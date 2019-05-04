/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

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
     * Gets or sets the OAuth Connections for the bot.
     */
    oauthConnections: IOAuthConnection[];

    /**
     * Gets or sets the CosmosDB Configuration for the bot.
     */
    cosmosDb: ICosmosDbConfiguration;

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
    appId: string;
    instrumentationKey: string;
}

export interface IBlobStorageConfiguration {
    connectionString: string;
    container: string;
}

export interface IContentModeratorConfiguration {
    key: string;
}

export interface ICosmosDbConfiguration {
    authkey: string;
    collectionId: string;
    cosmosDBEndpoint: string;
    databaseId: string;
}

export interface ICognitiveModelConfiguration {
    dispatchModel: DispatchService;
    languageModels: LuisService[];
    knowledgeBases: QnaMakerService[];
}
