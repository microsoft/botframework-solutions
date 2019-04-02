// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

const assert = require('assert');
const assistantTestBase = require('./assistantTestBase');
const testNock = require('../testBase');
const introJson = require('../../src/dialogs/main/resources/Intro.json');

describe("virtual assistant", function() {
    before(async function() {
        await assistantTestBase.initialize();
    });

    describe("intro card", function() {
        it("send conversationUpdate and verify card is received", function(done){
            const testAdapter = assistantTestBase.getTestAdapter();
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
					locale: "en"
                })
				.assertReply(function (activity, description) {
					assert.equal(activity.attachments[0].contentType, 'application/vnd.microsoft.card.adaptive');
					assert.deepEqual(activity.attachments[0].content, introJson);
                })

            testNock.resolveWithMocks('introCard_response', done, flow);
            
        });
    });

    describe("escalate", function() {
        it("send 'I want to talk to a human' and check you get the expected response", function(done) {
            const testAdapter = assistantTestBase.getTestAdapter();
            const flow = testAdapter
                .send('I want to talk to a human')
                .assertReply('Our agents are available 24/7 at 1(800)555-1234.');
            
            testNock.resolveWithMocks('escalate_response', done, flow);
        });
    });

    describe("help", function() {
        it("send 'Help' and check you get the expected response", function(done) {
            const testAdapter = assistantTestBase.getTestAdapter();
            const flow = testAdapter
                .send('Help')
                .assertReply(function (activity, description) {
                    assert.equal(activity.attachments[0].contentType, 'application/vnd.microsoft.card.hero');
                    assert.deepStrictEqual(activity.attachments[0].content.text, `I'm your Virtual Assistant! I can perform a number of tasks through my connected skills. Right now I can help you with Calendar, Email, Task and Point of Interest questions. Or you can help me do more by creating your own!`)
                    assert.deepStrictEqual(activity.attachments[0].content.title, 'Help for Virtual Assistant');
                })
            testNock.resolveWithMocks('help_response', done, flow);
        });
    });

    describe("cancel", function() {
        it("send 'Cancel' and check you get the expected response", function(done) {
            const testAdapter = assistantTestBase.getTestAdapter();
            const flow = testAdapter
                .send('Cancel')
                .assertReply('It looks like there is nothing to cancel. What can I help you with?')
            testNock.resolveWithMocks('cancel_response', done, flow);
        });
    });

    // TODO: All tests from here will be on-hold until the migration of skills are finished 
    xdescribe("confused", function() {
        it("send 'Blah Blah' and check you get the expected response", function(done) {
            const testAdapter = assistantTestBase.getTestAdapter();
            const flow = testAdapter
                .send('Blah Blah')
                .assertReply(`I'm sorry, I'm not able to help with that.`)
            testNock.resolveWithMocks('confused_response', done, flow);
        });
    });

    xdescribe("calendarSkillInvocation", function() {
        it("send 'Accept this event.' and check that the message is processed by CalendarSkill", function(done) {
            const testAdapter = assistantTestBase.getTestAdapter();
            const flow = testAdapter
                .send('Accept this event.')
                .assertReply(``)
            testNock.resolveWithMocks('calendarSkillInvocation_response', done, flow);
        });
    });

    xdescribe("emailSkillInvocation", function() {
        it("send 'Delete this message permanently' and check that the message is processed by EmailSkill", function(done) {
            const testAdapter = assistantTestBase.getTestAdapter();
            const flow = testAdapter
                .send('Delete this message permanently')
                .assertReply(``)
            testNock.resolveWithMocks('emailSkillInvocation_response', done, flow);
        });
    });

    xdescribe("toDoSkillInvocation", function() {
        it("send 'All shoping list' and check that the message is processed by toDoSkill", function(done) {
            const testAdapter = assistantTestBase.getTestAdapter();
            const flow = testAdapter
                .send('All shoping list')
                .assertReply(``)
            testNock.resolveWithMocks('toDoSkillInvocation_response', done, flow);
        });
    });

    xdescribe("pointOfInterestSkillInvocation", function() {
        it("send 'Find a route' and check that the message is processed by pointOfInterestSkill", function(done) {
            const testAdapter = assistantTestBase.getTestAdapter();
            const flow = testAdapter
                .send('Find a route')
                .assertReply(``)
            testNock.resolveWithMocks('pointOfInterestSkillInvocation_response', done, flow);
        });
    });

    xdescribe("locationEventProcessed", function() {

    });

    xdescribe("resetUserEventProcessed", function() {

    });

    xdescribe("timeZoneEventProcessed", function() {

    });
});