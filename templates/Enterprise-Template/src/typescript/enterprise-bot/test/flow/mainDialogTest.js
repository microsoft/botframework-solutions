const assert = require('assert');
const enterpriseBotTestBase = require('./enterpriseBotTestBase.js');
const testNock = require('../testBase');
const introJson = require('../../src/dialogs/main/resources/Intro.json');

describe("Main Dialog", function() {
    beforeEach(async function() {
        await enterpriseBotTestBase.initialize();
    });

  	describe("Intro Card", function() {
		it("Send conversationUpdate and verify card is received", function(done) {
			const testAdapter = enterpriseBotTestBase.getTestAdapter();
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
			.assertReply(function(activity, description) {
				assert.equal(activity.attachments[0].contentType, 'application/vnd.microsoft.card.adaptive');
				assert.deepEqual(activity.attachments[0].content, introJson);
			})
			
			testNock.resolveWithMocks(this.test.title, done, flow);
		});
  	});

	describe("Greeting", function() {
		it("Send Hello and check you get the expected response", function(done) {
			const testAdapter = enterpriseBotTestBase.getTestAdapter();
			const flow = testAdapter
				.send('Hi')
				.assertReply('Hello.');
				
				testNock.resolveWithMocks(this.test.title, done, flow);
		});
	});

	describe("Help", function() {
		it("Send Help and check you get the expected response", function(done) {
			const testAdapter = enterpriseBotTestBase.getTestAdapter();
			const flow = testAdapter
				.send('Help')
				.assertReply('This card can be used to display information to help your user interact with your bot. The buttons below can be used for sample queries or links to external sites.');
				
				testNock.resolveWithMocks(this.test.title, done, flow);
		});
	});

	describe("Greeting (localized)", function() {
		xit("Send Hello in FIGS+ZH, set locale property on activity", function(done) {
		});
		xit("Validate you get a localized response", function(done) {
		});
	});
});
