/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License
 */

const assert = require('assert');
const testNock = require('./helpers/testBase');
const { getTestAdapterDefault, templateEngine, testUserProfileState } = require('./helpers/botTestBase');

describe("Main Dialog", function () {
	describe("intro card", function() {
		it("send a 'conversationUpdate' and verify intro message card is received", function(done) {
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
				.assertReply(function (activity, description) {
					assert.strictEqual(1, activity.attachments.length);
				});

				return testNock.resolveWithMocks('mainDialog_introCard_response', done, flow);
			});
		});
	});

	describe("help", function () {
		it("send 'Help' and verify help message card is received", function (done) {
			getTestAdapterDefault().then((testAdapter) => {
				const flow = testAdapter
					.send('Help')
					.assertReply(function (activity, description) {
						assert.strictEqual(1, activity.attachments.length);
					});

				testNock.resolveWithMocks('mainDialog_help_response', done, flow);
			});
		});
	});

	describe("escalating", function () {
        it("send 'I want to talk to a human' and check you get the expected response", function (done) {
            getTestAdapterDefault().then((testAdapter) => {
				const flow = testAdapter
					.send('I want to talk to a human')	
					.assertReply(function (activity, description) {
						assert.strictEqual(1, activity.attachments.length);
					});

				testNock.resolveWithMocks('mainDialog_escalate_response', done, flow);
			});
        });
    });
	
    xdescribe("confused", function () {
        it("send an unhandled message", function (done) {
			const allResponseVariations = templateEngine.templateEnginesPerLocale.get('en-us').expandTemplate("UnsupportedMessage", testUserProfileState);

			getTestAdapterDefault().then((testAdapter) => {
				const flow = testAdapter
                .send('Unhandled message')
                .assertReplyOneOf(allResponseVariations);
                    
                testNock.resolveWithMocks('mainDialog_unhandled_response', done, flow);
            });
        });
    });
});
