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
    UserState, 
    TelemetryLoggerMiddleware} from 'botbuilder';
import { ApplicationInsightsTelemetryClient, ApplicationInsightsWebserverMiddleware, TelemetryInitializerMiddleware } from 'botbuilder-applicationinsights';
import {
    CosmosDbStorage,
    CosmosDbStorageSettings } from 'botbuilder-azure';
import {
    Dialog } from 'botbuilder-dialogs';
import {
    ICognitiveModelConfiguration,
    Locales,
    LocaleTemplateEngineManager,
    manifestGenerator,
    SkillHttpAdapter } from 'botbuilder-solutions';
import i18next from 'i18next';
import i18nextNodeFsBackend from 'i18next-node-fs-backend';
import { join } from 'path';
import * as restify from 'restify';
import { CustomSkillAdapter, DefaultAdapter } from './adapters';
import * as appsettings from './appsettings.json';
import { DefaultActivityHandler } from './bots/defaultActivityHandler';
import * as cognitiveModelsRaw from './cognitivemodels.json';
import { MainDialog } from './dialogs/mainDialog';
import { SampleDialog } from './dialogs/sampleDialog';
import { SkillState } from './models/skillState';
import { BotServices } from './services/botServices';
import { IBotSettings } from './services/botSettings';

// Configure internationalization and default locale
i18next.use(i18nextNodeFsBackend)
    .init({
        fallbackLng: 'en-us',
        preload: ['de-de', 'en-us', 'es-es', 'fr-fr', 'it-it', 'zh-cn']
    })
    .then(async (): Promise<void> => {
        await Locales.addResourcesFromPath(i18next, 'common');
    });

const cognitiveModels: Map<string, ICognitiveModelConfiguration> = new Map();
const cognitiveModelDictionary: { [key: string]: Record<string, any> } = cognitiveModelsRaw.cognitiveModels;
const cognitiveModelMap: Map<string, Record<string, any>>  = new Map(Object.entries(cognitiveModelDictionary));
cognitiveModelMap.forEach((value: Record<string, any>, key: string): void => {
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

// Configure localized responses
const localizedTemplates: Map<string, string[]> = new Map<string, string[]>();
const templateFiles: string[] = ['MainResponses', 'SampleResponses'];
const supportedLocales: string[] = ['en-us', 'de-de', 'es-es', 'fr-fr', 'it-it', 'zh-cn'];

supportedLocales.forEach((locale: string): void => {
    const localeTemplateFiles: string[] = [];
    templateFiles.forEach((template: string): void => {
        // LG template for default locale should not include locale in file extension.
        if (locale === (botSettings.defaultLocale || 'en-us')) {
            localeTemplateFiles.push(join(__dirname, '..', 'src', 'responses', `${ template }.lg`));
        } else {
            localeTemplateFiles.push(join(__dirname, '..', 'src',  'responses', `${ template }.${ locale }.lg`));
        }
    });

    localizedTemplates.set(locale, localeTemplateFiles);    
});

const localeTemplateEngine: LocaleTemplateEngineManager = new LocaleTemplateEngineManager(localizedTemplates, botSettings.defaultLocale || 'en-us');

const telemetryLoggerMiddleware: TelemetryLoggerMiddleware = new TelemetryLoggerMiddleware(telemetryClient);
const telemetryInitializerMiddleware: TelemetryInitializerMiddleware = new TelemetryInitializerMiddleware(telemetryLoggerMiddleware);

const adapterSettings: Partial<BotFrameworkAdapterSettings> = {
    appId: botSettings.microsoftAppId,
    appPassword: botSettings.microsoftAppPassword
};

const defaultAdapter: DefaultAdapter = new DefaultAdapter(
    botSettings,
    localeTemplateEngine,
    telemetryInitializerMiddleware,
    telemetryClient,
    adapterSettings);

const customSkillAdapter: CustomSkillAdapter = new CustomSkillAdapter(
    botSettings,
    userState,
    conversationState,
    localeTemplateEngine,
    telemetryInitializerMiddleware,
    telemetryClient);
const adapter: SkillHttpAdapter = new SkillHttpAdapter(customSkillAdapter);

let bot: DefaultActivityHandler<Dialog>;
try {
    const botServices: BotServices = new BotServices(botSettings, telemetryClient);
    const sampleDialog: SampleDialog = new SampleDialog(
        botSettings,
        botServices,
        stateAccessor,
        telemetryClient,
        localeTemplateEngine
    );
    const mainDialog: MainDialog = new MainDialog(
        botServices,
        stateAccessor,
        sampleDialog,
        telemetryClient,
        localeTemplateEngine
    );

    bot = new DefaultActivityHandler(conversationState, userState, mainDialog);
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
