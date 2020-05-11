/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    BotFrameworkAdapterSettings,
    BotTelemetryClient,
    ConversationState,
    NullTelemetryClient,
    StatePropertyAccessor,
    TurnContext,
    UserState} from 'botbuilder';
import { ApplicationInsightsTelemetryClient, ApplicationInsightsWebserverMiddleware } from 'botbuilder-applicationinsights';
import {
    CosmosDbStorage,
    CosmosDbStorageSettings } from 'botbuilder-azure';
import {
    Dialog,
    DialogState } from 'botbuilder-dialogs';
import {
    manifestGenerator,
    SkillContext,
    // PENDING: The SkillHttpAdapter should be replaced with SkillWebSocketAdapter
    SkillHttpAdapter, 
    WhitelistAuthenticationProvider} from 'botbuilder-skills';
import {
    ICognitiveModelConfiguration,
    Locales,
    ResponseManager } from 'botbuilder-solutions';
import i18next from 'i18next';
import i18nextNodeFsBackend from 'i18next-node-fs-backend';
import { join } from 'path';
import * as restify from 'restify';
import {
    CustomSkillAdapter,
    DefaultAdapter } from './adapters';
import * as appsettings from './appsettings.json';
import { DialogBot } from './bots/dialogBot';
import * as cognitiveModelsRaw from './cognitivemodels.json';
import { MainDialog } from './dialogs/mainDialog';
import { SampleDialog } from './dialogs/sampleDialog';
import { SkillState } from './models/skillState';
import { MainResponses } from './responses/main/mainResponses';
import { SampleResponses } from './responses/sample/sampleResponses';
import { SharedResponses } from './responses/shared/sharedResponses';
import { BotServices } from './services/botServices';
import { IBotSettings } from './services/botSettings';

// Configure internationalization and default locale
i18next.use(i18nextNodeFsBackend)
    .init({
        lowerCaseLng: true,
        fallbackLng: 'en-us',
        preload: ['de-de', 'en-us', 'es-es', 'fr-fr', 'it-it', 'zh-cn']
    })
    .then(async (): Promise<void> => {
        await Locales.addResourcesFromPath(i18next, 'common');
    });

const cognitiveModels: Map<string, ICognitiveModelConfiguration> = new Map();
const cognitiveModelDictionary: { [key: string]: Object } = cognitiveModelsRaw.cognitiveModels;
const cognitiveModelMap: Map<string, Object>  = new Map(Object.entries(cognitiveModelDictionary));
cognitiveModelMap.forEach((value: Object, key: string): void => {
    cognitiveModels.set(key, value as ICognitiveModelConfiguration);
});

const botSettings: Partial<IBotSettings> = {
    appInsights: appsettings.appInsights,
    blobStorage: appsettings.blobStorage,
    cognitiveModels: cognitiveModels,
    cosmosDb: appsettings.cosmosDb,
    defaultLocale: cognitiveModelsRaw.defaultLocale,
    microsoftAppId: appsettings.microsoftAppId,
    microsoftAppPassword: appsettings.microsoftAppPassword
};
if (botSettings.appInsights === undefined) {
    throw new Error('There is no appInsights value in appsettings file');
}

function getTelemetryClient(settings: Partial<IBotSettings>): BotTelemetryClient {
    if (settings !== undefined && settings.appInsights !== undefined && settings.appInsights.instrumentationKey !== undefined) {
        const instrumentationKey: string = settings.appInsights.instrumentationKey;

        return new ApplicationInsightsTelemetryClient(instrumentationKey);
    }

    return new NullTelemetryClient();
}

const telemetryClient: BotTelemetryClient = getTelemetryClient(botSettings);

if (botSettings.cosmosDb === undefined) {
    throw new Error();
}

const cosmosDbStorageSettings: CosmosDbStorageSettings = {
    authKey: botSettings.cosmosDb.authKey,
    collectionId: botSettings.cosmosDb.collectionId,
    databaseId: botSettings.cosmosDb.databaseId,
    serviceEndpoint: botSettings.cosmosDb.cosmosDBEndpoint
};

