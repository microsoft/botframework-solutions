/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    BotFrameworkAdapter,
    BotFrameworkAdapterSettings,
    BotTelemetryClient,
    ConversationState,
    NullTelemetryClient,
    StatePropertyAccessor,
    TurnContext,
    UserState, 
    TelemetryLoggerMiddleware } from 'botbuilder';
import { ApplicationInsightsTelemetryClient, ApplicationInsightsWebserverMiddleware, TelemetryInitializerMiddleware } from 'botbuilder-applicationinsights';
import { CosmosDbPartitionedStorageOptions, CosmosDbPartitionedStorage } from 'botbuilder-azure';
import {
    Dialog } from 'botbuilder-dialogs';
import {
    ICognitiveModelConfiguration,
    Locales,
    LocaleTemplateManager } from 'bot-solutions';
import i18next from 'i18next';
import i18nextNodeFsBackend from 'i18next-node-fs-backend';
import { join } from 'path';
import * as restify from 'restify';
import { DefaultAdapter } from './adapters';
import * as appsettings from './appsettings.json';
import { DefaultActivityHandler } from './bots/defaultActivityHandler';
import * as cognitiveModelsRaw from './cognitivemodels.json';
import { MainDialog } from './dialogs/mainDialog';
import { SampleDialog } from './dialogs/sampleDialog';
import { SampleAction } from './dialogs/sampleAction';
import { SkillState } from './models/skillState';
import { BotServices } from './services/botServices';
import { IBotSettings } from './services/botSettings';
import { TestUpdateActivityDialog } from './dialogs/testUpdateActivityDialog';

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
const cognitiveModelMap: Map<string, Object> = new Map(Object.entries(cognitiveModelDictionary));
cognitiveModelMap.forEach((value: Object, key: string): void => {
    cognitiveModels.set(key, value as ICognitiveModelConfiguration);
});

// Load settings
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

// Configure telemetry
const telemetryClient: BotTelemetryClient = getTelemetryClient(botSettings);
const telemetryLoggerMiddleware: TelemetryLoggerMiddleware = new TelemetryLoggerMiddleware(telemetryClient);
const telemetryInitializerMiddleware: TelemetryInitializerMiddleware = new TelemetryInitializerMiddleware(telemetryLoggerMiddleware);

if (botSettings.cosmosDb === undefined) {
    throw new Error();
}

// Configure storage
const cosmosDbStorageOptions: CosmosDbPartitionedStorageOptions = {
    authKey: botSettings.cosmosDb.authKey,
    containerId: botSettings.cosmosDb.containerId,
    databaseId: botSettings.cosmosDb.databaseId,
    cosmosDbEndpoint: botSettings.cosmosDb.cosmosDbEndpoint
};
const storage: CosmosDbPartitionedStorage =  new CosmosDbPartitionedStorage(cosmosDbStorageOptions);
const userState: UserState = new UserState(storage);
const conversationState: ConversationState = new ConversationState(storage);
const stateAccessor: StatePropertyAccessor<SkillState> = userState.createProperty(SkillState.name);

// Configure localized responses
const localizedTemplates: Map<string, string> = new Map<string, string>();
const templateFile = 'AllResponses';
const supportedLocales: string[] = ['en-us', 'de-de', 'es-es', 'fr-fr', 'it-it', 'zh-cn'];

supportedLocales.forEach((locale: string) => {
    // LG template for en-us does not include locale in file extension.
    const localTemplateFile = locale === 'en-us'
        ? join(__dirname, 'responses', `${ templateFile }.lg`)
        : join(__dirname, 'responses', `${ templateFile }.${ locale }.lg`);
    localizedTemplates.set(locale, localTemplateFile);
});

const localeTemplateManager: LocaleTemplateManager = new LocaleTemplateManager(localizedTemplates, botSettings.defaultLocale || 'en-us');

const adapterSettings: Partial<BotFrameworkAdapterSettings> = {
    appId: botSettings.microsoftAppId,
    appPassword: botSettings.microsoftAppPassword
};

const defaultAdapter: DefaultAdapter = new DefaultAdapter(
    botSettings,
    adapterSettings,
    localeTemplateManager,
    telemetryInitializerMiddleware,
    telemetryClient);

const adapter: BotFrameworkAdapter = defaultAdapter;

let bot: DefaultActivityHandler<Dialog>;
try {
    // Configure bot services
    const botServices: BotServices = new BotServices(botSettings, telemetryClient);

    // Register dialogs
    const sampleDialog: SampleDialog = new SampleDialog(
        botSettings,
        botServices,
        stateAccessor,
        telemetryClient,
        localeTemplateManager
    );
    const sampleAction: SampleAction = new SampleAction(
        botSettings,
        botServices,
        stateAccessor,
        telemetryClient,
        localeTemplateManager
    );
    // Register dialogs
    const updateDialog: TestUpdateActivityDialog = new TestUpdateActivityDialog(
        botSettings,
        botServices,
        localeTemplateManager,
        stateAccessor,
        telemetryClient
    );
    const mainDialog: MainDialog = new MainDialog(
        botServices,
        telemetryClient,
        stateAccessor,
        sampleDialog,
        sampleAction,
        updateDialog,
        localeTemplateManager
    );

    bot = new DefaultActivityHandler(conversationState, userState, localeTemplateManager, mainDialog);
} catch (err) {
    throw err;
}

// Create server
const server: restify.Server = restify.createServer({ maxParamLength: 1000000 });

// Enable the Application Insights middleware, which helps correlate all activity
// based on the incoming request.
server.use(restify.plugins.bodyParser());
server.use(restify.plugins.queryParser());
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

server.get('/api/messages', async (req: restify.Request, res: restify.Response): Promise<void> => {
    // Route received a request to adapter for processing
    await defaultAdapter.processActivity(req, res, async (turnContext: TurnContext): Promise<void> => {
        // route to bot activity handler.
        await bot.run(turnContext);
    });
});
