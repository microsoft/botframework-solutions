/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License
 */

const { join } = require('path');
const { ActivityTypes } = require('botframework-schema');
const { TestAdapter } = require('botbuilder-core');
const { AutoSaveStateMiddleware, ConversationState, MemoryStorage, NullTelemetryClient, TelemetryLoggerMiddleware, UserState } = require('botbuilder');
const { EventDebuggerMiddleware, FeedbackMiddleware, Locales, LocaleTemplateEngineManager, SetLocaleMiddleware, SwitchSkillDialog, SkillsConfiguration } = require('botbuilder-solutions');
const i18next = require('i18next');
const i18nextNodeFsBackend = require('i18next-node-fs-backend');
const { BotServices } = require('../../lib/services/botServices');
const { DefaultActivityHandler } = require('../../lib/bots/defaultActivityHandler');
const { OnboardingDialog } = require('../../lib/dialogs/onboardingDialog');
const { MainDialog } = require('../../lib/dialogs/mainDialog');

const TEST_MODE = require('./testBase').testMode;
const resourcesDir = TEST_MODE === 'lockdown' ? join('..', 'mocks', 'resources') : join('..', '..', 'src');

const appSettings = require(join(resourcesDir, 'appsettings.json'));
const cognitiveModelsRaw = require(join(resourcesDir, 'cognitivemodels.json'));
const cognitiveModels = new Map();
const cognitiveModelDictionary = cognitiveModelsRaw.cognitiveModels;
const cognitiveModelMap = new Map(Object.entries(cognitiveModelDictionary));
cognitiveModelMap.forEach((value, key) => {
    cognitiveModels.set(key, value);
});
const localizedTemplates = new Map();
const templateFiles = ['MainResponses','OnboardingResponses'];
const supportedLocales =  ['en-us','de-de','es-es','fr-fr','it-it','zh-cn'];
supportedLocales.forEach(locale => {
    const localeTemplateFiles = [];
    templateFiles.forEach(template => {
        // LG template for default locale should not include locale in file extension.
        if (locale === 'en-us'){
            localeTemplateFiles.push(join(__dirname, '..', '..', 'lib', 'responses', `${template}.lg`));
        }
        else {
            localeTemplateFiles.push(join(__dirname, '..', '..', 'lib', 'responses', `${template}.${locale}.lg`));
        }
    });

    localizedTemplates.set(locale, localeTemplateFiles);
});

const templateEngine = new LocaleTemplateEngineManager(localizedTemplates, 'en-us');
const testUserProfileState = { name: 'Bot' };

async function initConfiguration() {
    // Configure internationalization and default locale
    await i18next.use(i18nextNodeFsBackend)
    .init({
        fallbackLng: 'en-us',
        preload: ['de-de', 'en-us', 'es-es', 'fr-fr', 'it-it', 'zh-cn'],
        backend: {
            loadPath: join(__dirname, '..', '..', 'lib', 'locales', '{{lng}}.json')
        }
    })
    .then(async () => {
        await Locales.addResourcesFromPath(i18next, 'common');
        await i18next.changeLanguage('en-us');
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
        properties: {}
    };

    const telemetryClient = new NullTelemetryClient();
    const storage = settings.storage || new MemoryStorage();
    // create conversation and user state
    const conversationState = new ConversationState(storage);
    const userState = new UserState(storage);

    const botServices = new BotServices(botSettings);
    const botServicesAccesor = userState.createProperty(BotServices.name)
    const onboardingDialog = new OnboardingDialog(botServicesAccesor, botServices, templateEngine, telemetryClient);
    const skillDialogs = [];
    const userProfileStateAccesor = userState.createProperty('IUserProfileState');
    const previousResponseAccesor = userState.createProperty('Activity');
    const switchSkillDialog = new SwitchSkillDialog(conversationState);
    const skillsConfig = new SkillsConfiguration([], '');
    const mainDialog = new MainDialog(
        botSettings,
        botServices,
        templateEngine,
        userProfileStateAccesor,
        previousResponseAccesor,
        onboardingDialog,
        switchSkillDialog,
        skillDialogs,
        skillsConfig,
        telemetryClient
    );

    const botLogic = new DefaultActivityHandler(conversationState, userState, templateEngine, mainDialog);
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
    adapter.use(new FeedbackMiddleware(conversationState, telemetryClient));
    return adapter;
}

module.exports = {
    getTestAdapterDefault: getTestAdapterDefault,
    templateEngine: templateEngine,
    testUserProfileState: testUserProfileState
}