const storage: CosmosDbStorage = new CosmosDbStorage(cosmosDbStorageSettings);
const userState: UserState = new UserState(storage);
const conversationState: ConversationState = new ConversationState(storage);
const stateAccessor: StatePropertyAccessor<SkillState> = userState.createProperty(SkillState.name);
const dialogStateAccessor: StatePropertyAccessor<DialogState> = userState.createProperty('DialogState');
const skillContextAccessor: StatePropertyAccessor<SkillContext> = userState.createProperty(SkillContext.name);
const whitelistAuthenticationProvider: WhitelistAuthenticationProvider = new WhitelistAuthenticationProvider(botSettings);

const adapterSettings: Partial<BotFrameworkAdapterSettings> = {
    appId: botSettings.microsoftAppId,
    appPassword: botSettings.microsoftAppPassword
};

const defaultAdapter: DefaultAdapter = new DefaultAdapter(
    botSettings,
    adapterSettings,
    conversationState,
    telemetryClient,
    whitelistAuthenticationProvider);

const customSkillAdapter: CustomSkillAdapter = new CustomSkillAdapter(
    botSettings,
    userState,
    conversationState,
    telemetryClient,
    dialogStateAccessor);
const adapter: SkillHttpAdapter = new SkillHttpAdapter(customSkillAdapter);
// PENDING: these should be uncommented when the WS library is merged
// Also the constructor should receive an IAuthenticationProvider
// const skillWebSocketAdapter: SkillWebSocketAdapter = new SkillWebSocketAdapter(
//     customSkillAdapter,
//     botSettings,
//     telemetryClient);

let bot: DialogBot<Dialog>;
try {

    const responseManager: ResponseManager = new ResponseManager(
        ['en-us', 'de-de', 'es-es', 'fr-fr', 'it-it', 'zh-cn'],
        [SampleResponses, MainResponses, SharedResponses]);
    const botServices: BotServices = new BotServices(botSettings, telemetryClient);
    const sampleDialog: SampleDialog = new SampleDialog(
        botSettings,
        botServices,
        responseManager,
        stateAccessor,
        telemetryClient
    );
    const mainDialog: MainDialog = new MainDialog(
        botSettings,
        botServices,
        responseManager,
        stateAccessor,
        skillContextAccessor,
        sampleDialog,
        telemetryClient
    );

    bot = new DialogBot(conversationState, userState, telemetryClient, mainDialog);
} catch (err) {
    throw err;
}

// Create server
const server: restify.Server = restify.createServer();

// Enable the Application Insights middleware, which helps correlate all activity
// based on the incoming request.
server.use(restify.plugins.bodyParser());
server.use(ApplicationInsightsWebserverMiddleware);

server.listen(process.env.port || process.env.PORT || '3980', (): void => {
    console.log(`${ server.name } listening to ${ server.url }`);
    console.log(`Get the Emulator: https://aka.ms/botframework-emulator`);
    console.log(`To talk to your bot, open your '.bot' file in the Emulator`);
});

// Listen for incoming requests
server.post('/api/messages', async (req: restify.Request, res: restify.Response): Promise<void> => {
    // Route received a request to adapter for processing
    await defaultAdapter.processActivity(req, res, async (turnContext: TurnContext): Promise<void> => {
        // route to bot activity handler.
        await bot.run(turnContext);
    });
});
// PENDING: these should be uncommented when the WS library is merged
// This endpoint will replace the post one
// server.get('/api/skill/messages', async (req: restify.Request, res: restify.Response): Promise<void> => {
//     if (skillWebSocketAdapter !== undefined) {
//         await skillWebSocketAdapter.processActivity(req, res, async (turnContext: TurnContext): Promise<void> => {
//             // route to bot activity handler.
//             await bot.run(turnContext);
//         });
//     } else {
//         res.statusCode = 405;
//     }
// });

// Listen for incoming assistant requests
server.post('/api/skill/messages', async (req: restify.Request, res: restify.Response): Promise<void> => {
    // Route received a request to adapter for processing
    await adapter.processActivity(req, res, async (turnContext: TurnContext): Promise<void> => {
        // route to bot activity handler.
        await bot.run(turnContext);
    });
});

const manifestPath: string = join(__dirname, 'manifestTemplate.json');
server.use(restify.plugins.queryParser());
server.get('/api/skill/manifest', manifestGenerator(manifestPath, botSettings));
// PENDING
server.get('/api/skill/ping', async (req: restify.Request, res: restify.Response): Promise<void> => {
    // await authentication.authenticate(req, res);
});
