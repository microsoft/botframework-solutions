const assert = require('assert');
const { MemoryStorage } = require('botbuilder-core')
const botTestBase = require('./botTestBase.js');
const testNock = require('../testBase');
let testStorage = new MemoryStorage();

describe("Onboarding Dialog", function () {
    describe("Onboarding", function () {
        it("Spin up the OnboardingDialog directly", function (done) {
            botTestBase.getTestAdapterDefault().then((testAdapter) => {
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

                return testNock.simpleMock('onboardingDialog_init', done, flow);
            });
        });
    });    
    it("Response for name prompt", function (done) {
        botTestBase.getTestAdapterDefault().then((testAdapter) => {
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

            testNock.simpleMock('onboardingDialog_namePrompt', done, flow);
        });
    });
    it("Validate state is updated", function (done) {
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
                .assertReply(async function (activity, description) {
                    const state = await testStorage.read(['emulator/users/User/']);
                    assert(state['emulator/users/User/'].OnboardingState.name === 'user');
                });

            return testNock.simpleMock('onboardingDialog_validation', done, flow);
        });
    });
});   
