const assert = require('assert');
const { MemoryStorage } = require('botbuilder-core')
const enterpriseBotTestBase = require('./enterpriseBotTestBase.js');
const testNock = require('../testBase');

let testStorage = new MemoryStorage();

describe("Onboarding Dialog", function () {
    beforeEach(function () {
        enterpriseBotTestBase.initialize(testStorage);
    });

    afterEach(function () {
        testStorage = new MemoryStorage();
    });

    describe("Onboarding", function () {
        it("In the test spin up the OnboardingDialog directly", function (done) {
            const testAdapter = enterpriseBotTestBase.getTestAdapter();
            const flow = testAdapter
                .send({
                    channelData: {
                        postback: true
                    },
                    channelId: "emulator",
                    conversation: {
                        id: "06342ab0-2ada-11e9-9083-71c7608e7e77|livechat"
                    },
                    from: {
                        id: "3cdfda9c-eb22-420b-ab7b-6b4080ef9349",
                        name: "User"
                    },
                    locale: "",
                    recipient: {
                        id: "1",
                        name: "Bot",
                        role: "bot"
                    },
                    serviceUrl: "http://localhost:57959",
                    type: "message",
                    value: {
                        action: "startOnboarding"
                    }
                })
                .assertReply('What is your name?');

            testNock.resolveWithMocks(this.test.title, done, flow);
        });
        it("Response for name prompt", function (done) {
            const testAdapter = enterpriseBotTestBase.getTestAdapter();
            const flow = testAdapter
                .send({
                    channelData: {
                        postback: true
                    },
                    channelId: "emulator",
                    conversation: {
                        id: "06342ab0-2ada-11e9-9083-71c7608e7e77|livechat"
                    },
                    from: {
                        id: "3cdfda9c-eb22-420b-ab7b-6b4080ef9349",
                        name: "User"
                    },
                    locale: "",
                    recipient: {
                        id: "1",
                        name: "Bot",
                        role: "bot"
                    },
                    serviceUrl: "http://localhost:57959",
                    type: "message",
                    value: {
                        action: "startOnboarding"
                    }
                })
                .assertReply('What is your name?')
                .send({
                    channelId: "emulator",
                    conversation: {
                        id: "06342ab0-2ada-11e9-9083-71c7608e7e77|livechat"
                    },
                    entities: [
                        {
                            requiresBotState: true,
                            supportsListening: true,
                            supportsTts: true,
                            type: "ClientCapabilities"
                        }
                    ],
                    from: {
                        id: "3cdfda9c-eb22-420b-ab7b-6b4080ef9349",
                        name: "User"
                    },
                    locale: "",
                    recipient: {
                        id: "1",
                        name: "Bot",
                        role: "bot"
                    },
                    serviceUrl: "http://localhost:50802",
                    text: "user",
                    textFormat: "plain",
                    type: "message"
                })
                .assertReply('Hi, user!');

            testNock.resolveWithMocks(this.test.title, done, flow);
        });
        it("Response for email prompt", function (done) {
            const testAdapter = enterpriseBotTestBase.getTestAdapter();
            const flow = testAdapter
                .send({
                    channelData: {
                        postback: true
                    },
                    channelId: "emulator",
                    conversation: {
                        id: "06342ab0-2ada-11e9-9083-71c7608e7e77|livechat"
                    },
                    from: {
                        id: "3cdfda9c-eb22-420b-ab7b-6b4080ef9349",
                        name: "User"
                    },
                    locale: "",
                    recipient: {
                        id: "1",
                        name: "Bot",
                        role: "bot"
                    },
                    serviceUrl: "http://localhost:57959",
                    type: "message",
                    value: {
                        action: "startOnboarding"
                    }
                })
                .assertReply('What is your name?')
                .send({
                    channelId: "emulator",
                    conversation: {
                        id: "06342ab0-2ada-11e9-9083-71c7608e7e77|livechat"
                    },
                    entities: [
                        {
                            requiresBotState: true,
                            supportsListening: true,
                            supportsTts: true,
                            type: "ClientCapabilities"
                        }
                    ],
                    from: {
                        id: "3cdfda9c-eb22-420b-ab7b-6b4080ef9349",
                        name: "User"
                    },
                    locale: "",
                    recipient: {
                        id: "1",
                        name: "Bot",
                        role: "bot"
                    },
                    serviceUrl: "http://localhost:50802",
                    text: "user",
                    textFormat: "plain",
                    type: "message"
                })
                .assertReply('Hi, user!')
                .assertReply('What is your email?')
                .send({
                    channelId: "emulator",
                    conversation: {
                        id: "06342ab0-2ada-11e9-9083-71c7608e7e77|livechat"
                    },
                    entities: [
                        {
                            requiresBotState: true,
                            supportsListening: true,
                            supportsTts: true,
                            type: "ClientCapabilities"
                        }
                    ],
                    from: {
                        id: "3cdfda9c-eb22-420b-ab7b-6b4080ef9349",
                        name: "User"
                    },
                    locale: "",
                    recipient: {
                        id: "1",
                        name: "Bot",
                        role: "bot"
                    },
                    serviceUrl: "http://localhost:50802",
                    text: "user@mail.com",
                    textFormat: "plain",
                    type: "message"
                })
                .assertReply('Got it. I\'ve added user@mail.com as your primary contact address.');

            testNock.resolveWithMocks(this.test.title, done, flow);
        });
        it("Response for location prompt", function (done) {
            const testAdapter = enterpriseBotTestBase.getTestAdapter();
            const flow = testAdapter
                .send({
                    channelData: {
                        postback: true
                    },
                    channelId: "emulator",
                    conversation: {
                        id: "06342ab0-2ada-11e9-9083-71c7608e7e77|livechat"
                    },
                    from: {
                        id: "3cdfda9c-eb22-420b-ab7b-6b4080ef9349",
                        name: "User"
                    },
                    locale: "",
                    recipient: {
                        id: "1",
                        name: "Bot",
                        role: "bot"
                    },
                    serviceUrl: "http://localhost:57959",
                    type: "message",
                    value: {
                        action: "startOnboarding"
                    }
                })
                .assertReply('What is your name?')
                .send({
                    channelId: "emulator",
                    conversation: {
                        id: "06342ab0-2ada-11e9-9083-71c7608e7e77|livechat"
                    },
                    entities: [
                        {
                            requiresBotState: true,
                            supportsListening: true,
                            supportsTts: true,
                            type: "ClientCapabilities"
                        }
                    ],
                    from: {
                        id: "3cdfda9c-eb22-420b-ab7b-6b4080ef9349",
                        name: "User"
                    },
                    locale: "",
                    recipient: {
                        id: "1",
                        name: "Bot",
                        role: "bot"
                    },
                    serviceUrl: "http://localhost:50802",
                    text: "user",
                    textFormat: "plain",
                    type: "message"
                })
                .assertReply('Hi, user!')
                .assertReply('What is your email?')
                .send({
                    channelId: "emulator",
                    conversation: {
                        id: "06342ab0-2ada-11e9-9083-71c7608e7e77|livechat"
                    },
                    entities: [
                        {
                            requiresBotState: true,
                            supportsListening: true,
                            supportsTts: true,
                            type: "ClientCapabilities"
                        }
                    ],
                    from: {
                        id: "3cdfda9c-eb22-420b-ab7b-6b4080ef9349",
                        name: "User"
                    },
                    locale: "",
                    recipient: {
                        id: "1",
                        name: "Bot",
                        role: "bot"
                    },
                    serviceUrl: "http://localhost:50802",
                    text: "user@mail.com",
                    textFormat: "plain",
                    type: "message"
                })
                .assertReply('Got it. I\'ve added user@mail.com as your primary contact address.')
                .assertReply('Finally, where are you located?')
                .send({
                    channelId: "emulator",
                    conversation: {
                        id: "06342ab0-2ada-11e9-9083-71c7608e7e77|livechat"
                    },
                    entities: [
                        {
                            requiresBotState: true,
                            supportsListening: true,
                            supportsTts: true,
                            type: "ClientCapabilities"
                        }
                    ],
                    from: {
                        id: "3cdfda9c-eb22-420b-ab7b-6b4080ef9349",
                        name: "User"
                    },
                    locale: "",
                    recipient: {
                        id: "1",
                        name: "Bot",
                        role: "bot"
                    },
                    serviceUrl: "http://localhost:50802",
                    text: "Seattle",
                    textFormat: "plain",
                    type: "message"
                })
                .assertReply('Thanks, user. I\'ve added Seattle as your primary location. You\'re all set up!')
                .assertReply('What else can I help you with?');

            testNock.resolveWithMocks(this.test.title, done, flow);
        });
        it("Validate state is updated", function (done) {
            const testAdapter = enterpriseBotTestBase.getTestAdapter();
            const flow = testAdapter
                .send({
                    channelData: {
                        postback: true
                    },
                    channelId: "emulator",
                    conversation: {
                        id: "06342ab0-2ada-11e9-9083-71c7608e7e77|livechat"
                    },
                    from: {
                        id: "3cdfda9c-eb22-420b-ab7b-6b4080ef9349",
                        name: "User"
                    },
                    locale: "",
                    recipient: {
                        id: "1",
                        name: "Bot",
                        role: "bot"
                    },
                    serviceUrl: "http://localhost:57959",
                    type: "message",
                    value: {
                        action: "startOnboarding"
                    }
                })
                .assertReply('What is your name?')
                .send({
                    channelId: "emulator",
                    conversation: {
                        id: "06342ab0-2ada-11e9-9083-71c7608e7e77|livechat"
                    },
                    entities: [
                        {
                            requiresBotState: true,
                            supportsListening: true,
                            supportsTts: true,
                            type: "ClientCapabilities"
                        }
                    ],
                    from: {
                        id: "3cdfda9c-eb22-420b-ab7b-6b4080ef9349",
                        name: "User"
                    },
                    locale: "",
                    recipient: {
                        id: "1",
                        name: "Bot",
                        role: "bot"
                    },
                    serviceUrl: "http://localhost:50802",
                    text: "user",
                    textFormat: "plain",
                    type: "message"
                })
                .assertReply(async function (activity, description) {
                    const state = await testStorage.read(['emulator/users/3cdfda9c-eb22-420b-ab7b-6b4080ef9349/']);
                    assert(state['emulator/users/3cdfda9c-eb22-420b-ab7b-6b4080ef9349/'].OnboardingState.name === 'user');
                });

            testNock.resolveWithMocks(this.test.title, done, flow);
        });
    });

    describe("Onboarding Cancellation", function () {
        xit("Invoke a 'Cancel' action during name prompt", function (done) {
        });
        xit("Validate confirmation prompt", function (done) {
        });
        xit("Send Yes", function (done) {
        });
        xit("Validate you go back", function (done) {
        });
    });
});
