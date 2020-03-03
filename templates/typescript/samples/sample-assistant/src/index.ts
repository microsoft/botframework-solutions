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
    TelemetryLoggerMiddleware,
    SkillHttpClient,
    ChannelServiceHandler} from 'botbuilder';
import { ApplicationInsightsTelemetryClient, ApplicationInsightsWebserverMiddleware } from 'botbuilder-applicationinsights';
import { CosmosDbStorage, CosmosDbStorageSettings } from 'botbuilder-azure';
import { Dialog } from 'botbuilder-dialogs';
import {
    ICognitiveModelConfiguration,
    Locales,
    LocaleTemplateEngineManager,
    SkillDialog,
    SwitchSkillDialog,
    IEnhancedBotFrameworkSkill, 
    SkillsConfiguration,
    SkillConversationIdFactory} from 'botbuilder-solutions';
import { SimpleCredentialProvider, AuthenticationConfiguration, Claim } from 'botframework-connector';
import i18next from 'i18next';
import i18nextNodeFsBackend from 'i18next-node-fs-backend';
import * as path from 'path';
import * as restify from 'restify';
import { DefaultAdapter } from './adapters/defaultAdapter';
import * as appsettings from './appsettings.json';
import { DefaultActivityHandler } from './bots/defaultActivityHandler';
import * as cognitiveModelsRaw from './cognitivemodels.json';
import { MainDialog } from './dialogs/mainDialog';
import { OnboardingDialog } from './dialogs/onboardingDialog';
import { BotServices } from './services/botServices';
import { IBotSettings } from './services/botSettings';
import { Activity, ResourceResponse } from 'botframework-schema';
import { TelemetryInitializerMiddleware } from 'botbuilder-applicationinsights';
import { IUserProfileState } from './models/userProfileState'
import { AllowedCallersClaimsValidator } from './authentication/allowedCallersClaimsValidator';

