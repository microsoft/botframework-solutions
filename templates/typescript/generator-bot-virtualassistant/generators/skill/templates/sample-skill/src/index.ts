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
    TelemetryLoggerMiddleware } from 'botbuilder';
import { ApplicationInsightsTelemetryClient, ApplicationInsightsWebserverMiddleware, TelemetryInitializerMiddleware } from 'botbuilder-applicationinsights';
import { CosmosDbPartitionedStorage } from 'botbuilder-azure';
import {
    Dialog } from 'botbuilder-dialogs';
import {
    CognitiveModelConfiguration,
    CosmosDbPartitionedStorageOptions,
    LocaleTemplateManager } from 'bot-solutions';
import { join } from 'path';
import * as restify from 'restify';
import { DefaultAdapter } from './adapters';
import * as appsettings from './appsettings.json';
import { DefaultActivityHandler } from './bots/defaultActivityHandler';
import * as cognitiveModelsRaw from './cognitivemodels.json';
import { MainDialog } from './dialogs/mainDialog';
import { SampleDialog } from './dialogs/sampleDialog';
import { SampleAction } from './dialogs/sampleAction';
import { SkillState } from './models';
import { BotServices } from './services/botServices';
import { IBotSettings } from './services/botSettings';
import { readFileSync, existsSync } from 'fs';
import { AuthenticationConfiguration, allowedCallersClaimsValidator } from 'botframework-connector';

const cognitiveModels: Map<string, CognitiveModelConfiguration> = new Map();
const cognitiveModelDictionary: { [key: string]: Object } = cognitiveModelsRaw.cognitiveModels;
const cognitiveModelMap: Map<string, Object> = new Map(Object.entries(cognitiveModelDictionary));
cognitiveModelMap.forEach((value: Object, key: string): void => {
    cognitiveModels.set(key, value as CognitiveModelConfiguration);
});

// Load settings
const settings: Partial<IBotSettings> = {
    appInsights: appsettings.appInsights,
    blobStorage: appsettings.blobStorage,
    cognitiveModels: cognitiveModels,
    cosmosDb: appsettings.cosmosDb,
    defaultLocale: cognitiveModelsRaw.defaultLocale,
    microsoftAppId: appsettings.microsoftAppId,
    microsoftAppPassword: appsettings.microsoftAppPassword,
    oauthConnections: appsettings.oauthConnections
};
if (settings.appInsights === undefined) {
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
const telemetryClient: BotTelemetryClient = getTelemetryClient(settings);
const telemetryLoggerMiddleware: TelemetryLoggerMiddleware = new TelemetryLoggerMiddleware(telemetryClient);
const telemetryInitializerMiddleware: TelemetryInitializerMiddleware = new TelemetryInitializerMiddleware(telemetryLoggerMiddleware);

if (settings.cosmosDb === undefined) {
    throw new Error();
}

// Configure storage
const cosmosDbStorageOptions: CosmosDbPartitionedStorageOptions = {
    authKey: settings.cosmosDb.authKey,
    containerId: settings.cosmosDb.containerId,
    databaseId: settings.cosmosDb.databaseId,
    cosmosDbEndpoint: settings.cosmosDb.cosmosDbEndpoint
};
const storage: CosmosDbPartitionedStorage =  new CosmosDbPartitionedStorage(cosmosDbStorageOptions);
const userState: UserState = new UserState(storage);
const conversationState: ConversationState = new ConversationState(storage);
const stateAccessor: StatePropertyAccessor<SkillState> = conversationState.createProperty(SkillState.name);

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

const localeTemplateManager: LocaleTemplateManager = new LocaleTemplateManager(localizedTemplates, settings.defaultLocale || 'en-us');

// Register AuthConfiguration to enable custom claim validation.
const authenticationConfiguration: AuthenticationConfiguration = new AuthenticationConfiguration(
    undefined,
    allowedCallersClaimsValidator(appsettings.allowedCallers)
);

const adapterSettings: Partial<BotFrameworkAdapterSettings> = {
    appId: settings.microsoftAppId,
    appPassword: settings.microsoftAppPassword,
    authConfig: authenticationConfiguration
};

const defaultAdapter: DefaultAdapter = new DefaultAdapter(
    settings,
    localeTemplateManager,
    conversationState,
    telemetryInitializerMiddleware,
    telemetryClient,
    adapterSettings);

let bot: DefaultActivityHandler<Dialog>;
try {
    // Configure bot services
    const botServices: BotServices = new BotServices(settings as IBotSettings, telemetryClient);

    // Register dialogs
    const sampleDialog: SampleDialog = new SampleDialog(
        settings,
        botServices,
        stateAccessor,
        localeTemplateManager
    );
    const sampleAction: SampleAction = new SampleAction(
        settings,
        botServices,
        stateAccessor,
        localeTemplateManager
    );
    const mainDialog: MainDialog = new MainDialog(
        botServices,
        sampleDialog,
        sampleAction,
        localeTemplateManager
    );

    bot = new DefaultActivityHandler(conversationState, userState, localeTemplateManager, telemetryClient, mainDialog);
} catch (err) {
    throw err;
}

// Create server
const server: restify.Server = restify.createServer({ maxParamLength: 1000 });

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

// Enable endpoint to reach any file inside the bot manifest's folder
server.get('/manifest/:file', (req: restify.Request, res: restify.Response): void => {
    const manifestFilename: string = req.params.file;
    const manifestFilePath: string = join(__dirname, 'manifest', manifestFilename);
    if (!existsSync(manifestFilePath)) {
        res.send(404);
        return;
    }

    const manifest: Object = JSON.parse(readFileSync(manifestFilePath, 'UTF8'));
    res.send(200, manifest);
});