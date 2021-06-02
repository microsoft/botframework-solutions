/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
const { MemoryStorage, TestAdapter } = require("botbuilder");
const { ProactiveModel, ProactiveState, ProactiveStateMiddleware } = require("../../lib/proactive");
const { ActivityTypes } = require("botframework-schema");
const { ActivityEx } = require("../../lib/extensions/activityEx");
const { MD5Util } = require("../../lib/util");

xdescribe("proactive", function() {
    it("default options", async function() {
        const microsoftAppId = "";

        const storage = new MemoryStorage();
        const state = new ProactiveState(storage);
        const proactiveStateAccessor = state.createProperty(ProactiveModel.name);

        const testAdapter = new TestAdapter(async function(context) {
            if (context.activity.type === ActivityTypes.Event) {
                const proactiveModel = await proactiveStateAccessor.get(context);

                var hashedUserId = MD5Util.computeHash(context.activity.value);
                const conversationReference = proactiveModel[hashedUserId].conversation;
                
                await context.adapter.continueConversation(conversationReference, continueConversationCallback(context, context.activity.text));
            } else {
                await context.sendActivity(ActivityEx.createReply(context.activity, response));
            }
        })
        .use(new ProactiveStateMiddleware(state, proactiveStateAccessor));

        const response = "Response";
        const proactiveResponse = "ProactiveResponse";
        const proactiveEvent = {
            type: ActivityTypes.Event,
            value: "user1",
            text: proactiveResponse,
            from: {
                id: "user1",
                name: "user1",
                role: "user"
            }
        }

        await testAdapter.send("foo")
        .assertReply(response)
        .send(proactiveEvent)
        .assertReply(proactiveResponse)
        .startTest();
    })

    function continueConversationCallback(context, message) {
        return async(turnContext) => {
            const activity = turnContext.activity.createReply(message);
            await turnContext.sendActivity(activity);
        }
    }
});