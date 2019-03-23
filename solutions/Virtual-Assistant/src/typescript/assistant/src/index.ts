// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { TelemetryClient } from 'applicationinsights';
import { EventDebuggerMiddleware, Locales, SetLocaleMiddleware, SkillDefinition, TelemetryExtensions } from 'bot-solution';
import {
    AutoSaveStateMiddleware,
    BotFrameworkAdapter,
    ConversationState,
    TranscriptLoggerMiddleware,
    TurnContext,
    UserState
} from 'botbuilder';
import {
    AzureBlobTranscriptStore,
    CosmosDbStorage,
    CosmosDbStorageSettings
} from 'botbuilder-azure';
import {
    BlobStorageService,
    BotConfiguration,
    IAppInsightsService,
    IBlobStorageService,
    IBotConfiguration,
    IConnectedService,
    ICosmosDBService,
    IEndpointService,
    ServiceTypes
} from 'botframework-config';
import { ActivityTypes } from 'botframework-schema';
import { config } from 'dotenv';
import i18next from 'i18next';
import i18nextNodeFsBackend from 'i18next-node-fs-backend';
import * as path from 'path';
import * as restify from 'restify';
import { BotServices } from './botServices';
import { MainResponses } from './dialogs/main/mainResponses';
import { default as languageModelsRaw } from './languageModels.json';
import { default as skillsRaw } from './skills.json';
import { VirtualAssistant } from './virtualAssistant';

// Read variables from .env file.
const ENV_NAME: string = process.env.NODE_ENV || 'development';
config({ path: path.join(__dirname, '..', `.env.${ENV_NAME}`) });

const BOT_CONFIGURATION: string = (process.env.ENDPOINT || 'development');
const BOT_CONFIGURATION_ERROR: number = 1;

const CONFIGURATION_PATH: string = path.join(__dirname, '..', process.env.BOT_FILE_NAME || '.bot');
const BOT_SECRET: string = process.env.BOT_FILE_SECRET || '';

const DEFAULT_LOCALE: string = process.env.DEFAULT_LOCALE || 'en';
const APPINSIGHTS_NAME: string = process.env.APPINSIGHTS_NAME || '';
const STORAGE_CONFIGURATION: string = process.env.STORAGE_NAME || '';
const BLOB_NAME: string = process.env.BLOB_NAME || '';

// Configure internationalization and default locale
i18next.use(i18nextNodeFsBackend)
.init({
    fallbackLng: 'en',
    preload: [ 'de', 'en', 'es', 'fr', 'it', 'zh' ],
    backend: {
        loadPath: path.join(__dirname, 'locales', '{{lng}}.json')
    }
})
.then(async () => {
    await Locales.addResourcesFromPath(i18next, 'common');
});

function searchService(botConfiguration: IBotConfiguration, serviceType?: ServiceTypes, nameOrId?: string): IConnectedService|undefined {
    const candidates: IConnectedService[] = botConfiguration.services
        .filter((s: IConnectedService) =>  !serviceType || s.type === serviceType);
    const service: IConnectedService|undefined = candidates.find((s: IConnectedService) => s.id === nameOrId || s.name === nameOrId)
        || candidates.find((s: IConnectedService) => true);

    if (!service && nameOrId) {
        throw new Error(`Service '${nameOrId}' [type: ${serviceType}] not found in .bot file.`);
    }

    return service;
}

// Initializes your bot language models and skills definitions
const languageModels: Map<string, { botFilePath: string; botFileSecret: string }> = new Map(Object.entries(languageModelsRaw));

const skills: SkillDefinition[] = skillsRaw.map((skill: { [key: string]: Object|undefined }) => {
    const result: SkillDefinition = Object.assign(new SkillDefinition(), skill);
    result.configuration = new Map<string, string>(Object.entries(skill.configuration || {}));

    return result;
});

try {
    require.resolve(CONFIGURATION_PATH);
} catch (err) {
    // tslint:disable-next-line:no-console
    console.error('Error reading bot file. Please ensure you have valid botFilePath and botFileSecret set for your environment.');
    process.exit(BOT_CONFIGURATION_ERROR);
}

// Get bot configuration for services
const botConfig: BotConfiguration = BotConfiguration.loadSync(CONFIGURATION_PATH, BOT_SECRET);

// Get bot endpoint configuration by service name
const endpointService: IEndpointService = <IEndpointService> searchService(botConfig, ServiceTypes.Endpoint, BOT_CONFIGURATION);

// Create the adapter
const adapter: BotFrameworkAdapter = new BotFrameworkAdapter({
    appId: endpointService.appId || process.env.microsoftAppID,
    appPassword: endpointService.appPassword || process.env.microsoftAppPassword
});

