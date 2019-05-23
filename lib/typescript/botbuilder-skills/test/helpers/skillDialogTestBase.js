/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
const { AutoSaveStateMiddleware } = require('botbuilder');
const { TestAdapter } = require("botbuilder-core");
const { ActivityTypes } = require("botframework-schema");
const { BaseBot } = require('../helpers/baseBot');

/**
 * Initializes the properties for the skilLDialog to be tested.
 */
const initialize = async function(userState, conversationState, skillContextAccessor, dialogStateAccessor) {
    this.userState = userState;
    this.conversationState = conversationState;
    this.bot = new BaseBot(skillContextAccessor, this.conversationState, dialogStateAccessor);   
}

/**
 * Initializes the TestAdapter.
 * @returns {TestAdapter} with the Bot logic configured.
 */
const getTestAdapter = function(skillManifest, dialogs, actionId, slots) {
    
    const baseBot = this.bot;
    if (dialogs !== undefined){
        dialogs.forEach(dialog => {
            baseBot.dialogs.add(dialog);
        });
    }
    baseBot.skillManifest = skillManifest;
    baseBot.actionId = actionId;
    baseBot.slots = slots;
    const adapter = new TestAdapter(baseBot.run.bind(baseBot));

    adapter.onTurnError = async function(context, error) {
      await context.sendActivity({
        type: ActivityTypes.Trace,
        text: error.message
      });
      await context.sendActivity({
        type: ActivityTypes.Trace,
        text: error.stack
      });
    };
    
    adapter.use(new AutoSaveStateMiddleware(this.conversationState, this.userState));
    return adapter;
}

module.exports = {
    initialize: initialize,
    getTestAdapter: getTestAdapter
};