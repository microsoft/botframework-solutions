/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License
 */

const { join } = require('path');

const { ActivityTypes } = require('botframework-schema');
const { TestAdapter } = require('botbuilder-core');
const { AutoSaveStateMiddleware, ConversationState, MemoryStorage, NullTelemetryClient, TelemetryLoggerMiddleware, UserState } = require('botbuilder');
const { EventDebuggerMiddleware, Locales, SetLocaleMiddleware } = require('botbuilder-solutions');
const i18next = require('i18next');
const i18nextNodeFsBackend = require('i18next-node-fs-backend');

const { BotServices } = require('../../lib/services/botServices');
const { DialogBot } = require('../../lib/bots/dialogBot');
const { OnboardingDialog } = require('../../lib/dialogs/onboardingDialog');
const { EscalateDialog } = require('../../lib/dialogs/escalateDialog');
const { CancelDialog } = require('../../lib/dialogs/cancelDialog');
const { MainDialog } = require('../../lib/dialogs/mainDialog');

const TEST_MODE = require('../testBase').testMode;

const resourcesDir = TEST_MODE === 'lockdown' ? join('..', 'mockedResources') : join('..', '..', 'src');

const appSettings = require(join(resourcesDir, 'appsettings.json'));
const skills = require(join(resourcesDir, 'skills.json')).skills;

const cognitiveModelsRaw = require(join(resourcesDir, 'cognitivemodels.json'));
const cognitiveModels = new Map();
const cognitiveModelDictionary = cognitiveModelsRaw.cognitiveModels;
const cognitiveModelMap = new Map(Object.entries(cognitiveModelDictionary));
cognitiveModelMap.forEach((value, key) => {
    cognitiveModels.set(key, value);
});

async function initConfiguration() {
    // Configure internationalization and default locale
    await i18next.use(i18nextNodeFsBackend)
    .init({
        fallbackLng: 'en',
        preload: [ 'de', 'en', 'es', 'fr', 'it', 'zh' ],
        backend: {
            loadPath: join(__dirname, '..', '..', 'lib', 'locales', '{{lng}}.json')
        }
    })
    .then(async () => {
        await Locales.addResourcesFromPath(i18next, 'common');
    });
}

async function getTestAdapterDefault(settings) {
    // validate settings
    if (!settings) settings = {};
    
    await initConfiguration();
    const botSettings = {
        microsoftAppId: appSettings.microsoftAppId,
        microsoftAppPassword: appSettings.microsoftAppPassword,
        defaultLocale: cognitiveModelsRaw.defaultLocale,
        oauthConnections: [],
        cosmosDb: appSettings.cosmosDb,
        appInsights: appSettings.appInsights,
        blobStorage: appSettings.blobStorage,
        contentModerator: '',
        cognitiveModels: cognitiveModels,
        properties: {},
        skills: skills
    };

    const telemetryClient = new NullTelemetryClient();
    const storage = settings.storage || new MemoryStorage();
    // create conversation and user state
    const conversationState = new ConversationState(storage);
    const userState = new UserState(storage);

    const adapterSettings = {
        appId: botSettings.microsoftAppId,
        appPassword: botSettings.microsoftAppPassword
    };
    const botServices = new BotServices(botSettings);
    const onboardingStateAccessor = userState.createProperty('OnboardingState');
    const onboardingDialog = new OnboardingDialog(botServices, onboardingStateAccessor, telemetryClient)  
    const escalateDialog = new EscalateDialog(botServices, telemetryClient);
    const cancelDialog = new CancelDialog();
    const skillDialogs = [];
    const mainDialog = new MainDialog(
        botSettings,
        botServices,
        onboardingDialog,
        escalateDialog,
        cancelDialog,
        skillDialogs,
        onboardingStateAccessor,
        telemetryClient
    );

    const botLogic = new DialogBot(conversationState, telemetryClient, mainDialog);

    const adapter = new TestAdapter(botLogic.run.bind(botLogic));

    adapter.onTurnError = async function(context, error) {
        await context.sendActivity({
            type: ActivityTypes.Trace,
            text: error.message
        });
        await context.sendActivity({
            type: ActivityTypes.Trace,
            text: error.stack
        });
        await context.sendActivity(i18next.t('main.error'));
        telemetryClient.trackException({ exception: error });
    };
    
    adapter.use(new TelemetryLoggerMiddleware(telemetryClient, true));
    adapter.use(new SetLocaleMiddleware(botSettings.defaultLocale || 'en'));
    adapter.use(new EventDebuggerMiddleware());
    adapter.use(new AutoSaveStateMiddleware(conversationState, userState));
    return adapter;
}

module.exports = {
    getTestAdapterDefault: getTestAdapterDefault
}
