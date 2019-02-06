const { ConversationState, MemoryStorage, NullTelemetryClient, TestAdapter, UserState} = require('botbuilder-core')
const { BotConfiguration } = require('botframework-config');
const { config } = require('dotenv');
const i18n = require('i18n');
const path = require('path');
const { BotServices } = require('../../lib/botServices.js');
const { EnterpriseBot } = require('../../lib/enterpriseBot.js');
const ENV_NAME = 'development';

    /**
     * Initializes the properties for the bot to be tested.
     */
    var initialize = async function() {
        i18n.configure({
            directory: path.join(__dirname, '..', '..', 'src', 'locales'),
            defaultLocale: 'en',
            objectNotation: true
        });

        /**
         * This will be removed when the mocks are finished, since the env variables will be hardcoded
         */
        config({ path: path.join(__dirname, '..', '..', `.env.${ENV_NAME}`) });
        this.conversationState = new ConversationState(new MemoryStorage());
        this.userState = new UserState(new MemoryStorage());
        const telemetryClient = new NullTelemetryClient();

        /*
         * Here we should pass an instance of { BotServices } from '../../src/botServices',
         * for the moment we'll deploy a bot and use the .bot file, until we mock the services.
        */
        const botConfiguration = await BotConfiguration.load(process.env.BOT_FILE_NAME, process.env.BOT_FILE_SECRET);
        this.services = new BotServices(botConfiguration);
    }

    /**
     * Initializes the TestAdapter.
     * @returns TestAdapter with the Bot logic configured.
     */
    var getTestAdapter = function() {
        const adapter = new TestAdapter((context) =>{
            var bot = new EnterpriseBot(this.services, this.conversationState, this.userState);
            return bot.onTurn(context);
        });

        return adapter;
    }

module.exports = {
  initialize: initialize,
  getTestAdapter: getTestAdapter
}
