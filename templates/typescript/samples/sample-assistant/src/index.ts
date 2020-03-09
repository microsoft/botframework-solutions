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
import { ApplicationInsightsTelemetryClient, ApplicationInsightsWebserverMiddleware } from 'botbuilder-applicationinsights';
import { CosmosDbStorage, CosmosDbStorageSettings } from 'botbuilder-azure';
import { Dialog, OAuthPromptSettings } from 'botbuilder-dialogs';
import {
    ICognitiveModelConfiguration,
    IOAuthConnection,
    Locales,
    MultiProviderAuthDialog,
    IAuthenticationConnection,
    ISkillManifest,
    LocaleTemplateEngineManager, 
    MicrosoftAppCredentialsEx,
    SkillContext,
    SkillDialog,
    SwitchSkillDialog } from 'botbuilder-solutions';
import { MicrosoftAppCredentials } from 'botframework-connector';
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
import { skills as skillsRaw } from './skills.json';
import { Activity } from 'botframework-schema';
import { TelemetryInitializerMiddleware } from 'botbuilder-applicationinsights';
import { IUserProfileState } from './models/userProfileState';

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

const skills: ISkillManifest[] = skillsRaw;
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
    microsoftAppPassword: appsettings.microsoftAppPassword,
    skills: skills
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
    appPassword: botSettings.microsoftAppPassword
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

// Configure storage
const storage: CosmosDbStorage = new CosmosDbStorage(cosmosDbStorageSettings);
const userState: UserState = new UserState(storage);
const conversationState: ConversationState = new ConversationState(storage);

// Configure credentials
const appCredentials: MicrosoftAppCredentials = new MicrosoftAppCredentials(
    botSettings.microsoftAppId || '',
    botSettings.microsoftAppPassword || ''
);

const localizedTemplates: Map<string, string[]> = new Map<string, string[]>();
const templateFiles: string[] = ['MainResponses','OnboardingResponses'];
const supportedLocales: string[] =  ['en-us','de-de','es-es','fr-fr','it-it','zh-cn'];
    
supportedLocales.forEach((locale: string): void => {
    const localeTemplateFiles: string[] = [];
    templateFiles.forEach((template: string): void => {
        // LG template for default locale should not include locale in file extension.
        if (locale === (botSettings.defaultLocale || 'en-us')) {
            localeTemplateFiles.push(path.join(__dirname, 'responses', `${ template }.lg`));
        }
        else {
            localeTemplateFiles.push(path.join(__dirname, 'responses', `${ template }.${ locale }.lg`));
        }
    });

    localizedTemplates.set(locale, localeTemplateFiles);
});
    
const localeTemplateEngine: LocaleTemplateEngineManager = new LocaleTemplateEngineManager(localizedTemplates, botSettings.defaultLocale || 'en-us');

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

    const skillContextAccessor: StatePropertyAccessor<SkillContext> = userState.createProperty<SkillContext>(SkillContext.name);
    const userProfileStateAccesor: StatePropertyAccessor<IUserProfileState> = userState.createProperty<IUserProfileState>('IUserProfileState');
    const onboardingDialog: OnboardingDialog = new OnboardingDialog(userProfileStateAccesor, botServices , localeTemplateEngine, telemetryClient);
    const switchSkillDialog: SwitchSkillDialog = new SwitchSkillDialog(conversationState);
    const previousResponseAccesor: StatePropertyAccessor<Partial<Activity>[]> =
    userState.createProperty<Partial<Activity>[]>('Activity');

    // Register skill dialogs
    const skillDialogs: SkillDialog[] = skills.map((skill: ISkillManifest): SkillDialog => {
        const authDialog: MultiProviderAuthDialog|undefined = buildAuthDialog(skill, botSettings, appCredentials);
        const credentials: MicrosoftAppCredentialsEx = new MicrosoftAppCredentialsEx(
            botSettings.microsoftAppId || '',
            botSettings.microsoftAppPassword || '',
            skill.msaAppId);

        return new SkillDialog(skill, credentials, telemetryClient, skillContextAccessor, authDialog);
    });
    
    const mainDialog: MainDialog = new MainDialog(
        botSettings as IBotSettings,
        botServices,
        localeTemplateEngine,
        userProfileStateAccesor,
        skillContextAccessor,
        previousResponseAccesor,
        onboardingDialog,
        switchSkillDialog,
        skillDialogs,
        telemetryClient,
    );

    bot = new DefaultActivityHandler(conversationState, userState, mainDialog);
} catch (err) {
    throw err;
}

const oAuthPromptSettings: OAuthPromptSettings[] = [];

// Create server
const server: restify.Server = restify.createServer();

// Enable the Application Insights middleware, which helps correlate all activity
// based on the incoming request.
server.use(restify.plugins.bodyParser());
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

// This method creates a MultiProviderAuthDialog based on a skill manifest.
function buildAuthDialog(
    skill: ISkillManifest,
    settings: Partial<IBotSettings>,
    credentials: MicrosoftAppCredentials): MultiProviderAuthDialog|undefined {
    if (skill.authenticationConnections !== undefined && skill.authenticationConnections.length > 0) {
        if (settings.oauthConnections !== undefined) {
            const oauthConnections: IOAuthConnection[] | undefined = settings.oauthConnections.filter(
                (oauthConnection: IOAuthConnection): boolean => {
                    return skill.authenticationConnections.some((authenticationConnection: IAuthenticationConnection): boolean => {
                        return authenticationConnection.serviceProviderId === oauthConnection.provider;
                    });
                });
            if (oauthConnections !== undefined) {
                return new MultiProviderAuthDialog(oauthConnections, credentials, oAuthPromptSettings);
            }
        } else {
            throw new Error(`You must configure at least one supported OAuth connection to use this skill: ${ skill.name }.`);
        }
    }

    return undefined;
}
