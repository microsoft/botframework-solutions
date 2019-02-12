// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Setup our global error handler
// For production bots use AppInsights, or a production-grade telemetry service to
// log errors and other bot telemetry.

import { TelemetryClient } from 'applicationinsights';
import {
    AutoSaveStateMiddleware,
    BotFrameworkAdapter,
    ConversationState,
    TranscriptLoggerMiddleware,
    TurnContext,
    UserState } from 'botbuilder';
import {
    AzureBlobTranscriptStore,
    CosmosDbStorage,
    CosmosDbStorageSettings } from 'botbuilder-azure';
import {
    BlobStorageService,
    BotConfiguration,
    IAppInsightsService,
    IBlobStorageService,
    ICosmosDBService,
    IEndpointService,
    IGenericService } from 'botframework-config';

// Read variables from .env file.
import { config } from 'dotenv';
import * as i18n from 'i18n';
import * as path from 'path';
import * as restify from 'restify';
import { BotServices } from './botServices';
// Content Moderation Middleware (analyzes incoming messages for inappropriate content including PII, profanity, etc.)
import { ContentModeratorMiddleware } from './middleware/contentModeratorMiddleware';

import { EnterpriseBot } from './enterpriseBot';

i18n.configure({
    directory: path.join(__dirname, 'locales'),
    defaultLocale: 'en',
    objectNotation: true
});

const ENV_NAME: string = process.env.NODE_ENV || 'development';
config({ path: path.join(__dirname, '..', `.env.${ENV_NAME}`) });

const BOT_CONFIGURATION: string = (process.env.ENDPOINT || 'development');
const BOT_CONFIGURATION_ERROR: number = 1;

const CONFIGURATION_PATH: string = path.join(__dirname, '..', process.env.BOT_FILE_NAME || '.bot');
const BOT_SECRET: string = process.env.BOT_FILE_SECRET || '';

try {
    require.resolve(CONFIGURATION_PATH);
} catch (err) {
    process.exit(BOT_CONFIGURATION_ERROR);
    throw new Error(`Error reading bot file. Please ensure you have valid botFilePath and botFileSecret set for your environment.`);
}

// Get bot configuration for services
const BOT_CONFIG: BotConfiguration = BotConfiguration.loadSync(CONFIGURATION_PATH, BOT_SECRET);

// Get bot endpoint configuration by service name
const ENDPOINT_CONFIG: IEndpointService = <IEndpointService> BOT_CONFIG.findServiceByNameOrId(BOT_CONFIGURATION);

// Create the adapter
const ADAPTER: BotFrameworkAdapter = new BotFrameworkAdapter({
    appId: ENDPOINT_CONFIG.appId || process.env.microsoftAppID,
    appPassword: ENDPOINT_CONFIG.appPassword || process.env.microsoftAppPassword
});

import { TelemetryLoggerMiddleware } from './middleware/telemetry/telemetryLoggerMiddleware';
// Get AppInsights configuration by service name
const APPINSIGHTS_CONFIGURATION: string = process.env.APPINSIGHTS_NAME || '';
const APPINSIGHTS_CONFIG: IAppInsightsService = <IAppInsightsService> BOT_CONFIG.findServiceByNameOrId(APPINSIGHTS_CONFIGURATION);
if (!APPINSIGHTS_CONFIG) {
    process.exit(BOT_CONFIGURATION_ERROR);
    throw new Error('Please configure your AppInsights connection in your .bot file.');
}
const TELEMETRY_CLIENT: TelemetryClient = new TelemetryClient(APPINSIGHTS_CONFIG.instrumentationKey);

ADAPTER.onTurnError = async (turnContext: TurnContext, error: Error): Promise<void> => {
const appInsightsLogger: TelemetryLoggerMiddleware = new TelemetryLoggerMiddleware(TELEMETRY_CLIENT, true,  true);
ADAPTER.use(appInsightsLogger);

// CAUTION:  The sample simply logs the error to the console.
// tslint:disable-next-line:no-console
console.error(error);
// For production bots, use AppInsights or similar telemetry system.
// tell the user something happen
TELEMETRY_CLIENT.trackException({ exception: error });
// for multi-turn dialog interactions,
// make sure we clear the conversation state
await turnContext.sendActivity('Sorry, it looks like something went wrong.');
};

// CAUTION: The Memory Storage used here is for local bot debugging only. When the bot
// is restarted, anything stored in memory will be gone.
// const storage = new MemoryStorage();

 // this is the name of the cosmos DB configuration in your .bot file
