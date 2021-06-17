/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    BotFrameworkAdapterSettings,
    BotTelemetryClient,
    ChannelServiceRoutes,
    ConversationState,
    NullTelemetryClient,
    StatePropertyAccessor,
    TurnContext,
    UserState,
    TelemetryLoggerMiddleware,
    SkillHttpClient,
    SkillConversationIdFactory,
    BotFrameworkSkill } from 'botbuilder';
import { ApplicationInsightsTelemetryClient, ApplicationInsightsWebserverMiddleware } from 'botbuilder-applicationinsights';
import { CosmosDbPartitionedStorage } from 'botbuilder-azure';
import { Dialog, SkillDialog, SkillDialogOptions } from 'botbuilder-dialogs';
import {
    CognitiveModelConfiguration,
    CosmosDbPartitionedStorageOptions,
    LocaleTemplateManager,
    SwitchSkillDialog,
    IEnhancedBotFrameworkSkill,
    SkillsConfiguration } from 'bot-solutions';
import { SimpleCredentialProvider, AuthenticationConfiguration, allowedCallersClaimsValidator } from 'botframework-connector';
import { join } from 'path';
import * as restify from 'restify';
import { DefaultAdapter } from './adapters/defaultAdapter';
import * as appsettings from './appsettings.json';
import { DefaultActivityHandler } from './bots/defaultActivityHandler';
import * as cognitiveModelsRaw from './cognitivemodels.json';
import { MainDialog } from './dialogs/mainDialog';
import { OnboardingDialog } from './dialogs/onboardingDialog';
import { BotServices } from './services/botServices';
import { IBotSettings } from './services/botSettings';
import { Activity } from 'botframework-schema';
import { TelemetryInitializerMiddleware } from 'botbuilder-applicationinsights';
import { IUserProfileState } from './models/userProfileState';
import { ITokenExchangeConfig, TokenExchangeSkillHandler } from './tokenExchange';

function getTelemetryClient(settings: Partial<IBotSettings>): BotTelemetryClient {
    if (settings !== undefined && settings.appInsights !== undefined && settings.appInsights.instrumentationKey !== undefined) {
        const instrumentationKey: string = settings.appInsights.instrumentationKey;

        return new ApplicationInsightsTelemetryClient(instrumentationKey);
    }

    return new NullTelemetryClient();
}

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
    tokenExchangeConfig: appsettings.tokenExchangeConfig
};

// Configure configuration provider
const credentialProvider: SimpleCredentialProvider = new SimpleCredentialProvider(appsettings.microsoftAppId, appsettings.microsoftAppPassword);

// Register the skills configuration class.
const skillsConfig: SkillsConfiguration = new SkillsConfiguration(appsettings.botFrameworkSkills as IEnhancedBotFrameworkSkill[], appsettings.skillHostEndpoint);

let authenticationConfiguration = new AuthenticationConfiguration();

// Register AuthConfiguration to enable custom claim validation.
if (skillsConfig.skills.size > 0) {
    const allowedCallers: string[] = [...skillsConfig.skills.values()].map(skill => skill.appId);
    authenticationConfiguration = new AuthenticationConfiguration(
        undefined,
        allowedCallersClaimsValidator(allowedCallers)
    );
}

// Configure telemetry
const telemetryClient: BotTelemetryClient = getTelemetryClient(settings);
const telemetryLoggerMiddleware: TelemetryLoggerMiddleware = new TelemetryLoggerMiddleware(telemetryClient);
const telemetryInitializerMiddleware: TelemetryInitializerMiddleware = new TelemetryInitializerMiddleware(telemetryLoggerMiddleware);

// Configure bot services
const botServices: BotServices = new BotServices(settings as IBotSettings, telemetryClient);

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

// Register the skills conversation ID factory, the client.
const skillConversationIdFactory: SkillConversationIdFactory = new SkillConversationIdFactory(storage);
const skillClient: SkillHttpClient = new SkillHttpClient(credentialProvider, skillConversationIdFactory);

