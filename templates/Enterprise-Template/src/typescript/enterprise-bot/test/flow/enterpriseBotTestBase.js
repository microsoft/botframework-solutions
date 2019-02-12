const { ConversationState, MemoryStorage, TestAdapter, UserState } = require('botbuilder-core')
const BotConfiguration = require('botframework-config').BotConfiguration;
const config = require('dotenv').config;
const i18n = require('i18n');
const path = require('path');
const TEST_MODE = require('../testBase').testMode;
const BotServices = require('../../lib/botServices.js').BotServices;
const EnterpriseBot = require('../../lib/enterpriseBot.js').EnterpriseBot;
const ENV_NAME = 'development';

    /**
     * Initializes the properties for the bot to be tested.
     */
    var initialize = async function(testStorage) {
        i18n.configure({
            directory: path.join(__dirname, '..', '..', 'src', 'locales'),
            defaultLocale: 'en',
            objectNotation: true
        });

        /**
         * This will be removed when the mocks are finished, since the env variables will be hardcoded
         */
        config({ path: path.join(__dirname, '..', '..', `.env.${ENV_NAME}`) });
        this.conversationState = new ConversationState(testStorage || new MemoryStorage());
        this.userState = new UserState(testStorage || new MemoryStorage());
        const telemetryClient = new NullTelemetryClient();

        /*
         * Here we should pass an instance of { BotServices } from '../../src/botServices',
         * for the moment we'll deploy a bot and use the .bot file, until we mock the services.
        */
        const botConfiguration = await BotConfiguration.load(process.env.BOT_FILE_NAME, process.env.BOT_FILE_SECRET);
        this.services = new BotServices(botConfiguration);
    }

const setupEnvironment = function(testMode) {
    switch (testMode) {
        case 'record':
            config({ path: path.join(__dirname, '..', '..', '.env.development') });
            break;
        case 'lockdown':
            config({ path: path.join(__dirname, '..', '.env.test') });
            break;
    }
}

/**
 * Initializes the properties for the bot to be tested.
 */
const initialize = function () {
    i18n.configure({
        directory: path.join(__dirname, '..', '..', 'src', 'locales'),
        defaultLocale: 'en',
        objectNotation: true
    });

    setupEnvironment(TEST_MODE);

    const storage = new MemoryStorage();
    const conversationState = new ConversationState(storage);
    const userState = new UserState(storage);

    const botConfiguration = BotConfiguration.loadSync(process.env.BOT_FILE_NAME, process.env.BOT_FILE_SECRET);
    const services = new BotServices(botConfiguration);

    this.bot = new EnterpriseBot(services, conversationState, userState);
}

/**
 * Initializes the TestAdapter.
 * @returns TestAdapter with the Bot logic configured.
 */
const getTestAdapter = function () {
    const bot = this.bot;

    return new TestAdapter(function(context) {
        return bot.onTurn(context);
    });
}

module.exports = {
    initialize: initialize,
    getTestAdapter: getTestAdapter
}
