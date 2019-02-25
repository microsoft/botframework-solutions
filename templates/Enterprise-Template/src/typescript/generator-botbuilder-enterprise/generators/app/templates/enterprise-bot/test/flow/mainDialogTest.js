const assert = require('assert');
const botTestBase = require('./botTestBase.js');
const testNock = require('../testBase');
const introJson = require('../../src/dialogs/main/resources/Intro.json');

describe("Main Dialog", function () {
	beforeEach(function () {
		botTestBase.initialize();
	});

	describe("Intro Card", function () {
		it("Send conversationUpdate and verify card is received", function (done) {
			const testAdapter = botTestBase.getTestAdapter();
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
				})

			testNock.resolveWithMocks('mainDialog_introCard_response', done, flow);
		});
	});

	describe("Greeting", function () {
		it("Send Hello and check you get the expected response", function (done) {
			const testAdapter = botTestBase.getTestAdapter();
			const flow = testAdapter
				.send('Hi')
				.assertReply('Hello.');

			testNock.resolveWithMocks('mainDialog_greeting_response', done, flow);
		});
	});

	describe("Help", function () {
		it("Send Help and check you get the expected response", function (done) {
			const testAdapter = botTestBase.getTestAdapter();
			const flow = testAdapter
				.send('Help')
				.assertReply('This card can be used to display information to help your user interact with your bot. The buttons below can be used for sample queries or links to external sites.');

			testNock.resolveWithMocks('mainDialog_help_response', done, flow);
		});
	});

	describe("Localization", function () {
		it("Send a message in FIGS+ZH, set locale property on activity and validate the localized response", function (done) {
			const testAdapter = botTestBase.getTestAdapter();
			const flow = testAdapter
				.send({
					type: "message",
					text: "All of them",
					locale: "zh"
				})
				.assertReply('对不起, 我帮不上忙。');

			testNock.resolveWithMocks('mainDialog_localization_response', done, flow);
		});
	});
});
