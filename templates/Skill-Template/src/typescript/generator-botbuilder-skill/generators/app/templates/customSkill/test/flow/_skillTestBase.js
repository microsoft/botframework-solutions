// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { TelemetryClient } = require('applicationinsights');
const { AutoSaveStateMiddleware, ConversationState, MemoryStorage, TestAdapter, UserState } = require('botbuilder-core');
const { BotConfiguration, ServiceTypes } = require('botframework-config');
const config = require('dotenv').config;
const i18next = require('i18next');
const i18nextNodeFsBackend = require('i18next-node-fs-backend');
const path = require('path');
const <%=skillTemplateName%> = require('../../lib/<%=skillTemplateNameFile%>').<%=skillTemplateName%>;
const { Locales, SkillConfiguration, SkillDefinition } = require('botbuilder-solutions');
const TEST_MODE = require('../testBase').testMode;
let languageModelsRaw;
let skillsRaw;

const configuration = async function() {
    // Configure internationalization and default locale
    await i18next.use(i18nextNodeFsBackend)
    .init({
        fallbackLng: 'en',
        preload: [ 'de', 'en', 'es', 'fr', 'it', 'zh' ],
        backend: {
            loadPath: path.join(__dirname, '..', '..', 'src', 'locales', '{{lng}}.json')
        }
    });

    await Locales.addResourcesFromPath(i18next, 'common');


    setupEnvironment(TEST_MODE);
}

const setupEnvironment = function (testMode) {
    switch (testMode) {
        case 'record':
            config({ path: path.join(__dirname, '..', '..', '.env.development') });
            languageModelsRaw = require('../../src/languageModels.json'); 
            skillsRaw = require('../../src/skills.json');
            break;
        case 'lockdown':
            config({ path: path.join(__dirname, '..', '.env.test') });
            languageModelsRaw = require('../mockResources/languageModels.json');
            skillsRaw = require('../mockResources/skills.json');
            break;
    }
}

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

/**
 * Initializes the properties for the bot to be tested.
 */
const initialize = async function(testStorage) {
    await configuration();
    
    const storage = testStorage || new MemoryStorage();
    const botConfiguration = BotConfiguration.loadSync(process.env.BOT_FILE_NAME, process.env.BOT_FILE_SECRET);
    // Initializes your bot language models and skills definitions
    const languageModels = new Map(Object.entries(languageModelsRaw));
    const skills = skillsRaw.map((skill) => {
        const result = Object.assign(new SkillDefinition(), skill);
        result.configuration = new Map(Object.entries(skill.configuration || {}));
        return result;
    });
    const skillMode = false
    const services = new SkillConfiguration(
        botConfiguration,
        languageModels,
        skills[0].supportedProviders,
        skills[0].parameters,
        skills[0].configuration);
    const APPINSIGHTS_NAME = process.env.APPINSIGHTS_NAME || '';
    const conversationState = new ConversationState(storage);
    const userState = new UserState(storage);
    // Get AppInsights configuration by service name
    const appInsightsConfig = searchService(botConfiguration, ServiceTypes.AppInsights, APPINSIGHTS_NAME);
    const telemetryClient = new TelemetryClient(appInsightsConfig.instrumentationKey);
    this.bot = new <%=skillTemplateName%>(services, conversationState, userState, telemetryClient, skillMode);
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

module.exports = {
    configuration: configuration,
    initialize: initialize,
    getTestAdapter: getTestAdapter
}