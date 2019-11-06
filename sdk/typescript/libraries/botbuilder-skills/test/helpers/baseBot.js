/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const {
    ActivityHandler } = require('botbuilder');
const { DialogSet } = require('botbuilder-dialogs');   
const { SkillContext } = require('../../lib/skillContext');

class BaseBot extends ActivityHandler {
    constructor(skillContextAccessor, conversationState, dialogStateAccessor) {
        super();
        this.conversationState = conversationState;
        this.skillContextAccessor = skillContextAccessor;
        this.dialogs = new DialogSet(dialogStateAccessor)
        this.onTurn(this.turn.bind(this));
    }

    async turn (turnContext) {
        const dc = await this.dialogs.createContext(turnContext);
        const userState = await this.skillContextAccessor.get(dc.context, new SkillContext()); 
        
        // If we have skillContext data to populate
        if(this.slots !== undefined) {
            // Add state to the SKillContext
            this.slots.forEach(slot => {
                userState[slot.key] = slot.value;
            });
        }

        if(dc.activeDialog !== undefined) {
            const result = await dc.continueDialog();
        } else {
            // actionId lets the SkillDialog know which action to call
            await dc.beginDialog(this.skillManifest.id, this.actionId);

            // We don't continue as we don't care about the message being sent
            // just the initial instantiation, we need to send a message within tests
            // to invoke the flow. If continue is called then HttpMocks need be updated
            // to handle the subsequent activity "ack"
            // var result = await dc.ContinueDialogAsync();
        }
    }
}

exports.BaseBot = BaseBot;