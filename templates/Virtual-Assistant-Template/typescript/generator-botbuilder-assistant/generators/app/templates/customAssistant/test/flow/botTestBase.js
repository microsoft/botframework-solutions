// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

const { TelemetryClient } = require('applicationinsights');
const { BotFrameworkAdapterSettings } = require ('botbuilder');
const { MicrosoftAppCredentialsEx, ISkillManifest } = require ('botbuilder-skills');
const { AutoSaveStateMiddleware, ConversationState, MemoryStorage, TestAdapter, UserState } = require('botbuilder-core');
const { BotConfiguration, ServiceTypes } = require('botframework-config');
const config = require('dotenv').config;
const i18next = require('i18next');
const i18nextNodeFsBackend = require('i18next-node-fs-backend');
const path = require('path');
const botServices = require('../../lib/services/botServices.js').BotServices;
const botSettings= require('../../lib/services/botSettings.js');
const { DialogBot } = require('../../lib/bots/dialogBot.js');
const { OnboardingDialog } = require('../../lib/dialog/onbordingDialog.js')
const appsettings = require('../appsettings.json');
const cognitiveModelsRaw = require ('../cognitivemodels.json');
const skillsRaw = require ('../../../sample-assistant/src/skills.json');
const { Locales, ProactiveState, SkillDefinition } = require('botbuilder-solutions');
const TEST_MODE = require('../testBase').testMode;

const setupEnvironment = function (testMode) {
    switch (testMode) {
        case 'record':
            config({ path: path.join(__dirname, '..', '..', 'appsettings.json') });
            //skillsRaw = require('../../../sample-assistant/src/skills.json'); 
            break;
        case 'lockdown':
            config({ path: path.join(__dirname, '..', 'appsettings.json') });
            //skillsRaw = require('../../../sample-assistant/src/skills.json'); 
            break;
    }
}

const configuration = async function() {
    // Configure internationalization and default locale
    await i18next.use(i18nextNodeFsBackend)
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

    setupEnvironment(TEST_MODE);
}
const appId = botSettings.microsoftAppId
const appPassword = botSettings.microsoftAppPassword

const searchService = function(botConfiguration, serviceType, nameOrId) {
    const candidates = botConfiguration.services
        .filter((s) => !serviceType || s.type === serviceType);
    const service = candidates.find((s) => s.id === nameOrId || s.name === nameOrId)
        || candidates.find((s) => true);
    if (!service && nameOrId) {
        throw new Error(`Service '${nameOrId}' [type: ${serviceType}] not found in .bot file.`);
    }
    return service;
}

const cognitiveModels = new Map(Object.entries(cognitiveModelsRaw));
const cognitiveModelDictionary = cognitiveModelsRaw.cognitiveModels;
const cognitiveModelMap = new Map(Object.entries(cognitiveModelDictionary));
cognitiveModelMap.forEach((value = Object, key = string) => {
    cognitiveModels.set(key, value);
});

/**
 * Initializes the properties for the bot to be tested.
 */
const initialize = async function(testStorage) {
    await configuration();
    
    const botSettings = new BotSettings(appsettings.appInsights, appsettings.blobStorage, cognitiveModels, appsettings.cosmosDb, cognitiveModels.defaultLocale, appsettings.microsoftAppId, appsettings.microsoftAppPassword, skills)
    const botServices = new BotServices(botSettings);
    const onboardingDialog = new OnboardingDialog(botServices, onboardingStateAccessor, telemetryClient)

    const mainDialog = new MainDialog(
        botSettings,
        botServices,
        onboardingDialog,
        escalateDialog,
        skillDialogs,
        onboardingStateAccessor,
        adapter.telemetryClient
    );
    this.bot = new DialogBot(conversationState, telemetryClient, mainDialog);
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

// PENDING initialize skills

module.exports = {
    configuration: configuration,
    initialize: initialize,
    getTestAdapter: getTestAdapter
}