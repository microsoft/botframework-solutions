// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

const { TelemetryClient } = require('applicationinsights');
const { AutoSaveStateMiddleware, ConversationState, MemoryStorage, TestAdapter, UserState } = require('botbuilder-core');
const { BotConfiguration, ServiceTypes } = require('botframework-config');
const config = require('dotenv').config;
const i18n = require('i18n');
const path = require('path');
const BotServices = require('../../lib/botServices.js').BotServices;
const VirtualAssistant = require('../../lib/virtualAssistant.js').VirtualAssistant;
let languageModelsRaw;
let skillsRaw;
const { ProactiveState, SkillDefinition } = require('bot-solution');
const TEST_MODE = require('../testBase').testMode;

const setupEnvironment = function (testMode) {
    switch (testMode) {
        case 'record':
            config({ path: path.join(__dirname, '..', '..', '.env.development') });
            languageModelsRaw = require('../../../assistant/src/languageModels.json'); 
            skillsRaw = require('../../../assistant/src/skills.json');
            break;
        case 'lockdown':
            config({ path: path.join(__dirname, '..', '.env.test') });
            languageModelsRaw = require('../mockResources/languageModels.json');
            skillsRaw = require('../mockResources/skills.json');
            break;
    }
}

const configuration = function() {
    i18n.configure({
        directory: path.join(__dirname, '..', '..', 'src', 'locales'),
        defaultLocale: 'en',
        objectNotation: true
    });

    setupEnvironment(TEST_MODE);
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
const initialize = function(testStorage) {
    configuration();
    
    const storage = testStorage || new MemoryStorage();
    
    const botConfiguration = BotConfiguration.loadSync(process.env.BOT_FILE_NAME, process.env.BOT_FILE_SECRET);
    // Initializes your bot language models and skills definitions
    const languageModels = new Map(Object.entries(languageModelsRaw));
    const skills = skillsRaw.map((skill) => {
        const result = Object.assign(new SkillDefinition(), skill);
        result.configuration = new Map(Object.entries(skill.configuration || {}));
        return result;
    });
    const services = new BotServices(botConfiguration, languageModels, skills);
    const APPINSIGHTS_NAME = process.env.APPINSIGHTS_NAME || '';
    const conversationState = new ConversationState(storage);
    const userState = new UserState(storage);
    const BOT_CONFIGURATION = (process.env.ENDPOINT || 'development');
    // Get bot endpoint configuration by service name
    const endpointService = searchService(botConfiguration, ServiceTypes.Endpoint, BOT_CONFIGURATION);
    // Get AppInsights configuration by service name
    const appInsightsConfig = searchService(botConfiguration, ServiceTypes.AppInsights, APPINSIGHTS_NAME);
    const telemetryClient = new TelemetryClient(appInsightsConfig.instrumentationKey);
    const proactiveState = new ProactiveState(storage);
    this.bot = new VirtualAssistant(services, conversationState, userState, proactiveState ,endpointService, telemetryClient);

    // PENDING authentication and skill registration
}

/**
 * Initializes the TestAdapter.
 * @returns TestAdapter with the Bot logic configured.
 */
const getTestAdapter = function() {
    const bot = this.bot;

    return new TestAdapter(function (context) {
        i18n.setLocale(context.activity.locale || 'en');
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