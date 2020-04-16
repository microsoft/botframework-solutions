/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License
 */

const assert = require('assert');
const { getTestAdapterDefault, templateEngine, testUserProfileState } = require('./helpers/botTestBase');
const { MemoryStorage } = require('botbuilder-core')
const testNock = require("./helpers/testBase");
let testStorage = new MemoryStorage();

describe("Interruption", function() {
    describe("help interruption", function() {
        beforeEach(function(done) {
            testStorage = new MemoryStorage();
            done();
        });

        it("send help and check that there is a attachment", function(done) {
            getTestAdapterDefault({ storage: testStorage }).then((testAdapter) => {
                const flow = testAdapter
                .send("Help")
                .assertReply((activity, description) => {
                    assert.strictEqual(1, activity.attachments.length)
                })

                return testNock.resolveWithMocks("interruption_help_response", done, flow);
            });
        });

        it("send help and check that there is a attachment of the response file", function(done) {
            const allNamePromptVariations = templateEngine.templateEnginesPerLocale.get("en-us").expandTemplate("NamePrompt");

            getTestAdapterDefault({ storage: testStorage }).then((testAdapter) => {
                const flow = testAdapter
                .send({
                    type: "conversationUpdate",
                    membersAdded: [
                        {
                            id: "1",
                            name: "user"
                        }
                    ],
                })
                .assertReply((activity, description) => {
                    assert.strictEqual(1, activity.attachments.length)
                })
                .assertReplyOneOf(allNamePromptVariations)
                .send("Help")
                .assertReply((activity, description) => {
                    assert.strictEqual(1, activity.attachments.length)
                })
                .assertReplyOneOf(allNamePromptVariations)

                return testNock.resolveWithMocks("interruption_help_in_dialog_response", done, flow);
            });
        });
    });

    describe ("cancel interruption", function(done) {
        it("send cancel and check the response is one of the file", function(done) {
            const allResponseVariations = templateEngine.templateEnginesPerLocale.get("en-us").expandTemplate("CancelledMessage", testUserProfileState);

            getTestAdapterDefault().then((testAdapter) => {
                const flow = testAdapter
                    .send("Cancel")
                    .assertReplyOneOf(allResponseVariations)

                return testNock.resolveWithMocks("interruption_cancel_response", done, flow);
            });
        });

        it("send cancel during a flow and check the response is one of the file", function(done) {
            const allNamePromptVariations = templateEngine.templateEnginesPerLocale.get("en-us").expandTemplate("NamePrompt");
            const allCancelledVariations = templateEngine.templateEnginesPerLocale.get("en-us").expandTemplate("CancelledMessage", testUserProfileState);

            getTestAdapterDefault().then((testAdapter) => {
                const flow = testAdapter
                    .send({
                        type: "conversationUpdate",
                        membersAdded: [
                            {
                                id: "1",
                                name: "user"
                            }
                        ],
                    })
                    .assertReply((activity, description) => {
                        assert.strictEqual(1, activity.attachments.length)
                    })
                    .assertReplyOneOf(allNamePromptVariations)
                    .send("Cancel")
                    .assertReplyOneOf(allCancelledVariations)
                return testNock.resolveWithMocks("interruption_confirm_cancel_response", done, flow);
            });
        });

        it("send repeat during a flow and check the response is one of the file", function(done) {
            const allNamePromptVariations = templateEngine.templateEnginesPerLocale.get("en-us").expandTemplate("NamePrompt");

            getTestAdapterDefault().then((testAdapter) => {
                const flow = testAdapter
                    .send('')
                    .assertReplyOneOf(allNamePromptVariations)
                    .send("Repeat")
                    .assertReplyOneOf(allNamePromptVariations)
                return testNock.resolveWithMocks("interruption_repeat_response", done, flow);
            });
        });
    });
});
