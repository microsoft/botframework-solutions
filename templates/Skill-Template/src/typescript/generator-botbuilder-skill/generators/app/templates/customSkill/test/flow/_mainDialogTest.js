// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

const assert = require('assert');
const <%=skillTemplateName%> = require('./<%=skillTemplateNameFile%>');
const testNock = require('../testBase');
const unhandledReplies = [
    "Can you try to ask me again? I didn't get what you mean.",
    "Can you say that in a different way?",
    "Can you try to ask in a different way?",
    "Could you elaborate?",
    "Please say that again in a different way.",
    "I didn't understand, perhaps try again in a different way.",
    "I didn't get what you mean, can you try in a different way?",
    "Sorry, I didn't understand what you meant.",
    "I didn't quite get that."
];

describe("mainDialog", function() {
    before(async function() {
        await <%=skillTemplateName%>.initialize();
    });

    describe("intro message", function() {
        it("send conversationUpdate and verify the response", function(done){
            const testAdapter = customSkill.getTestAdapter();
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
            .assertReply('[Enter your intro message here]');

            testNock.resolveWithMocks('introMessage_response', done, flow);
        });
    });

    describe("help intent", function() {
        it("send 'Help' and check you get the expected response", function(done){
            const testAdapter = customSkill.getTestAdapter();
            const flow = testAdapter
                .send('Help')
                .assertReply('[Enter your help message here]');
            
            testNock.resolveWithMocks('helpIntent_response', done, flow);
        }); 
    });

    describe("unhandled message", function() {
        it("send 'Blah Blah' and check you get the expected response", function(done){
            const testAdapter = customSkill.getTestAdapter();
            const flow = testAdapter
                .send('Blah Blah')
				.assertReply(function (activity, description) {
                    assert.notEqual(-1, unhandledReplies.indexOf(activity.text));
				})
                
            testNock.resolveWithMocks('unhandledMessage_response', done, flow);
        }); 
    });
});
