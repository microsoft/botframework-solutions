const assert = require('assert');
const { MemoryStorage } = require('botbuilder-core')
const botTestBase = require('./botTestBase.js');
const testNock = require('../testBase');

let testStorage = new MemoryStorage();

describe("Onboarding Dialog", function () {
    beforeEach(function () {
        botTestBase.initialize(testStorage);
    });

    afterEach(function () {
        testStorage = new MemoryStorage();
    });

    describe("Onboarding", function () {
        it("Spin up the OnboardingDialog directly", function (done) {
            const testAdapter = botTestBase.getTestAdapter();
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

            testNock.resolveWithMocks('onboardingDialog_init', done, flow);
        });
        it("Response for name prompt", function (done) {
            const testAdapter = botTestBase.getTestAdapter();
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
        it("Response for email prompt", function (done) {
            const testAdapter = botTestBase.getTestAdapter();
            const flow = testAdapter
                .send({
                    channelId: "emulator",
                    conversation: {
                        id: "answerEmail"
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
                        id: "answerEmail"
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
                .assertReply('What is your email?')
                .send({
                    channelId: "emulator",
                    conversation: {
                        id: "answerEmail"
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
                    text: "user@mail.com",
                    textFormat: "plain",
                    type: "message"
                })
                .assertReply('Got it. I\'ve added user@mail.com as your primary contact address.');

            testNock.resolveWithMocks('onboardingDialog_emailPrompt', done, flow);
        });
        it("Response for location prompt", function (done) {
            const testAdapter = botTestBase.getTestAdapter();
            const flow = testAdapter
                .send({
                    channelId: "emulator",
                    conversation: {
                        id: "answerLocation"
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
                        id: "answerLocation"
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
                .assertReply('What is your email?')
                .send({
                    channelId: "emulator",
                    conversation: {
                        id: "answerLocation"
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
                    text: "user@mail.com",
                    textFormat: "plain",
                    type: "message"
                })
                .assertReply('Got it. I\'ve added user@mail.com as your primary contact address.')
                .assertReply('Finally, where are you located?')
                .send({
                    channelId: "emulator",
                    conversation: {
                        id: "answerLocation"
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
                    text: "Seattle",
                    textFormat: "plain",
                    type: "message"
                })
                .assertReply('Thanks, user. I\'ve added Seattle as your primary location. You\'re all set up!')
                .assertReply('What else can I help you with?');

            testNock.resolveWithMocks('onboardingDialog_locationPrompt', done, flow);
        });
        it("Validate state is updated", function (done) {
            const testAdapter = botTestBase.getTestAdapter();
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

            testNock.resolveWithMocks('onboardingDialog_validation', done, flow);
        });
    });

    describe("Onboarding Cancellation", function () {
        it("Invoke a 'Cancel' action during name prompt and confirm", function (done) {
            const testAdapter = botTestBase.getTestAdapter();
            const flow = testAdapter
                .send({
                    channelId: "emulator",
                    conversation: {
                        id: "cancelNamePrompt"
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
                        id: "cancelNamePrompt"
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
                    text: "cancel",
                    textFormat: "plain",
                    type: "message"
                })
                .assertReply(async function (activity, description) {
                    assert(activity.suggestedActions);
                    assert.deepStrictEqual(activity.suggestedActions.actions[0].value, "Yes");
                    assert.deepStrictEqual(activity.suggestedActions.actions[1].value, "No");
                })
                .send({
                    channelId: "emulator",
                    conversation: {
                      id: "cancelNamePrompt"
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
                    text: "Yes",
                    textFormat: "plain",
                    type: "message"
                })
                .assertReply('Ok, let\'s start over.')
                .assertReply('What else can I help you with?');

            testNock.resolveWithMocks('onboardingDialog_cancel_confirm', done, flow);
        });
        it("Invoke a 'Cancel' action during email prompt and deny", function (done) {
            const testAdapter = botTestBase.getTestAdapter();
            const flow = testAdapter
                .send({
                    channelId: "emulator",
                    conversation: {
                        id: "cancelEmailPrompt"
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
                        id: "cancelEmailPrompt"
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
                .assertReply('What is your email?')
                .send({
                    channelId: "emulator",
                    conversation: {
                        id: "cancelEmailPrompt"
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
                    text: "cancel",
                    textFormat: "plain",
                    type: "message"
                })
                .assertReply(async function (activity, description) {
                    assert(activity.suggestedActions);
                    assert.deepStrictEqual(activity.suggestedActions.actions[0].value, "Yes");
                    assert.deepStrictEqual(activity.suggestedActions.actions[1].value, "No");
                })
                .send({
                    channelId: "emulator",
                    conversation: {
                      id: "cancelEmailPrompt"
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
                    text: "No",
                    textFormat: "plain",
                    type: "message"
                })
                .assertReply('Ok, let\'s keep going.')
                .assertReply('What is your email?');

            testNock.resolveWithMocks('onboardingDialog_cancel_deny', done, flow);
        });
    });
});
