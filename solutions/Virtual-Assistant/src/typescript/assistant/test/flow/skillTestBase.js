// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

const assistantBot = require('./assistantTestBase');
const BotConfiguration = require('botframework-config').BotConfiguration;
const BotServices = require('../../lib/botServices.js').BotServices;
/**
 * Initializes the properties for the bot to be tested.
 */
const initialize = function(testStorage) {
    assistantBot.configuration(testStorage);
    
    const botConfiguration =  BotConfiguration.loadSync(process.env.BOT_FILE_NAME, process.env.BOT_FILE_SECRET);
    const services = new BotServices(botConfiguration);
    //services.localeConfigurations.set();
     
}
