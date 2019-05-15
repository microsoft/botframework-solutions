/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License
 */

const botTestBase = require("./botTestBase");
const { MemoryStorage } = require('botbuilder-core')
const testNock = require("../testBase");
let testStorage = new MemoryStorage();

describe("Interruption", function() {
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
        beforeEach(function(done) {
            testStorage = new MemoryStorage();
            done();
        });
        it("send 'help' during the onBoarding dialog", function(done) {
            botTestBase.getTestAdapterDefault({ storage: testStorage }).then((testAdapter) => {
                const flow = testAdapter
                .send({
                    channelId: "test",
                    conversation: {
                        id: "Convo1"
                    },
                    from: {
                        id: "user",
                        name: "User1"
                    },
                    recipient: {
                        id: "bot",
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

    describe("cancel interruption flow", function() {
        it("Confirm 'cancel' during the onboarding dialog", function(done) {
            botTestBase.getTestAdapterDefault().then((testAdapter) => {
                const flow = testAdapter
                    .send({
                        channelId: "test",
                        conversation: {
                            id: "Convo1"
                        },
                        from: {
                            id: "user",
                            name: "User1"
                        },
                        recipient: {
                            id: "bot",
                            name: "Bot",
                            role: "bot"
                        },
                        type: "message",
                        value: {
                            action: "startOnboarding"
                        }
                    })
                    .assertReply('What is your name?')
                    .send("cancel")
                    .assertReply(" (1) Yes or (2) No")
                    .send("Yes")
                    .assertReply("Ok, let's start over.");
                return testNock.resolveWithMocks("interruption_confirm_cancel_response", done, flow);
            });
        });

        it("Deny 'cancel' during the onboarding dialog", function(done) {
            botTestBase.getTestAdapterDefault().then((testAdapter) => {
                const flow = testAdapter
                    .send({
                        channelId: "test",
                        conversation: {
                            id: "Convo1"
                        },
                        from: {
                            id: "user",
                            name: "User1"
                        },
                        recipient: {
                            id: "bot",
                            name: "Bot",
                            role: "bot"
                        },
                        type: "message",
                        value: {
                            action: "startOnboarding"
                        }
                    })
                    .assertReply('What is your name?')
                    .send("cancel")
                    .assertReply(" (1) Yes or (2) No")
                    .send("No")
                    .assertReply("Ok, let's keep going.");
                return testNock.resolveWithMocks("interruption_deny_cancel_response", done, flow);
            });
        });
    });
});