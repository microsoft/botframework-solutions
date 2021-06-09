/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { CosmosClientOptions } from '@azure/cosmos';
import { LuisService, QnaMakerService } from 'botframework-config';
import { IOAuthConnection } from './authentication';

/**
 * Base interface representing the configuration for a bot.
 */
export interface BotSettingsBase {
    /**
     * Gets or sets the Microsoft Application Id.
     * The Microsoft Application Id.
     */
    microsoftAppId: string;

    /**
     * Gets or sets the Microsoft Application Password.
     * The Microsoft Application Password.
     */
    microsoftAppPassword: string;

    /**
     * Gets or sets the default locale of the bot.
     * The default locale of the bot.
     */
    defaultLocale: string;

    /**
     * Gets or sets the voiceFont for the bot.
     * The default voice font of the bot.
     */
    voiceFont: string;

    /**
     * Gets or sets the OAuth Connections for the bot.
     * The OAuth Connections for the bot.
     */
    oauthConnections: IOAuthConnection[];

    /**
     * Gets or sets the OAuthCredentials for OAuth.
     * The OAuthCredentials for OAuth for the bot.
     */
    oauthCredentials: OAuthCredentialsConfiguration;

    /**
     * Gets or sets the CosmosDB Configuration for the bot.
     * The CosmosDB Configuration for the bot.
     */
    cosmosDb: CosmosDbPartitionedStorageOptions;

    /**
     * Gets or sets the Application Insights configuration for the bot.
     * The Application Insights configuration for the bot.
     */
    appInsights: TelemetryConfiguration;

    /**
     * Gets or sets the Azure Blob Storage configuration for the bot.
     * The Azure Blob Storage configuration for the bot.
     */
    blobStorage: BlobStorageConfiguration;

    /**
     * Gets or sets the Azure Content Moderator configuration for the bot.
     * The Azure Content Moderator configuration for the bot.
     */
    contentModerator: ContentModeratorConfiguration;

    /**
     * Gets or sets the dictionary of cognitive model configurations by locale for the bot.
     * The dictionary of cognitive model configurations by locale for the bot.
     */
    cognitiveModels: Map<string, CognitiveModelConfiguration>;

    /**
     * Gets or sets the Properties dictionary.
     * The Properties dictionary.
     */
    properties: Map<string, string>;
}

export interface TelemetryConfiguration {
    instrumentationKey: string;
}

/**
 * Class representing configuration for an Azure Blob Storage service.
 */
export interface BlobStorageConfiguration {
    /**
     * Gets or sets the connection string for the Azure Blob Storage service.
     * The connection string for the Azure Blob Storage service.
     */
    connectionString: string;
    /**
     * Gets or sets the blob container for the Azure Blob Storage service.
     * The blob container for the Azure Blob Storage service.
     */
    container: string;
}

/**
 * Class representing configuration for an Azure Content Moderator service.
 */
export interface ContentModeratorConfiguration {
    /**
     * Gets or sets the subscription key for the Content Moderator service.
     * The subscription key for the Content Moderator service.
     */
    key: string;
}

/**
 * Class representing configuration for a collection of Azure Cognitive Models.
 */
export interface CognitiveModelConfiguration {
    /**
     * The Dispatch service for the set of cognitive models.
     */
    dispatchModel: LuisService;
    /**
     * Gets or sets the collection of LUIS models.
     * The collection of LUIS models.
     */
    languageModels: LuisService[];
    /**
     * Gets or sets the collection of QnA Maker knowledge bases.
     * The collection of QnA Maker knowledgebases.
     */
    knowledgeBases: QnaMakerService[];
}

export interface OAuthCredentialsConfiguration {
    /**
     * Gets or sets the Microsoft App Id for OAuth.
     * The microsoft app id for OAuth.
     */
    microsoftAppId: string;

    /**
     * Gets or sets the Microsoft App Password for OAuth.
     * The microsoft app password for OAuth.
     */
    microsoftAppPassword: string;
}

/**
 * Cosmos DB Partitioned Storage Options.
 */
export interface CosmosDbPartitionedStorageOptions {
    /**
     * The CosmosDB endpoint.
     */
    cosmosDbEndpoint?: string;
    /**
     * The authentication key for Cosmos DB.
     */
    authKey?: string;
    /**
     * The database identifier for Cosmos DB instance.
     */
    databaseId: string;
    /**
     * The container identifier.
     */
    containerId: string;
    /**
     * The options for the CosmosClient.
     */
    cosmosClientOptions?: CosmosClientOptions;
    /**
     * The throughput set when creating the Container. Defaults to 400.
     */
    containerThroughput?: number;
    /**
     * The suffix to be added to every key. See cosmosDbKeyEscape.escapeKey
     *
     * Note: compatibilityMode must be set to 'false' to use a KeySuffix.
     * When KeySuffix is used, keys will NOT be truncated but an exception will
     * be thrown if the key length is longer than allowed by CosmosDb.
     *
     * The keySuffix must contain only valid CosmosDb key characters.
     * (e.g. not: '\\', '?', '/', '#', '*')
     */
    keySuffix?: string;
    /**
     * Early version of CosmosDb had a max key length of 255.  Keys longer than
     * this were truncated in cosmosDbKeyEscape.escapeKey.  This remains the default
     * behavior of cosmosDbPartitionedStorage, but can be overridden by setting
     * compatibilityMode to false.
     *
     * compatibilityMode cannot be true if keySuffix is used.
     */
    compatibilityMode?: boolean;
}