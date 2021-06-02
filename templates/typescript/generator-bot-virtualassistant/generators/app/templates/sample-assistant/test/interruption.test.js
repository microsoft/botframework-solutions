/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License
 */

const assert = require('assert');
const { getAllResponsesTemplates, getTestAdapterDefault, testUserProfileState } = require('./helpers/botTestBase');
const { MemoryStorage } = require('botbuilder-core')
const testNock = require("./helpers/testBase");
let testStorage = new MemoryStorage();

describe("Interruption", function() {
    describe("help interruption", function() {
        beforeEach(function(done) {
            testStorage = new MemoryStorage();
            done();
        });

        it("send help and check that there is an attachment", function(done) {
            getTestAdapterDefault({ storage: testStorage }).then((testAdapter) => {
                const flow = testAdapter
                .send("Help")
                .assertReply((activity, description) => {
                    assert.strictEqual(1, activity.attachments.length)
                })

                return testNock.resolveWithMocks("interruption_help_response", done, flow);
            });
        });

        it("send help and check that there is an attachment of the response file", function(done) {
            const allNamePromptVariations = getAllResponsesTemplates("en-us").expandTemplate("NamePrompt");

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
        // "the LG template 'UnsupportedMessage' has randomly generated response which makes this test unreliable"
        xit("send cancel during a flow and check the response is one of the file", function(done) {
            const allNamePromptVariations = getAllResponsesTemplates("en-us").expandTemplate("NamePrompt");
            const allCancelledVariations = getAllResponsesTemplates("en-us").expandTemplate("CancelledMessage", testUserProfileState);

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
            const allNamePromptVariations = getAllResponsesTemplates("en-us").expandTemplate("NamePrompt");

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