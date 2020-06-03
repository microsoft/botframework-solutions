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
    SkillHandler, 
    BotFrameworkSkill} from 'botbuilder';
import { ApplicationInsightsTelemetryClient, ApplicationInsightsWebserverMiddleware } from 'botbuilder-applicationinsights';
import { CosmosDbPartitionedStorage, CosmosDbPartitionedStorageOptions } from 'botbuilder-azure';
import { Dialog, SkillDialog, SkillDialogOptions } from 'botbuilder-dialogs';
import {
    ICognitiveModelConfiguration,
    Locales,
    LocaleTemplateManager,
    SwitchSkillDialog,
    IEnhancedBotFrameworkSkill,
    SkillsConfiguration, 
    SkillConversationIdFactory } from 'bot-solutions';
import { SimpleCredentialProvider, AuthenticationConfiguration, Claim } from 'botframework-connector';
import i18next from 'i18next';
import i18nextNodeFsBackend from 'i18next-node-fs-backend';
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
import { AllowedCallersClaimsValidator } from './authentication/allowedCallersClaimsValidator';
import { TestSkillHandler } from './testSkillHandler';

// Configure internationalization and default locale
i18next.use(i18nextNodeFsBackend)
    .init({
        lowerCaseLng: true,
        fallbackLng: 'en-us',
        preload: ['de-de', 'en-us', 'es-es', 'fr-fr', 'it-it', 'zh-cn'],
        backend: {
            loadPath: join(__dirname, 'locales', '{{lng}}.json')
        }
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

const adapterSettings: Partial<BotFrameworkAdapterSettings> = {
    appId: botSettings.microsoftAppId,
    appPassword: botSettings.microsoftAppPassword
};

if (botSettings.cosmosDb === undefined) {
    throw new Error();
}

// Configure configuration provider
const credentialProvider: SimpleCredentialProvider = new SimpleCredentialProvider(appsettings.microsoftAppId, appsettings.microsoftAppPassword);

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

// Register the Bot Framework Adapter with error handling enabled.
// Note: some classes use the base BotAdapter so we add an extra registration that pulls the same instance.
const adapter: DefaultAdapter = new DefaultAdapter(
    botSettings,
    localeTemplateManager,
    conversationState,
    adapterSettings,
    telemetryInitializerMiddleware,
    telemetryClient
);

// Register AuthConfiguration to enable custom claim validation.
let authenticationConfiguration: AuthenticationConfiguration = new AuthenticationConfiguration();
// Create the skills configuration class
let skillsConfiguration: SkillsConfiguration = new SkillsConfiguration([], '') ;

// Register the skills conversation ID factory, the client.
const skillConversationIdFactory: SkillConversationIdFactory = new SkillConversationIdFactory(storage);
const skillHttpClient: SkillHttpClient = new SkillHttpClient(credentialProvider, skillConversationIdFactory);

// Configure bot
let bot: DefaultActivityHandler<Dialog>;
try {
    // Configure bot services
    const botServices: BotServices = new BotServices(botSettings, telemetryClient);

    const userProfileStateAccesor: StatePropertyAccessor<IUserProfileState> = userState.createProperty<IUserProfileState>('IUserProfileState');
    const onboardingDialog: OnboardingDialog = new OnboardingDialog(userProfileStateAccesor, botServices, localeTemplateManager);
    const switchSkillDialog: SwitchSkillDialog = new SwitchSkillDialog(conversationState);
    const previousResponseAccesor: StatePropertyAccessor<Partial<Activity>[]> = userState.createProperty<Partial<Activity>[]>('Activity');

    const activeSkillProperty: StatePropertyAccessor<BotFrameworkSkill> = conversationState.createProperty<BotFrameworkSkill>(MainDialog.activeSkillPropertyName);
    let skillDialogs: SkillDialog[] = [];
    // Register the SkillDialogs (remote skills).
    const skills: IEnhancedBotFrameworkSkill[] = appsettings.botFrameworkSkills;
    if (skills !== undefined && skills.length > 0) {
        const hostEndpoint: string = appsettings.skillHostEndpoint;
        if (hostEndpoint === undefined || hostEndpoint.trim().length === 0) {
            throw new Error('\'skillHostEndpoint\' is not in the configuration');
        } else {
            skillsConfiguration = new SkillsConfiguration(skills, hostEndpoint);
            const allowedCallersClaimsValidator: AllowedCallersClaimsValidator = new AllowedCallersClaimsValidator(skillsConfiguration);
    
            // Create AuthConfiguration to enable custom claim validation.
            authenticationConfiguration = new AuthenticationConfiguration(
                undefined,
                (claims: Claim[]) => allowedCallersClaimsValidator.validateClaims(claims)
            );

            skillDialogs = skills.map((skill: IEnhancedBotFrameworkSkill): SkillDialog => {
                const skillDialogOptions: SkillDialogOptions = {
                    botId: appsettings.microsoftAppId,
                    conversationIdFactory: skillConversationIdFactory,
                    skillClient: skillHttpClient,
                    skillHostEndpoint: hostEndpoint,
                    skill: skill,
                    conversationState: conversationState
                };
                return new SkillDialog(skillDialogOptions, skill.id);
            });
        }
    }

    const mainDialog: MainDialog = new MainDialog(
        botServices,
        localeTemplateManager,
        userProfileStateAccesor,
        previousResponseAccesor,
        onboardingDialog,
        switchSkillDialog,
        skillDialogs,
        skillsConfiguration,
        activeSkillProperty
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
const handler: TestSkillHandler = new TestSkillHandler(adapter, bot, skillConversationIdFactory, credentialProvider, authenticationConfiguration);
const skillEndpoint = new ChannelServiceRoutes(handler);
skillEndpoint.register(server, '/api/skills');
