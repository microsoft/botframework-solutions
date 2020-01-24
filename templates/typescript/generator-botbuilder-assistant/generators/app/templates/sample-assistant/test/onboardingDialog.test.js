/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License
 */

const assert = require('assert');
const { MemoryStorage } = require('botbuilder-core')
const botTestBase = require('./helpers/botTestBase');
const testNock = require('./helpers/testBase');
let testStorage = new MemoryStorage();

describe("Onboarding Dialog", function () {
    describe("Onboarding", function () {
        beforeEach(function(done) {
            testStorage = new MemoryStorage();
            done();
        });

        it("Spin up the OnboardingDialog directly", function (done) {
            botTestBase.getTestAdapterDefault({ storage: testStorage }).then((testAdapter) => {
                const flow = testAdapter
                    .send({
                        channelId: "emulator",
                        conversation: {
                            id: "spinUpOnboardingDirectly"
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
                    .assertReply('What is your name?');

                return testNock.resolveWithMocks('onboardingDialog_init', done, flow);
            });
        });    
        it("Response for name prompt", function (done) {
            botTestBase.getTestAdapterDefault({ storage: testStorage }).then((testAdapter) => {
                const flow = testAdapter
                    .send({
                        channelId: "emulator",
                        conversation: {
                            id: "answerName"
                        },
                        from: {
                            id: "User",
                            name: "User"
                        },
                        locale: "",
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
                    .send({
                        channelId: "emulator",
                        conversation: {
                            id: "answerName"
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
                        text: "user",
                        textFormat: "plain",
                        type: "message"
                    })
                    .assertReply('Hi, user!');
    
                testNock.resolveWithMocks('onboardingDialog_namePrompt', done, flow);
            });
        });
        it("Validate state is updated", function (done) {
            botTestBase.getTestAdapterDefault({ storage: testStorage }).then((testAdapter) => {
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
                        text: "user",
                        textFormat: "plain",
                        type: "message"
                    })
                    .assertReply('Hi, user!')
                    .then(async () => {
                        const state = await testStorage.read(['emulator/users/User/']);
                        assert.strictEqual(state['emulator/users/User/'].OnboardingState.name, 'user');
                    });
    
                return testNock.resolveWithMocks('onboardingDialog_validation', done, flow);
            });
        });
    });
});   