// Get AppInsights configuration by service name
const appInsightsConfig: IAppInsightsService = <IAppInsightsService> searchService(botConfig, ServiceTypes.AppInsights, APPINSIGHTS_NAME);
if (!appInsightsConfig) {
    // tslint:disable-next-line:no-console
    console.error('Please configure your AppInsights connection in your .bot file.');
    process.exit(BOT_CONFIGURATION_ERROR);
}
const telemetryClient: TelemetryClient = new TelemetryClient(appInsightsConfig.instrumentationKey);

// For production bots use the Azure CosmosDB storage, Azure Blob, or Azure Table storage provides.
const cosmosConfig: ICosmosDBService = <ICosmosDBService> searchService(botConfig, ServiceTypes.CosmosDB, STORAGE_CONFIGURATION);
const cosmosDbStorageSettings: CosmosDbStorageSettings = {
    authKey: cosmosConfig.key,
    collectionId: cosmosConfig.collection,
    databaseId: cosmosConfig.database,
    serviceEndpoint: cosmosConfig.endpoint,
    documentCollectionRequestOptions: {},
    databaseCreationRequestOptions: {}
};
const storage: CosmosDbStorage  = new CosmosDbStorage(cosmosDbStorageSettings);

if (!cosmosConfig) {
    // tslint:disable-next-line:no-console
    console.error('Please configure your CosmosDB connection in your .bot file.');
    process.exit(BOT_CONFIGURATION_ERROR);
}

// create conversation and user state
const conversationState: ConversationState = new ConversationState(storage);
const userState: UserState = new UserState(storage);

// Use the AutoSaveStateMiddleware middleware to automatically read and write conversation and user state.
adapter.use(new AutoSaveStateMiddleware(conversationState, userState));

// Transcript Middleware (saves conversation history in a standard format)
const blobStorageConfig: IBlobStorageService = <IBlobStorageService> searchService(botConfig, ServiceTypes.BlobStorage, BLOB_NAME);
if (!blobStorageConfig) {
    // tslint:disable-next-line:no-console
    console.error('Please configure your Blob storage connection in your .bot file.');
    process.exit(BOT_CONFIGURATION_ERROR);
}
const blobStorage: BlobStorageService = new BlobStorageService(blobStorageConfig);
const transcriptStore: AzureBlobTranscriptStore = new AzureBlobTranscriptStore({
    containerName: blobStorage.container,
    storageAccountOrConnectionString: blobStorage.connectionString
});
adapter.use(new TranscriptLoggerMiddleware(transcriptStore));

/* Typing Middleware
(automatically shows typing when the bot is responding/working)
(not implemented https://github.com/Microsoft/botbuilder-js/issues/470)
adapter.use(new ShowTypingMiddleware());*/
adapter.use(new SetLocaleMiddleware(DEFAULT_LOCALE));
adapter.use(new EventDebuggerMiddleware());

adapter.onTurnError = async (context: TurnContext, error: Error): Promise<void> => {
    // tslint:disable-next-line:no-console
    console.error(`${error.message}/n${error.stack}`);
    const responseBuilder: MainResponses = new MainResponses();
    await responseBuilder.replyWith(context, MainResponses.responseIds.error);
    await context.sendActivity({
        type: ActivityTypes.Trace,
        text: `Virtual Assistant Error: ${error.message} | ${error.stack}`
    });

    TelemetryExtensions.trackExceptionEx(telemetryClient, error, context.activity);
};

let bot: VirtualAssistant;
try {
    const botServices: BotServices = new BotServices(botConfig, languageModels, skills);
    bot = new VirtualAssistant(botServices, conversationState, userState, endpointService, telemetryClient);
} catch (err) {
    throw err;
}

// Create server
const server: restify.Server = restify.createServer();
server.listen(process.env.port || process.env.PORT || 3979, (): void => {
    // tslint:disable-next-line:no-console
    console.log(`${server.name} listening to ${server.url}`);
    // tslint:disable-next-line:no-console
    console.log(`Get the Emulator: https://aka.ms/botframework-emulator`);
    // tslint:disable-next-line:no-console
    console.log(`To talk to your bot, open your '.bot' file in the Emulator`);
});

// Listen for incoming requests
server.post('/api/messages', (req: restify.Request, res: restify.Response) => {
    // Route received a request to adapter for processing
    adapter.processActivity(req, res, async (turnContext: TurnContext) => {
        // route to bot activity handler.
        await bot.onTurn(turnContext);
    });
});
