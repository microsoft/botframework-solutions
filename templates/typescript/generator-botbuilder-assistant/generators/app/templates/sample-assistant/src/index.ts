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
    UserState } from 'botbuilder';
import { ApplicationInsightsTelemetryClient, ApplicationInsightsWebserverMiddleware } from 'botbuilder-applicationinsights';
import {
    CosmosDbStorage,
    CosmosDbStorageSettings } from 'botbuilder-azure';
import { Dialog } from 'botbuilder-dialogs';
import {
    IAuthenticationConnection,
    ISkillManifest,
    MicrosoftAppCredentialsEx,
    SkillContext,
    SkillDialog } from 'botbuilder-skills';
import {
    ICognitiveModelConfiguration,
    IOAuthConnection,
    Locales,
    MultiProviderAuthDialog } from 'botbuilder-solutions';
import { MicrosoftAppCredentials } from 'botframework-connector';
import i18next from 'i18next';
import i18nextNodeFsBackend from 'i18next-node-fs-backend';
import * as path from 'path';
import * as restify from 'restify';
import { DefaultAdapter } from './adapters/defaultAdapter';
import * as appsettings from './appsettings.json';
import { DialogBot } from './bots/dialogBot';
import * as cognitiveModelsRaw from './cognitivemodels.json';
import { CancelDialog } from './dialogs/cancelDialog';
import { EscalateDialog } from './dialogs/escalateDialog';
import { MainDialog } from './dialogs/mainDialog';
import { OnboardingDialog } from './dialogs/onboardingDialog';
import { IOnboardingState } from './models/onboardingState';
import { BotServices } from './services/botServices';
import { IBotSettings } from './services/botSettings';
import { skills as skillsRaw } from './skills.json';

// Configure internationalization and default locale
i18next.use(i18nextNodeFsBackend)
    .init({
        lowerCaseLng: true,
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

// Configure adapters
// DefaultAdapter is for all regular channels that use Http transport
const adapter: DefaultAdapter = new DefaultAdapter(
    botSettings,
    conversationState,
    adapterSettings,
    telemetryClient,
);

// DefaultWebSocketAdapter is for directline speech channel
// This adapter implementation is currently a workaround as
// later on we'll have a WebSocketEnabledHttpAdapter implementation that handles
// both Http for regular channels and websocket for directline speech channel
// const webSocketEnabledHttpAdapter: webSocketEnabledHttpAdapter = (botsettings, adapter))

let bot: DialogBot<Dialog>;
try {
    // Configure bot services
    const botServices: BotServices = new BotServices(botSettings, telemetryClient);

    const onboardingStateAccessor: StatePropertyAccessor<IOnboardingState> =
        userState.createProperty<IOnboardingState>('OnboardingState');
    const skillContextAccessor: StatePropertyAccessor<SkillContext> =
        userState.createProperty<SkillContext>(SkillContext.name);

    // Register dialogs
    const onboardingDialog: OnboardingDialog = new OnboardingDialog(botServices, onboardingStateAccessor, telemetryClient);
    const escalateDialog: EscalateDialog = new EscalateDialog(botServices, telemetryClient);
    const cancelDialog: CancelDialog = new CancelDialog();

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
        botSettings,
        botServices,
        onboardingDialog,
        escalateDialog,
        cancelDialog,
        skillDialogs,
        skillContextAccessor,
        onboardingStateAccessor,
        telemetryClient
    );

    // Configure bot
    bot = new DialogBot(conversationState, telemetryClient, mainDialog);
} catch (err) {
    throw err;
}

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
                return new MultiProviderAuthDialog(oauthConnections, credentials);
            }
        } else {
            throw new Error(`You must configure at least one supported OAuth connection to use this skill: ${ skill.name }.`);
        }
    }

    return undefined;
}
