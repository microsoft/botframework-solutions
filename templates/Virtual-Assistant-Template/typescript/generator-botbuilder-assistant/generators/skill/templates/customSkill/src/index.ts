/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TelemetryClient } from 'applicationinsights';
import {
    BotFrameworkAdapter,
    BotFrameworkAdapterSettings,
    ConversationState,
    StatePropertyAccessor,
    TurnContext,
    UserState } from 'botbuilder';
import {
    CosmosDbStorage,
    CosmosDbStorageSettings } from 'botbuilder-azure';
import {
    Dialog,
    DialogState } from 'botbuilder-dialogs';
import {
    IAuthenticationConnection,
    ISkillManifest,
    SkillContext,
    SkillHttpAdapter} from 'botbuilder-skills';
import {
    ICognitiveModelConfiguration,
    IOAuthConnection,
    Locales,
    MultiProviderAuthDialog,
    ResponseManager} from 'botbuilder-solutions';
import i18next from 'i18next';
import i18nextNodeFsBackend from 'i18next-node-fs-backend';
import * as path from 'path';
import * as restify from 'restify';
import { CustomSkillAdapter } from './adapters/customSkillAdapter';
import { DefaultAdapter } from './adapters/defaultAdapter';
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
    fallbackLng: 'en',
    preload: [ 'de', 'en', 'es', 'fr', 'it', 'zh' ]
})
.then(async () => {
    await Locales.addResourcesFromPath(i18next, 'common');
});

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
    microsoftAppPassword: appsettings.microsoftAppPassword
};
if (botSettings.appInsights === undefined) {
    throw new Error('There is no appInsights value in appsettings file');
}

let cosmosDbStorageSettings: CosmosDbStorageSettings;
if (botSettings.cosmosDb === undefined) {
    throw new Error();
}
cosmosDbStorageSettings = {
    authKey: botSettings.cosmosDb.authkey,
    collectionId: botSettings.cosmosDb.collectionId,
    databaseId: botSettings.cosmosDb.databaseId,
    serviceEndpoint: botSettings.cosmosDb.cosmosDBEndpoint
};

const storage: CosmosDbStorage = new CosmosDbStorage(cosmosDbStorageSettings);
const userState: UserState = new UserState(storage);
const conversationState: ConversationState = new ConversationState(storage);
const telemetryClient: TelemetryClient = new TelemetryClient(botSettings.appInsights.instrumentationKey);
const stateAccessor: StatePropertyAccessor<SkillState> = userState.createProperty(SkillState.name);
const skillContextAccessor: StatePropertyAccessor<SkillContext> = userState.createProperty(SkillContext.name);
const dialogStateAccessor: StatePropertyAccessor<DialogState> = userState.createProperty('DialogState');
const adapterSettings: Partial<BotFrameworkAdapterSettings> = {
    appId: botSettings.microsoftAppId,
    appPassword: botSettings.microsoftAppPassword
};
const botAdapter: DefaultAdapter = new DefaultAdapter(
    botSettings,
    adapterSettings,
    userState,
    conversationState,
    telemetryClient);

const customSkillAdapter: CustomSkillAdapter = new CustomSkillAdapter(
    botSettings,
    userState,
    conversationState,
    telemetryClient,
    skillContextAccessor,
    dialogStateAccessor);
const adapter: SkillHttpAdapter = new SkillHttpAdapter(
    customSkillAdapter
);

let bot: DialogBot<Dialog>;
try {

    const responseManager: ResponseManager = new ResponseManager(
        ['en', 'de', 'es', 'fr', 'it', 'zh'],
        [SampleResponses, MainResponses, SharedResponses]);
    const botServices: BotServices = new BotServices(botSettings);
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

    bot = new DialogBot(conversationState, telemetryClient, mainDialog);
} catch (err) {
    throw err;
}

// Create server
const server: restify.Server = restify.createServer();
server.listen(3980, (): void => {
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
    botAdapter.processActivity(req, res, async (turnContext: TurnContext) => {
        // route to bot activity handler.
        await bot.run(turnContext);
    });
});

// Listen for incoming assistant requests
server.post('/api/skill/messages', (req: restify.Request, res: restify.Response) => {
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
