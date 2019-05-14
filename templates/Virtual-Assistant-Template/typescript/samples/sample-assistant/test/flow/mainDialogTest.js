/**
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License
 */

const assert = require('assert');
const botTestBase = require('./botTestBase.js');
const testNock = require('../testBase');
const introJson = require('../../src/content/NewUserGreeting.json');
const introJsonEs = require('../../src/content/NewUserGreeting.es.json');

describe("Main Dialog", function () {
	describe("Intro Card", function() {
		it("Send conversationUpdate and verify card is received", function(done) {
			botTestBase.getTestAdapterDefault().then((testAdapter) => {
				const flow = testAdapter
				.send({
					type: "conversationUpdate",
					membersAdded: [
						{
							id: "1",
							name: "Bot"
						}
					],
					channelId: "emulator",
					recipient: {
						id: "1"
					}
				})
				.assertReply(function (activity, description) {
					assert.equal(activity.attachments[0].contentType, 'application/vnd.microsoft.card.adaptive');
					assert.deepEqual(activity.attachments[0].content, introJson);
				});

				return testNock.resolveWithMocks('mainDialog_introCard_response', done, flow);
			});
		});
	});

	describe("Greeting", function () {
		it("Send Hello and check you get the expected response", function (done) {
			botTestBase.getTestAdapterDefault().then((testAdapter) => {
				const flow = testAdapter
					.send('Hi')
					.assertReply('Hello.');

				testNock.resolveWithMocks('mainDialog_greeting_response', done, flow);
			});
		});
	});

	describe("Help", function () {
		it("Send Help and check you get the expected response", function (done) {
			botTestBase.getTestAdapterDefault().then((testAdapter) => {
				const flow = testAdapter
					.send('Help')
					.assertReply('I\'m your Virtual Assistant! I can perform a number of tasks through my connected skills. Right now I can help you with Calendar, Email, Task and Point of Interest questions. Or you can help me do more by creating your own!');

				testNock.resolveWithMocks('mainDialog_help_response', done, flow);
			});
		});
	});

	describe("Escalating", function () {
        it("Send 'I want to talk to a human' and check you get the expected response", function (done) {
            botTestBase.getTestAdapterDefault().then((testAdapter) => {
				const flow = testAdapter
					.send('I want to talk to a human')	
					.assertReply(function (activity, description) {
						assert.equal(activity.attachments[0].contentType, 'application/vnd.microsoft.card.hero');
						assert.deepEqual(activity.attachments[0].content.title, 'Our agents are available 24/7 at 1(800)555-1234. Or connect with us through Microsoft Teams.');
					});

				testNock.resolveWithMocks('mainDialog_escalate_response', done, flow);
			});
        });
    });

	describe("Localization", function () {
		it("Send a message in spanish, set locale property on activity and validate the localized response", function (done) {
			botTestBase.getTestAdapterDefault().then((testAdapter) => {
				const flow = testAdapter
				.send({
					type: "conversationUpdate",
					membersAdded: [
						{
							id: "1",
							name: "Bot"
						}
					],
					channelId: "emulator",
					recipient: {
						id: "1"
					},
					locale: 'es-es'
				})
				.assertReply(function (activity, description) {
					assert.equal(activity.attachments[0].contentType, 'application/vnd.microsoft.card.adaptive');
					assert.deepEqual(activity.attachments[0].content, introJsonEs);
				});

				return testNock.resolveWithMocks('mainDialog_localization_response', done, flow);
			});
		});
	});
});
