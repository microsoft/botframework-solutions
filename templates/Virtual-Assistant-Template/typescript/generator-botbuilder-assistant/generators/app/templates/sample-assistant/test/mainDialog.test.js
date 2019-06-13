/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License
 */

const assert = require('assert');
const botTestBase = require('./helpers/botTestBase');
const testNock = require('./helpers/testBase');
const introJson = require('../src/content/NewUserGreeting.json');
const introJsonEs = require('../src/content/NewUserGreeting.es.json');

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
					assert.strictEqual(activity.attachments[0].contentType, 'application/vnd.microsoft.card.adaptive');
					assert.deepStrictEqual(activity.attachments[0].content, introJson);
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
						assert.strictEqual(activity.attachments[0].contentType, 'application/vnd.microsoft.card.hero');
						assert.deepStrictEqual(activity.attachments[0].content.title, 'Our agents are available 24/7 at 1(800)555-1234. Or connect with us through Microsoft Teams.');
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
					assert.strictEqual(activity.attachments[0].contentType, 'application/vnd.microsoft.card.adaptive');
					assert.deepStrictEqual(activity.attachments[0].content, introJsonEs);
				});

				return testNock.resolveWithMocks('mainDialog_localization_response', done, flow);
			});
		});
	});
	
    describe("Confused", function () {
        it("Send an unhandled message", function (done) {
            botTestBase.getTestAdapterDefault().then((testAdapter) => {
                const flow = testAdapter
                    .send('Unhandled message')
                    .assertReply("I'm sorry, I'm not able to help with that.");
                    
                testNock.resolveWithMocks('mainDialog_unhandled_response', done, flow);
            });
        });
    });

    describe("FAQ", function () {
        it("Send a message with faq responses", function (done) {
            botTestBase.getTestAdapterDefault().then((testAdapter) => {
                const flow = testAdapter
                    .send('What is a Virtual Assistant?')
                    .assertReply("We have seen significant need from our customers and partners to deliver a conversational assistant tailored to their brand, personalized to their customers and made available across a broad range of conversational canvases and devices. Continuing Microsoft open-sourced approach toward Bot Framework SDK, the open source Virtual Assistant solution provides full control over the end user experience built on a set of foundational capabilities. Additionally, the experience can be infused with intelligence about the end-user and any device/ecosystem information for a truly integrated and intelligent experience.\nFind out more [here](https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/docs/README.md).");
                
                testNock.resolveWithMocks('mainDialog_faq_response', done, flow);
            });
        });
    });
});