// Configure internationalization and default locale
i18next.use(i18nextNodeFsBackend)
    .init({
        fallbackLng: 'en-us',
        preload: ['de-de', 'en-us', 'es-es', 'fr-fr', 'it-it', 'zh-cn'],
        backend: {
            loadPath: path.join(__dirname, 'locales', '{{lng}}.json')
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

const skills: IEnhancedBotFrameworkSkill[] = appsettings.botFrameworkSkills;
const skillHostEndpoint: string = appsettings.skillHostEndpoint;
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

const adapterSettings: Partial<BotFrameworkAdapterSettings> = {
    appId: botSettings.microsoftAppId,
    appPassword: botSettings.microsoftAppPassword,
    channelService: ""
};

if (botSettings.cosmosDb === undefined) {
    throw new Error();
}

const cosmosDbStorageSettings: CosmosDbStorageSettings = {
    authKey: botSettings.cosmosDb.authKey,
    collectionId: botSettings.cosmosDb.collectionId,
    databaseId: botSettings.cosmosDb.databaseId,
    serviceEndpoint: botSettings.cosmosDb.cosmosDBEndpoint
};

const credentialProvider: SimpleCredentialProvider = new SimpleCredentialProvider(botSettings.microsoftAppId || "", botSettings.microsoftAppPassword || "");

// Configure storage
const storage: CosmosDbStorage = new CosmosDbStorage(cosmosDbStorageSettings);
const userState: UserState = new UserState(storage);
const conversationState: ConversationState = new ConversationState(storage);

// Configure localized responses
const localizedTemplates: Map<string, string[]> = new Map<string, string[]>();
const templateFiles: string[] = ['MainResponses', 'OnboardingResponses'];
const supportedLocales: string[] = ['en-us', 'de-de', 'es-es', 'fr-fr', 'it-it', 'zh-cn'];

supportedLocales.forEach((locale: string) => {
    const localeTemplateFiles: string[] = [];
    templateFiles.forEach(template => {
        // LG template for en-us does not include locale in file extension.
        if (locale === 'en-us') {
            localeTemplateFiles.push(path.join(__dirname, 'responses', `${template}.lg`));
        }
        else {
            localeTemplateFiles.push(path.join(__dirname, 'responses', `${ template }.${ locale }.lg`));
        }
    });

    localizedTemplates.set(locale, localeTemplateFiles);
});

const localeTemplateEngine: LocaleTemplateEngineManager = new LocaleTemplateEngineManager(localizedTemplates, botSettings.defaultLocale || 'en-us')

// Create the skills configuration class
let authConfig: AuthenticationConfiguration;
let skillConfiguration: SkillsConfiguration;
if (skills !== undefined && skills.length > 0 && skillHostEndpoint.trim().length !== 0) {
        skillConfiguration = new SkillsConfiguration(skills, skillHostEndpoint);
        const allowedCallersClaimsValidator: AllowedCallersClaimsValidator = new AllowedCallersClaimsValidator(skillConfiguration);

        // Create AuthConfiguration to enable custom claim validation.
        authConfig = new AuthenticationConfiguration(
            undefined,
            (claims: Claim[]) => allowedCallersClaimsValidator.validateClaims(claims)
        );
}

const telemetryLoggerMiddleware: TelemetryLoggerMiddleware = new TelemetryLoggerMiddleware(telemetryClient);
const telemetryInitializerMiddleware: TelemetryInitializerMiddleware = new TelemetryInitializerMiddleware(telemetryLoggerMiddleware);

const adapter: DefaultAdapter = new DefaultAdapter(
    botSettings,
    localeTemplateEngine,
    conversationState,
    adapterSettings,
    telemetryInitializerMiddleware,
    telemetryClient
);

let bot: DefaultActivityHandler<Dialog>;
try {
    // Configure bot services
    const botServices: BotServices = new BotServices(botSettings, telemetryClient);

    const userProfileStateAccesor: StatePropertyAccessor<IUserProfileState> = userState.createProperty<IUserProfileState>('IUserProfileState');
    const onboardingDialog: OnboardingDialog = new OnboardingDialog(userProfileStateAccesor, botServices, localeTemplateEngine, telemetryClient);
    const switchSkillDialog: SwitchSkillDialog = new SwitchSkillDialog(conversationState);
    const previousResponseAccesor: StatePropertyAccessor<Partial<Activity>[]> =
        userState.createProperty<Partial<Activity>[]>('Activity');


    const skillHttpClient: SkillHttpClient = new SkillHttpClient(
        credentialProvider,
        new SkillConversationIdFactory(storage)
    );

    let skillDialogs: SkillDialog[] = [];
    if (skills !== undefined && skills.length > 0) {
        if (skillHostEndpoint === undefined) {
            throw new Error("'skillHostEndpoint' is not in the configuration");
        } else {
            skillDialogs = skills.map((skill: IEnhancedBotFrameworkSkill): SkillDialog => {
                return new SkillDialog(conversationState, skillHttpClient, skill, <IBotSettings> botSettings, skillHostEndpoint);
            });
        }
    }

    const mainDialog: MainDialog = new MainDialog(
        botSettings as IBotSettings,
        botServices,
        localeTemplateEngine,
        userProfileStateAccesor,
        previousResponseAccesor,
        onboardingDialog,
        switchSkillDialog,
        skillDialogs,
        skillsConfiguration,
        telemetryClient,
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
server.use(restify.plugins.queryParser());
server.use(restify.plugins.authorizationParser());
server.use(ApplicationInsightsWebserverMiddleware);

server.listen(process.env.port || process.env.PORT || '3979', (): void => {
    console.log(` ${server.name} listening to ${server.url} `);
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

let handler: ChannelServiceHandler = new ChannelServiceHandler(credentialProvider, new AuthenticationConfiguration());

server.post('/api/skills/v3/conversations/:conversationId/activities/:activityId', async (req: restify.Request): Promise<ResourceResponse> => {
    const activity: Activity = JSON.parse(req.body);
    return await handler.handleReplyToActivity(req.authorization?.credentials || "", req.params.conversationId, req.params.activityId, activity);
});

server.post('/api/skills/v3/conversations/:conversationId/activities', async (req: restify.Request): Promise<ResourceResponse> => {
    const activity: Activity = JSON.parse(req.body);
    return await handler.handleSendToConversation(req.authorization?.credentials || "", req.params.conversationId, activity);
});
