/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    BotFrameworkAdapterSettings,
    TurnContext } from 'botbuilder';
import { Dialog } from 'botbuilder-dialogs';
import {
    IAuthenticationConnection,
    ISkillManifest } from 'botbuilder-skills';
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
import { MainResponses } from './responses/main/mainResponses';
import { SampleResponses } from './responses/sample/sampleResponses';
import { SharedResponses } from './responses/shared/sharedResponses';
import { BotServices } from './services/botServices';
import { IBotSettings } from './services/botSettings';

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
const adapterSettings: Partial<BotFrameworkAdapterSettings> = {
    appId: botSettings.microsoftAppId,
    appPassword: botSettings.microsoftAppPassword
};
const adapter: DefaultAdapter = new DefaultAdapter(botSettings, adapterSettings);
const skillAdapter: CustomSkillAdapter = new CustomSkillAdapter(botSettings, adapter.userState, adapter.conversationState);
let bot: DialogBot<Dialog>;
try {

    const responseManager: ResponseManager = new ResponseManager(
        ['en', 'de', 'es', 'fr', 'it', 'zh'],
        [new SampleResponses(), new MainResponses(), new SharedResponses()]);
    const botServices: BotServices = new BotServices(botSettings);
    const sampleDialog: SampleDialog = new SampleDialog(
        botSettings,
        botServices,
        responseManager,
        adapter.conversationState,
        adapter.telemetryClient
    );
    const mainDialog: MainDialog = new MainDialog(
        botSettings,
        botServices,
        responseManager,
        adapter.userState,
        adapter.conversationState,
        sampleDialog,
        adapter.telemetryClient
    );

    bot = new DialogBot(adapter.conversationState, adapter.telemetryClient, mainDialog);
} catch (err) {
    throw err;
}

// Create server
const server: restify.Server = restify.createServer();
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
