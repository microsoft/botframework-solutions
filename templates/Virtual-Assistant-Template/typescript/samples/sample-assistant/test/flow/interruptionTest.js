    
/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const botTestBase = require("./botTestBase.js");
const testNock = require("../testBase");

describe("interruption", function() {
    describe("nothing to cancel", function() {
        it("send 'cancel' without dialog stack", function(done) {
            botTestBase.getTestAdapterDefault().then((testAdapter) => {
                const flow = testAdapter
                    .send("cancel")
                    .assertReply("Looks like there's nothing to cancel! Try saying 'help' to get started.");

                return testNock.resolveWithMocks("interruption_nothing_to_cancel", done, flow);
            });
        });
    });

    describe("help interruption", function() {
        it("send 'help' during the onBoarding dialog", function(done) {
            botTestBase.getTestAdapterDefault().then((testAdapter) => {
                const flow = testAdapter
                .send({
                    channelId: "emulator",
                    conversation: {
                        id: "stateUpdated"
                    },
                    from: {
                        id: "User",
                        name: "User"
                    },
                    recipient: {
                        id: "1",
                        name: "Bot",
                        role: "bot"
                    },
                    type: "message",
                    value: {
                        action: "startOnboarding"
                    }
                })
                .assertReply('What is your name?')
                .send("help")
                .assertReply('I\'m your Virtual Assistant! I can perform a number of tasks through my connected skills. Right now I can help you with Calendar, Email, Task and Point of Interest questions. Or you can help me do more by creating your own!')
                .assertReply('What is your name?');

                return testNock.resolveWithMocks("interruption_help_response", done, flow);
            });
        });
    });

    xdescribe("cancel interruption", function() {
            it("send 'cancel' during the sample dialog", function(done) {
                botTestBase.getTestAdapterDefault().then((testAdapter) => {
                const flow = testAdapter
                .send("sample dialog")
                .assertReply("What is your name?")
                .send("cancel")
                .assertReply("Ok, let's start over.");
            testNock.resolveWithMocks("interruption_cancel_response", done, flow);
            });
        });
    });
});