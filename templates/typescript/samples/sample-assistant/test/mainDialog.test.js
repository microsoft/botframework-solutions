/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License
 */

const assert = require('assert');
const testNock = require('./helpers/testBase');
const { getTestAdapterDefault, testUserProfileState, getAllResponsesTemplates } = require('./helpers/botTestBase');

describe("Main Dialog", function () {
	describe("intro card", function() {
		it("test intro message", function(done) {
			getTestAdapterDefault().then((testAdapter) => {
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
				.assertReply(function (activity, description) {
					assert.strictEqual(1, activity.attachments.length);
				});

				return testNock.resolveWithMocks('mainDialog_introCard_response', done, flow);
			});
		});
	});

	describe("help", function () {
		it("test help intent", function (done) {
			getTestAdapterDefault().then((testAdapter) => {
				const flow = testAdapter
					.send('Help')
					.assertReply(function (activity, description) {
						assert.strictEqual(1, activity.attachments.length);
					});

					return testNock.resolveWithMocks('mainDialog_help_response', done, flow);
			});
		});
	});

	describe("escalating", function () {
        it("test escalate intent", function (done) {
            getTestAdapterDefault().then((testAdapter) => {
				const flow = testAdapter
					.send('I want to talk to a human')	
					.assertReply(function (activity, description) {
						assert.strictEqual(1, activity.attachments.length);
					});

				return testNock.resolveWithMocks('mainDialog_escalate_response', done, flow);
			});
        });
    });
	
	/*
	ChitChat is the default fallback which will not be configured at functional test time so a mock ensures QnAMaker returns no answer
	enabling the unsupported message to be returned.
	*/
    xdescribe("confused", function () {
        it("test unhandled message", function (done) {
			const allFirstPromptVaritions = getAllResponsesTemplates("en-us").expandTemplate("FirstPromptMessage");
			const allResponseVariations = getAllResponsesTemplates("en-us").expandTemplate("UnsupportedMessage", testUserProfileState);

			getTestAdapterDefault().then((testAdapter) => {
				const flow = testAdapter
					.send('')
					.assertReplyOneOf(allFirstPromptVaritions)
					.send("Unhandled message")
					.assertReplyOneOf(allResponseVariations)      
				
				return testNock.resolveWithMocks('mainDialog_unhandled_response', done, flow);
            });
        });
    });
});
