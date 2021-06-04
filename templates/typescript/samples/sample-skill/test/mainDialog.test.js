/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
const assert = require("assert");
const ActivityTypes = require("botbuilder").ActivityTypes;
const skillTestBase = require("./helpers/skillTestBase");
const testNock = require("./helpers/testBase");

describe("main dialog", function() {
    beforeEach(async function() {
        await skillTestBase.initialize();
    });

    describe("intro message", function() {
        it("send conversationUpdate and check the intro message is received", function(done) {
            const testAdapter = skillTestBase.getTestAdapter();
            const flow = testAdapter
                .send({
                    type: "conversationUpdate",
                    membersAdded: [
                        {
                            id: "1",
                            name: "user"
                        },
                        {
                            id: "2",
                            name: "bot"
                        }
                    ],
                    channelId: "emulator",
                    recipient: {
                        id: "1"
                    },
                    locale: "en-us"
                })
                .assertReply("Welcome to your custom skill!");

            testNock.resolveWithMocks("mainDialog_intro_response", done, flow);
        });
    });

    describe("help intent", function() {
        it("send 'help' and check you get the expected response", function(done) {
            const testAdapter = skillTestBase.getTestAdapter();
            const flow = testAdapter
                .send("help")
                .assertReply(function (activity) {
                    assert.strictEqual(1, activity.attachments.length);
                });

            testNock.resolveWithMocks("mainDialog_help_response", done, flow);
        });
    });

    describe("test unhandled message", function() {
        it("send 'Unhandled message' and check you get the expected response", function(done) {
            const allResponseVariations = skillTestBase.templateManager.lgPerLocale.get('en-us').expandTemplate("UnsupportedText", { name: '' });
            const testAdapter = skillTestBase.getTestAdapter();
            const flow = testAdapter
                .send("sample dialog")
                .assertReplyOneOf(skillTestBase.getTemplates('en-us','FirstPromptText'))
                .send("Unhandled message")
                .assertReplyOneOf(allResponseVariations);

            testNock.resolveWithMocks("mainDialog_unhandled_response", done, flow);
        });
    });
});