// Register the Bot Framework Adapter with error handling enabled.
// Note: some classes use the base BotAdapter so we add an extra registration that pulls the same instance.
const adapterSettings: Partial<BotFrameworkAdapterSettings> = {
    appId: settings.microsoftAppId,
    appPassword: settings.microsoftAppPassword
};
const adapter: DefaultAdapter = new DefaultAdapter(
    settings,
    localeTemplateManager,
    conversationState,
    adapterSettings,
    telemetryInitializerMiddleware,
    telemetryClient,
    skillsConfig,
    skillClient
);

// Configure bot
let bot: DefaultActivityHandler<Dialog>;
try {
    // Register the SkillDialogs (remote skills).
    const botId: string = appsettings.microsoftAppId;
    if (botId === undefined || botId.trim().length === 0) {
        throw new Error('microsoftAppId is not in the configuration');
    }
    
    const skillDialogs: SkillDialog[] = [];
    skillsConfig.skills.forEach((skill: IEnhancedBotFrameworkSkill, skillId: string) => {
        const skillDialogOptions: SkillDialogOptions = {
            botId: botId,
            conversationIdFactory: skillConversationIdFactory,
            skillClient: skillClient,
            skillHostEndpoint: skillsConfig.skillHostEndpoint,
            skill: skill,
            conversationState: conversationState
        };

        skillDialogs.push(new SkillDialog(skillDialogOptions, skillId));
    });

    // Register dialogs
    const previousResponseAccesor: StatePropertyAccessor<Partial<Activity>[]> = userState.createProperty<Partial<Activity>[]>('Activity');
    const activeSkillProperty: StatePropertyAccessor<BotFrameworkSkill> = conversationState.createProperty<BotFrameworkSkill>(MainDialog.activeSkillPropertyName);
    const userProfileStateAccesor: StatePropertyAccessor<IUserProfileState> = userState.createProperty<IUserProfileState>('IUserProfileState');
    const onboardingDialog: OnboardingDialog = new OnboardingDialog(userProfileStateAccesor, botServices, localeTemplateManager);
    const switchSkillDialog: SwitchSkillDialog = new SwitchSkillDialog(conversationState);
    const mainDialog: MainDialog = new MainDialog(
        botServices,
        localeTemplateManager,
        userProfileStateAccesor,
        previousResponseAccesor,
        onboardingDialog,
        switchSkillDialog,
        skillDialogs,
        skillsConfig,
        activeSkillProperty
    );

    bot = new DefaultActivityHandler(conversationState, userState, localeTemplateManager, mainDialog, telemetryClient);
} catch (err) {
    throw err;
}

// Create server
const server: restify.Server = restify.createServer({ maxParamLength: 1000 });

// Enable the Application Insights middleware, which helps correlate all activity
// based on the incoming request.
server.use(restify.plugins.bodyParser());
server.use(restify.plugins.queryParser());
server.use(restify.plugins.authorizationParser());
server.use(ApplicationInsightsWebserverMiddleware);

server.listen(process.env.port || process.env.PORT || '3979', (): void => {
    console.log(` ${ server.name } listening to ${ server.url } `);
    console.log(`Get the Emulator: https://aka.ms/botframework-emulator`);
    console.log(`To talk to your bot, open your '.bot' file in the Emulator`);
});

// Listen for incoming requests
server.post('/api/messages', async (req: restify.Request, res: restify.Response): Promise<void> => {
    // Route received a request to adapter for processing
    await adapter.processActivity(req, res, async (turnContext: TurnContext): Promise<void> => {
        // route to bot activity handler.
        await bot.run(turnContext);
    });
});

// Register the request handler.
const handler: TokenExchangeSkillHandler = new TokenExchangeSkillHandler(adapter, bot, settings as IBotSettings, skillConversationIdFactory, skillsConfig, skillClient, credentialProvider, authenticationConfiguration, settings.tokenExchangeConfig as ITokenExchangeConfig);
const skillEndpoint = new ChannelServiceRoutes(handler);
skillEndpoint.register(server, '/api/skills');