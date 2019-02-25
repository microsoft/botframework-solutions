const { AutoSaveStateMiddleware, ConversationState, MemoryStorage, TestAdapter, UserState } = require('botbuilder-core')
const BotConfiguration = require('botframework-config').BotConfiguration;
const config = require('dotenv').config;
const i18n = require('i18n');
const path = require('path');
const BotServices = require('../../lib/botServices.js').BotServices;
const <%= botNameClass %> = require('../../lib/<%= botNameFile %>.js').<%= botNameClass %>;

const TEST_MODE = require('../testBase').testMode;

const setupEnvironment = function (testMode) {
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
const initialize = function (testStorage) {
    i18n.configure({
        directory: path.join(__dirname, '..', '..', 'src', 'locales'),
        defaultLocale: 'en',
        objectNotation: true
    });

    setupEnvironment(TEST_MODE);

    const storage = testStorage || new MemoryStorage();
    const conversationState = new ConversationState(storage);
    const userState = new UserState(storage);

    const botConfiguration = BotConfiguration.loadSync(process.env.BOT_FILE_NAME, process.env.BOT_FILE_SECRET);
    const services = new BotServices(botConfiguration);

    this.bot = new <%= botNameClass %>(services, conversationState, userState);
}

/**
 * Initializes the TestAdapter.
 * @returns TestAdapter with the Bot logic configured.
 */
const getTestAdapter = function () {
    const bot = this.bot;

    return new TestAdapter(function (context) {
        i18n.setLocale(context.activity.locale || 'en');
        return bot.onTurn(context);
    })
    .use(new AutoSaveStateMiddleware(bot.conversationState, bot.userState));
}

module.exports = {
    initialize: initialize,
    getTestAdapter: getTestAdapter
}
