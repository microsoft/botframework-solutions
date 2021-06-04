/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License
 */

const { join } = require('path');
const { ActivityTypes } = require('botframework-schema');
const { TestAdapter } = require('botbuilder-core');
const { AutoSaveStateMiddleware, ConversationState, MemoryStorage, NullTelemetryClient, TelemetryLoggerMiddleware, UserState } = require('botbuilder');
const { EventDebuggerMiddleware, LocaleTemplateManager, SetLocaleMiddleware, SwitchSkillDialog, SkillsConfiguration } = require('bot-solutions');
const { BotServices } = require('../../lib/services/botServices');
const { DefaultActivityHandler } = require('../../lib/bots/defaultActivityHandler');
const { OnboardingDialog } = require('../../lib/dialogs/onboardingDialog');
const { MainDialog } = require('../../lib/dialogs/mainDialog');
const { Templates } = require('botbuilder-lg');

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
const templateFile = 'AllResponses';
const supportedLocales = ['en-us', 'de-de', 'es-es', 'fr-fr', 'it-it', 'zh-cn'];

function getAllResponsesTemplates(locale) {
    const path = locale === 'en-us'
        ? join(__dirname, '..', '..', 'lib', 'responses', `AllResponses.lg`)
        : join(__dirname, '..', '..', 'lib', 'responses', `AllResponses.${ locale }.lg`);
    return Templates.parseFile(path);
}

supportedLocales.forEach((locale) => {
    // LG template for en-us does not include locale in file extension.
    const localTemplateFile = locale === 'en-us'
        ? join(__dirname, '..', '..', 'lib', 'responses', `${ templateFile }.lg`)
        : join(__dirname, '..', '..', 'lib', 'responses', `${ templateFile }.${ locale }.lg`);
    localizedTemplates.set(locale, localTemplateFile);
});

const templateManager = new LocaleTemplateManager(localizedTemplates, 'en-us');
const testUserProfileState = { name: 'Bot' };

async function getTestAdapterDefault(settings) {
    // validate settings
    if (!settings) settings = {};
    
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
    const onboardingDialog = new OnboardingDialog(botServicesAccesor, botServices, templateManager, telemetryClient);
    const skillDialogs = [];
    const userProfileStateAccesor = userState.createProperty('IUserProfileState');
    const previousResponseAccesor = userState.createProperty('Activity');
    const switchSkillDialog = new SwitchSkillDialog(conversationState);
    const skillsConfig = new SkillsConfiguration([], '');
    const activeSkillProperty = conversationState.createProperty('BotFrameworkSkill');
    const mainDialog = new MainDialog(
        botServices,
        templateManager,
        userProfileStateAccesor,
        previousResponseAccesor,
        onboardingDialog,
        switchSkillDialog,
        skillDialogs,
        skillsConfig,
        activeSkillProperty
    );

    const botLogic = new DefaultActivityHandler(conversationState, userState, templateManager, mainDialog);
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
        
        await context.sendActivity(templateManager.generateActivityForLocale('ErrorMessage', context.activity.locale));
        telemetryClient.trackException({ exception: error });
    };
    
    adapter.use(new TelemetryLoggerMiddleware(telemetryClient, true));
    adapter.use(new SetLocaleMiddleware(botSettings.defaultLocale || 'en-us'));
    adapter.use(new EventDebuggerMiddleware());
    adapter.use(new AutoSaveStateMiddleware(conversationState, userState));
    return adapter;
}

module.exports = {
    getAllResponsesTemplates: getAllResponsesTemplates,
    getTestAdapterDefault: getTestAdapterDefault,
    templateManager: templateManager,
    testUserProfileState: testUserProfileState
}
