/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const skillTestBase = require("./helpers/skillTestBase");
const testNock = require("./helpers/testBase");
const assert = require("assert");

describe("interruption", function() {
    beforeEach(async function() {
        await skillTestBase.initialize();
    });

    describe("help interruption", function() {
        it("send 'help' during the sample dialog", function(done) {
            const testAdapter = skillTestBase.getTestAdapter();
            const flow = testAdapter
                .send("run sample dialog")
                .assertReplyOneOf(skillTestBase.getTemplates('en-us','FirstPromptText'))
                .send("run sample dialog")
                .assertReplyOneOf(skillTestBase.getTemplates('en-us','NamePromptText'))
                .send("help")
                .assertReply(function (activity) {
                    assert.strictEqual(1, activity.attachments.length);
                })
                .assertReplyOneOf(skillTestBase.getTemplates('en-us','NamePromptText'));
                testNock.resolveWithMocks("interruption_help_response", done, flow);
        });
    });

    describe("cancel interruption", function() {
        it("send 'cancel' during the sample dialog", function(done) {
            const testAdapter = skillTestBase.getTestAdapter();
            const flow = testAdapter
                .send("run sample dialog")
                .assertReplyOneOf(skillTestBase.getTemplates('en-us','FirstPromptText'))
                .send("run sample dialog")
                .assertReplyOneOf(skillTestBase.getTemplates('en-us','NamePromptText'))
                .send("cancel")
                .assertReplyOneOf(skillTestBase.getTemplates('en-us','CancelledText'));
            testNock.resolveWithMocks("interruption_cancel_response", done, flow);
        });
    });
});
