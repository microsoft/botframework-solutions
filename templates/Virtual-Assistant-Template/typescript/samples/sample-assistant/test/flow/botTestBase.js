// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

const { ActivityTypes } = require('botframework-schema');
const { TestAdapter } = require('botbuilder-core');
const { AutoSaveStateMiddleware, ConversationState, MemoryStorage, NullTelemetryClient, TelemetryLoggerMiddleware, UserState } = require('botbuilder');
const { EventDebuggerMiddleware, Locales, SetLocaleMiddleware } = require('botbuilder-solutions');
const i18next = require('i18next');
const i18nextNodeFsBackend = require('i18next-node-fs-backend');
const path = require('path');
const { BotServices } = require('../../lib/services/botServices');
const { DialogBot } = require('../../lib/bots/dialogBot.js');
const { OnboardingDialog } = require('../../lib/dialogs/onboardingDialog.js')
const { EscalateDialog } = require('../../lib/dialogs/escalateDialog.js')
const { CancelDialog } = require('../../lib/dialogs/cancelDialog.js')
const { MainDialog } = require('../../lib/dialogs/mainDialog')
const appsettings = require('../appsettings.json');
const cognitiveModelsRaw = require ('../cognitivemodels.json');
const skills = require ('../skills.json');

const TEST_MODE = require('../testBase').testMode;
//const getTelemetryClient = require('../../lib/index.js').getTelemetryClient;

const setupEnvironment = function (testMode) {
    switch (testMode) {
        case 'record':
            break;
        case 'lockdown':
            break;
    }
}

async function configuration() {
    // Configure internationalization and default locale
    await i18next.use(i18nextNodeFsBackend)
    .init({
        fallbackLng: 'en',
        preload: [ 'de', 'en', 'es', 'fr', 'it', 'zh' ],
        backend: {
            loadPath: path.join(__dirname, '..', '..', 'lib', 'locales', '{{lng}}.json')
        }
    })
    .then(async () => {
        await Locales.addResourcesFromPath(i18next, 'common');
    });

    setupEnvironment(TEST_MODE);
}

const cognitiveModels = new Map();
const cognitiveModelDictionary = cognitiveModelsRaw.cognitiveModels;
const cognitiveModelMap = new Map(Object.entries(cognitiveModelDictionary));
cognitiveModelMap.forEach((value, key) => {
    cognitiveModels.set(key, value);
});

/**
 * Initializes the properties for the bot to be tested.
 */
const initialize = async function(testStorage) {
    await configuration();
    
    const telemetryClient = getTelemetryClient(botSettings);
    const botSettings = new BotSettings(appsettings.appInsights, appsettings.blobStorage, cognitiveModels, appsettings.cosmosDb, cognitiveModels.defaultLocale, appsettings.microsoftAppId, appsettings.microsoftAppPassword, skills)
    const adapterSettings = {
        appId: botSettings.microsoftAppId,
        appPassword: botSettings.microsoftAppPassword
    };
    const adapter = new DefaultAdapter(botSettings, adapterSettings, telemetryClient);
    const botServices = new BotServices(botSettings);
    const onboardingStateAccessor = adapter.userState.createProperty('OnboardingState');
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
    this.bot = new DialogBot(adapter.conversationState, telemetryClient, mainDialog);
}

/**
 * Initializes the TestAdapter.
 * @returns TestAdapter with the Bot logic configured.
 */
const getTestAdapter = function() {
    const bot = this.bot;

    return new TestAdapter(async function (context) {
        const cultureInfo = context.activity.locale || 'en';
        await i18next.changeLanguage(cultureInfo);
        return bot.onTurn(context);
    })
    .use(new AutoSaveStateMiddleware(bot.conversationState, bot.userState));
}

async function getTestAdapterDefault() {
    await configuration();
    const botSettings = {
        microsoftAppId: appsettings.microsoftAppId,
        microsoftAppPassword: appsettings.microsoftAppPassword,
        defaultLocale: cognitiveModels.defaultLocale,
        oauthConnections: [],
        cosmosDb: appsettings.cosmosDb,
        appInsights: appsettings.appInsights,
        blobStorage: appsettings.blobStorage,
        contentModerator: '',
        cognitiveModels: cognitiveModels,
        properties: {},
        skills: skills
    };

    const telemetryClient = new NullTelemetryClient();
    const storage = new MemoryStorage();
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
    adapter.use(new SetLocaleMiddleware(botSettings.defaultLocale || 'en-us'));
    adapter.use(new EventDebuggerMiddleware());
    adapter.use(new AutoSaveStateMiddleware(conversationState, userState));
    return adapter;
}

module.exports = {
    configuration: configuration,
    initialize: initialize,
    getTestAdapter: getTestAdapter,
    getTestAdapterDefault: getTestAdapterDefault
}