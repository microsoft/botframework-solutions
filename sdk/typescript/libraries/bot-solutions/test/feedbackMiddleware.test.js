/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { MemoryStorage, ConversationState, TestAdapter, NullTelemetryClient } = require("botbuilder");
const { FeedbackMiddleware, FeedbackOptions } = require("../lib/feedback");
const positiveFeedback = "positive";
const negativeFeedback = "negative";

xdescribe("feedback middleware", function() {
    it("should default options to positive", async function() {
        const storage = new MemoryStorage();
        const convState = new ConversationState(storage);

        const response = "Response";
        const tag = "Tag";

        const testAdapter = new TestAdapter(async function(context) {
            await context.sendActivity(response);
            await FeedbackMiddleware.requestFeedback(context, tag);

            // TODO save manually
            await convState.saveChanges(context, false);
        })
        .use(new FeedbackMiddleware(convState, new NullTelemetryClient()));

        await testAdapter.send("foo")
        .assertReply(response)
        .assertReply((activity) => {
            const card = activity.attachments[0].content;
            strictEqual(card.buttons.length, 3);
        })
        .send(positiveFeedback)
        .assertReply("Thanks for your feedback!")
        .startTest();
    });

    it("should default options to comment", async function() {
        const storage = new MemoryStorage();
        const convState = new ConversationState(storage);

        const response = "Response";
        const tag = "Tag";
        const feedbackOptions = new FeedbackOptions()
        feedbackOptions.commentsEnabled = true;

        const testAdapter = new TestAdapter(async function(context) {
            await context.sendActivity(response);
            await FeedbackMiddleware.requestFeedback(context, tag);

            // TODO save manually
            await convState.saveChanges(context, false);
        })
        .use(new FeedbackMiddleware(convState, new NullTelemetryClient(), feedbackOptions));

        await testAdapter.send("foo")
        .assertReply(response)
        .assertReply((activity) => {
            const card = activity.attachments[0].content;
            strictEqual(card.buttons.length, 3);
        })
        .send(negativeFeedback)
        .assertReply((activity) => {
            const card = activity.attachments[0].content;
            strictEqual(card.title, "Please add any additional comments in the chat.");
            strictEqual(card.buttons.length, 1);
        })
        .send("comment")
        .assertReply("Your comment has been received.")
        .startTest();
    });
});
