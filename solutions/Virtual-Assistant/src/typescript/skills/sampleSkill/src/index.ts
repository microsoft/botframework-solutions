/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TelemetryClient } from 'applicationinsights';
import * as botSkill from 'bot-skill';
import {
    ActivityExtensions,
    EventDebuggerMiddleware,
    Locales,
    ProactiveState,
    ProactiveStateMiddleware,
    ResponseManager,
    SetLocaleMiddleware,
    SkillConfiguration,
    SkillDefinition,
    TelemetryExtensions
} from 'bot-solution';
import {
    AutoSaveStateMiddleware,
    BotFrameworkAdapter,
    ConversationState,
    ShowTypingMiddleware,
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
import { MainResponses } from './dialogs/main/mainResponses';
import { SampleResponses } from './dialogs/sample/sampleResponses';
import { SharedResponses } from './dialogs/shared/sharedResponses';
import { default as languageModelsRaw } from './languageModels.json';
import { SampleSkill } from './sampleSkill';
import { ServiceManager } from './serviceClients/serviceManager';
import { default as skillsRaw } from './skills.json';

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
const languageModels: Map<string, { botFilePath: string; botFileSecret: string }> = new Map(
    Object.entries(languageModelsRaw)
    .map((f: [string, { botFilePath: string; botFileSecret: string }]) => {
        const fullPath: string = path.join(__dirname, f[1].botFilePath);
        f[1].botFilePath = fullPath;

        return f;
    })
    );

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

// Create the skill adapter
const skillAdapter: SkillAdapter = new SkillAdapter();

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
const conversationState: ConversationState = new ConversationState(storage, 'sampleSkill');
const userState: UserState = new UserState(storage, 'sampleSkill');
const proactiveState: ProactiveState = new ProactiveState(storage);

// Use the AutoSaveStateMiddleware middleware to automatically read and write conversation and user state.
adapter.use(new AutoSaveStateMiddleware(conversationState, userState));
skillAdapter.use(new AutoSaveStateMiddleware(conversationState, userState));

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
skillAdapter.use(new TranscriptLoggerMiddleware(transcriptStore));

/* Typing Middleware
(automatically shows typing when the bot is responding/working)*/
adapter.use(new ShowTypingMiddleware());
adapter.use(new SetLocaleMiddleware(DEFAULT_LOCALE));
adapter.use(new EventDebuggerMiddleware());
adapter.use(new ProactiveStateMiddleware(proactiveState));

skillAdapter.use(new SetLocaleMiddleware(DEFAULT_LOCALE));
skillAdapter.use(new EventDebuggerMiddleware());

adapter.onTurnError = async (context: TurnContext, error: Error): Promise<void> => {
    // tslint:disable-next-line:no-console
    console.error(`${error.message}/n${error.stack}`);
    await context.sendActivity(ActivityExtensions.createReply(context.activity, SharedResponses.errorMessage));
    await context.sendActivity({
        type: ActivityTypes.Trace,
        text: `Skill Error: ${error.message} | ${error.stack}`
    });

    TelemetryExtensions.trackExceptionEx(telemetryClient, error, context.activity);
};

skillAdapter.onTurnError = async (context: TurnContext, error: Error): Promise<void> => {
    // tslint:disable-next-line:no-console
    console.error(`${error.message}/n${error.stack}`);
    await context.sendActivity(ActivityExtensions.createReply(context.activity, SharedResponses.errorMessage));
    await context.sendActivity({
        type: ActivityTypes.Trace,
        text: `Skill Error: ${error.message} | ${error.stack}`
    });

    TelemetryExtensions.trackExceptionEx(telemetryClient, error, context.activity);
};

const configuration: SkillConfiguration = new SkillConfiguration(
    botConfig,
    languageModels,
    skills[0].supportedProviders,
    skills[0].parameters,
    skills[0].configuration);

const responseManager: ResponseManager = new ResponseManager(
    Array.from(configuration.localeConfigurations.keys()),
    [MainResponses, SharedResponses, SampleResponses]);

let bot: SampleSkill;
try {
    bot = new SampleSkill(
        configuration,
        conversationState,
        userState,
        telemetryClient,
        true,
        responseManager,
        new ServiceManager());
} catch (err) {
    throw err;
}

// Create server
const server: restify.Server = restify.createServer();
server.listen(process.env.port || process.env.PORT || 3980, (): void => {
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

// Listen for incoming requests as a skill
server.post('/api/skill/messages', async (req: restify.Request, res: restify.Response, next: restify.Next) => {
    // Route received a request to adapter for processing
    await skillAdapter.processActivity(req, res, async (turnContext: TurnContext) => {
        // route to bot activity handler.
        await bot.onTurn(turnContext);
    });
    await next();
});
