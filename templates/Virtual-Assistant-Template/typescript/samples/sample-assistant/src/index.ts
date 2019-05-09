/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotFrameworkAdapterSettings, BotTelemetryClient, NullTelemetryClient, StatePropertyAccessor, TurnContext } from 'botbuilder';
import { ApplicationInsightsTelemetryClient, ApplicationInsightsWebserverMiddleware } from 'botbuilder-applicationinsights';
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
import i18next from 'i18next';
import i18nextNodeFsBackend from 'i18next-node-fs-backend';
import * as path from 'path';
import * as restify from 'restify';
import { DefaultAdapter } from './adapters/defaultAdapter';
import * as appsettings from './appsettings.json';
import { DialogBot } from './bots/dialogBot';
import * as cognitiveModelsRaw from './cognitivemodels.json';
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
    fallbackLng: 'en',
    preload: [ 'de', 'en', 'es', 'fr', 'it', 'zh' ],
    backend: {
        loadPath: path.join(__dirname, 'locales', '{{lng}}.json')
    }
})
.then(async () => {
    await Locales.addResourcesFromPath(i18next, 'common');
});

const skills: ISkillManifest[] = skillsRaw;
const cognitiveModels: Map<string, ICognitiveModelConfiguration> = new Map();
const cognitiveModelDictionary: { [key: string]: Object } = cognitiveModelsRaw.cognitiveModels;
const cognitiveModelMap: Map<string, Object>  = new Map(Object.entries(cognitiveModelDictionary));
cognitiveModelMap.forEach((value: Object, key: string) => {
    cognitiveModels.set(key, <ICognitiveModelConfiguration> value);
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
    if (settings && settings.appInsights && settings.appInsights.instrumentationKey) {
        const instrumentationKey: string = settings.appInsights.instrumentationKey;

        return new ApplicationInsightsTelemetryClient(instrumentationKey);
    }

    return new NullTelemetryClient();
}

const telemetryClient: BotTelemetryClient = getTelemetryClient(botSettings);

const adapterSettings: Partial<BotFrameworkAdapterSettings> = {
    appId: botSettings.microsoftAppId,
    appPassword: botSettings.microsoftAppPassword
};
const adapter: DefaultAdapter = new DefaultAdapter(botSettings, adapterSettings, telemetryClient);

let bot: DialogBot<Dialog>;
try {
    const botServices: BotServices = new BotServices(botSettings, telemetryClient);

    const onboardingStateAccessor: StatePropertyAccessor<IOnboardingState> =
        adapter.userState.createProperty<IOnboardingState>('OnboardingState');
    const skillContextAccessor: StatePropertyAccessor<SkillContext> =
        adapter.userState.createProperty<SkillContext>(SkillContext.name);

    const onboardingDialog: OnboardingDialog = new OnboardingDialog(botServices, onboardingStateAccessor, telemetryClient);
    const escalateDialog: EscalateDialog = new EscalateDialog(botServices, telemetryClient);
    const skillDialogs: SkillDialog[] = skills.map((skill: ISkillManifest) => {
        const authDialog: MultiProviderAuthDialog|undefined = buildAuthDialog(skill, botSettings);
        const credentials: MicrosoftAppCredentialsEx = new MicrosoftAppCredentialsEx(
            botSettings.microsoftAppId || '',
            botSettings.microsoftAppPassword || '',
            skill.msAppId);

        return new SkillDialog(skill, credentials, telemetryClient, skillContextAccessor, authDialog);
    });
    const mainDialog: MainDialog = new MainDialog(
        botSettings,
        botServices,
        onboardingDialog,
        escalateDialog,
        skillDialogs,
        onboardingStateAccessor,
        telemetryClient
    );

    bot = new DialogBot(adapter.conversationState, telemetryClient, mainDialog);
} catch (err) {
    throw err;
}

// Create server
const server: restify.Server = restify.createServer();

// Enable the Application Insights middleware, which helps correlate all activity
// based on the incoming request.
server.use(restify.plugins.bodyParser());
server.use(ApplicationInsightsWebserverMiddleware);

server.listen(3979, (): void => {
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
        await bot.run(turnContext);
    });
});

// This method creates a MultiProviderAuthDialog based on a skill manifest.
function buildAuthDialog(skill: ISkillManifest, settings: Partial<IBotSettings>): MultiProviderAuthDialog|undefined {
    if (skill.authenticationConnections !== undefined && skill.authenticationConnections.length > 0) {
        if (settings.oauthConnections !== undefined) {
            const oauthConnections: IOAuthConnection[] | undefined = settings.oauthConnections.filter(
                (oauthConnection: IOAuthConnection) => {
                return skill.authenticationConnections.some((authenticationConnection: IAuthenticationConnection) => {
                    return authenticationConnection.serviceProviderId === oauthConnection.provider;
                });
            });
            if (oauthConnections !== undefined) {
                return new MultiProviderAuthDialog(oauthConnections);
            }
        } else {
            throw new Error(`You must configure at least one supported OAuth connection to use this skill: ${skill.name}.`);
        }
    }

    return undefined;
}