const STORAGE_CONFIGURATION: string = process.env.STORAGE_NAME || '';
const COSMOS_CONFIG: ICosmosDBService = <ICosmosDBService> BOT_CONFIG.findServiceByNameOrId(STORAGE_CONFIGURATION);
// For production bots use the Azure CosmosDB storage, Azure Blob, or Azure Table storage provides.
const COSMOS_DB_STORAGE_SETTINGS: CosmosDbStorageSettings = {
    authKey: COSMOS_CONFIG.key,
    collectionId: COSMOS_CONFIG.collection,
    databaseId: COSMOS_CONFIG.database,
    serviceEndpoint: COSMOS_CONFIG.endpoint,
    documentCollectionRequestOptions: {},
    databaseCreationRequestOptions: {}
};
const STORAGE: CosmosDbStorage  = new CosmosDbStorage(COSMOS_DB_STORAGE_SETTINGS);

if (!COSMOS_CONFIG) {
    // tslint:disable-next-line:no-console
    console.log('Please configure your CosmosDB connection in your .bot file.');
    process.exit(BOT_CONFIGURATION_ERROR);
}

// create conversation and user state with in-memory storage provider.
const CONVERSATION_STATE: ConversationState = new ConversationState(STORAGE);
const USER_STATE: UserState = new UserState(STORAGE);

// Use the AutoSaveStateMiddleware middleware to automatically read and write conversation and user state.
// CONSIDER:  if only using userState, then switch to adapter.use(userState);
ADAPTER.use(new AutoSaveStateMiddleware(CONVERSATION_STATE, USER_STATE));

// Transcript Middleware (saves conversation history in a standard format)
const BLOB_CONFIGURATION: string = process.env.BLOB_NAME || ''; // this is the name of the BlobStorage configuration in your .bot file
const BLOB_STORAGE_CONFIG: IBlobStorageService = <IBlobStorageService> BOT_CONFIG.findServiceByNameOrId(BLOB_CONFIGURATION);
if (!BLOB_STORAGE_CONFIG) {
    // tslint:disable-next-line:no-console
    console.log('Please configure your Blob storage connection in your .bot file.');
    process.exit(BOT_CONFIGURATION_ERROR);
}
const BLOB_STORAGE: BlobStorageService = new BlobStorageService(BLOB_STORAGE_CONFIG);
const TRANSCRIPT_STORE: AzureBlobTranscriptStore = new AzureBlobTranscriptStore({
    containerName: BLOB_STORAGE.container,
    storageAccountOrConnectionString: BLOB_STORAGE.connectionString
});
ADAPTER.use(new TranscriptLoggerMiddleware(TRANSCRIPT_STORE));

/* Typing Middleware
(automatically shows typing when the bot is responding/working)
(not implemented https://github.com/Microsoft/botbuilder-js/issues/470)
adapter.use(new ShowTypingMiddleware());*/

// this is the name of the Content Moderator configuration in your .bot file
const CM_CONFIGURATION: string = process.env.CONTENT_MODERATOR_NAME || '';
const CM_CONFIG: IGenericService = <IGenericService> BOT_CONFIG.findServiceByNameOrId(CM_CONFIGURATION);
if (CM_CONFIG && CM_CONFIG.configuration.key && CM_CONFIG.configuration.region) {
    const CONTENT_MODERATOR: ContentModeratorMiddleware = new ContentModeratorMiddleware(
        CM_CONFIG.configuration.key,
        CM_CONFIG.configuration.region
    );
    ADAPTER.use(CONTENT_MODERATOR);
} else {
    console.warn('Content Moderator not configured in .bot file.');
}

let bot: EnterpriseBot;
try {
    const SERVICES: BotServices = new BotServices(BOT_CONFIG);
    bot = new EnterpriseBot(SERVICES, CONVERSATION_STATE, USER_STATE);
} catch (err) {
    process.exit(BOT_CONFIGURATION_ERROR);
    throw new Error(err);
}

// Create server
const SERVER: restify.Server = restify.createServer();
SERVER.listen(process.env.port || process.env.PORT || 3978, (): void => {
    // tslint:disable-next-line:no-console
    console.log(`${SERVER.name} listening to ${SERVER.url}`);
    // tslint:disable-next-line:no-console
    console.log(`Get the Emulator: https://aka.ms/botframework-emulator`);
    // tslint:disable-next-line:no-console
    console.log(`To talk to your bot, open your '.bot' file in the Emulator`);
});

// Listen for incoming requests
SERVER.post('/api/messages', (req: restify.Request, res: restify.Response) => {
    // Route received a request to adapter for processing
    ADAPTER.processActivity(req, res, async (turnContext: TurnContext) => {

        // set location using activity's locate information
        i18n.setLocale(turnContext.activity.locale || i18n.getLocale());

        // route to bot activity handler.
        await bot.onTurn(turnContext);
    });
});
