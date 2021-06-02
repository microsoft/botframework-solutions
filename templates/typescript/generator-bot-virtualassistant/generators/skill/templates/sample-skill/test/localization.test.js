/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const skillTestBase = require("./helpers/skillTestBase");
const testNock = require("./helpers/testBase");

describe("localization", function() {
    beforeEach(async function() {
        await skillTestBase.initialize();
    });

    describe("de-de locale", function() {
        it("send conversationUpdate and check the intro message is received with the de-de locale", function(done) {
            const testAdapter = skillTestBase.getTestAdapter();
            const flow = testAdapter
                .send({
                    type: "conversationUpdate",
                    membersAdded: [
                        {
                            id: "1",
                            name: "user"
                        },
                        {
                            id: "2",
                            name: "bot"
                        }
                    ],
                    channelId: "emulator",
                    recipient: {
                        id: "1"
                    },
                    locale: "de-de"
                })
                .assertReply("Willkommen zu Ihrem benutzerdefinierten skill!");

            return testNock.resolveWithMocks("localization_de-de_response", done, flow);
        });
    });

    describe("es-es locale", function() {
        it("send conversationUpdate and check the intro message is received with the es-es locale", function(done) {
            const testAdapter = skillTestBase.getTestAdapter();
            const flow = testAdapter
                .send({
                    type: "conversationUpdate",
                    membersAdded: [
                        {
                            id: "1",
                            name: "user"
                        },
                        {
                            id: "2",
                            name: "bot"
                        }
                    ],
                    channelId: "emulator",
                    recipient: {
                        id: "1"
                    },
                    locale: "es-es"
                })
                .assertReply("¡Bienvenido a tu skill personalizada!");

            return testNock.resolveWithMocks("localization_es-es_response", done, flow);
        });
    });

    describe("fr-fr locale", function() {
        it("send conversationUpdate and check the intro message is received with the fr-fr locale", function(done) {
            const testAdapter = skillTestBase.getTestAdapter();
            const flow = testAdapter
                .send({
                    type: "conversationUpdate",
                    membersAdded: [
                        {
                            id: "1",
                            name: "user"
                        },
                        {
                            id: "2",
                            name: "bot"
                        }
                    ],
                    channelId: "emulator",
                    recipient: {
                        id: "1"
                    },
                    locale: "fr-fr"
                })
                .assertReply("Bienvenue à vos skill personnalisées!");

            return testNock.resolveWithMocks("localization_fr-fr_response", done, flow);
        });
    });

    describe("it-it locale", function() {
        it("send conversationUpdate and check the intro message is received with the it-it locale", function(done) {
            const testAdapter = skillTestBase.getTestAdapter();
            const flow = testAdapter
                .send({
                    type: "conversationUpdate",
                    membersAdded: [
                        {
                            id: "1",
                            name: "user"
                        },
                        {
                            id: "2",
                            name: "bot"
                        }
                    ],
                    channelId: "emulator",
                    recipient: {
                        id: "1"
                    },
                    locale: "it-it"
                })
                .assertReply("Benvenuto nella tua skill personalizzata!");

            return testNock.resolveWithMocks("localization_it-it_response", done, flow);
        });
    });

    describe("en-us locale", function() {
        it("send conversationUpdate and check the intro message is received with the en-us locale", function(done) {
            const testAdapter = skillTestBase.getTestAdapter();
            const flow = testAdapter
                .send({
                    type: "conversationUpdate",
                    membersAdded: [
                        {
                            id: "1",
                            name: "user"
                        },
                        {
                            id: "2",
                            name: "bot"
                        }
                    ],
                    channelId: "emulator",
                    recipient: {
                        id: "1"
                    },
                    locale: "en-us"
                })
                .assertReply("Welcome to your custom skill!");

            return testNock.resolveWithMocks("localization_en-us_response", done, flow);
        });
    });

    describe("zh-cn locale", function() {
        it("send conversationUpdate and check the intro message is received with the zh-zh locale", function(done) {
            const testAdapter = skillTestBase.getTestAdapter();
            const flow = testAdapter
                .send({
                    type: "conversationUpdate",
                    membersAdded: [
                        {
                            id: "1",
                            name: "user"
                        },
                        {
                            id: "2",
                            name: "bot"
                        }
                    ],
                    channelId: "emulator",
                    recipient: {
                        id: "1"
                    },
                    locale: "zh-cn"
                })
                .assertReply("欢迎来到您的定制技能！");

            return testNock.resolveWithMocks("localization_zh-cn_response", done, flow);
        });
    });

    describe("Defaulting localization", function () {
        xit("Fallback to a locale of the root language locale", function (done) {
            const testAdapter = skillTestBase.getTestAdapter();
            const flow = testAdapter
                .send({
                    type: "conversationUpdate",
                    membersAdded: [
                        {
                            id: "1",
                            name: "user"
                        },
                        {
                            id: "2",
                            name: "bot"
                        }
                    ],
                    channelId: "emulator",
                    recipient: {
                        id: "1"
                    },
                    locale: "en-gb"
                })
                .assertReply("Welcome to your custom skill!");

            return testNock.resolveWithMocks('localization_response_en-gb', done, flow);
        });
    });
});
