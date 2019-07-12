/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { deepStrictEqual, strictEqual } = require("assert");
const { ConversationState, MemoryStorage, ActivityTypes } = require("botbuilder");  
const { TestAdapter } = require("botbuilder-core");
const { SkillMiddleware } = require("../lib/skillMiddleware");
const { SkillEvents } = require("../lib/models/skillEvents");

const storage = new MemoryStorage();
const conversationState = new ConversationState(storage); 
const dialogStateAccessor = conversationState.createProperty("DialogState");

// Test basic invocation of Skills that have slots configured and ensure the slots are filled as expected.
describe("skill middleware", function() {
    it("should cancel all skill dialogs", async function() {
        let afterEvent = false;
        let index = 0;
        const cancelEvent = {
            type: ActivityTypes.Event,
            name: SkillEvents.cancelAllSkillDialogsEventName,
            text: ActivityTypes.Event
        };

        const testAdapter = new TestAdapter(async function(context) {
            const actual = await dialogStateAccessor.get(context, { dialogStack: [] });
            if (afterEvent) {
                deepStrictEqual(actual, { dialogStack: [] });
                afterEvent = false;
            } else {
                strictEqual(actual.dialogStack.length, index);
                index = index + 1;
            }
            
            actual.dialogStack.push(`Element ${actual.dialogStack.length}`)
            await dialogStateAccessor.set(context, actual);
            await conversationState.saveChanges(context, true);
            await context.sendActivity(context.activity.text);
        })
        .use(async function(context, next) {
            // check if the message is an event
            if (context.activity.type === ActivityTypes.Event) {
                afterEvent = true;
                index = 1;
            }

            return next();
        })
        .use(new SkillMiddleware(conversationState, dialogStateAccessor));

        await testAdapter.test('hello', 'hello')
            .test('test', 'test')
            .test('test', 'test')
            .send(cancelEvent)
            .test('test', 'test')
            .test('bye', 'bye');
    });
});
