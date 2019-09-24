/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License
 */

const assert = require('assert');
const botTestBase = require('./helpers/botTestBase');
const testNock = require('./helpers/testBase');
const localizationJsonDe = require('../src/content/NewUserGreeting.de-de.json');
const localizationJsonEs = require('../src/content/NewUserGreeting.es-es.json');
const localizationJsonFr = require('../src/content/NewUserGreeting.fr-fr.json');
const localizationJsonIt = require('../src/content/NewUserGreeting.it-it.json');
const localizationJson = require('../src/content/NewUserGreeting.json');
const localizationJsonZh = require('../src/content/NewUserGreeting.zh-cn.json');

describe("Localization", function() {
	describe("de-de locale", function () {
            it("send conversationUpdate and check the card is received with the de-de locale", function (done) {
                botTestBase.getTestAdapterDefault().then((testAdapter) => {
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
                    locale: "de-de"
                })
                .assertReply(function (activity, description) {
                    assert.strictEqual(activity.attachments[0].contentType, 'application/vnd.microsoft.card.adaptive');
					assert.deepStrictEqual(activity.attachments[0].content, localizationJsonDe);
                });

                return testNock.resolveWithMocks('localization_response_de-de', done, flow);
            });
        });
    });
	describe("es-es locale", function () {
        it("send conversationUpdate and check the card is received with the es-es locale", function (done) {
                botTestBase.getTestAdapterDefault().then((testAdapter) => {
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
                    locale: "es-es"
                })
                .assertReply(function (activity, description) {
                    assert.strictEqual(activity.attachments[0].contentType, 'application/vnd.microsoft.card.adaptive');
					assert.deepStrictEqual(activity.attachments[0].content, localizationJsonEs);
                });

                return testNock.resolveWithMocks('localization_response_es-es', done, flow);
            });
        });
    });
	describe("fr-fr locale", function () {
            it("send conversationUpdate and check the card is received with the fr-fr locale", function (done) {
                botTestBase.getTestAdapterDefault().then((testAdapter) => {
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
                    locale: "fr-fr"
                })
                .assertReply(function (activity, description) {
                    assert.strictEqual(activity.attachments[0].contentType, 'application/vnd.microsoft.card.adaptive');
					assert.deepStrictEqual(activity.attachments[0].content, localizationJsonFr);
                });

                return testNock.resolveWithMocks('localization_response_fr-fr', done, flow);
            });
        });
    });
	describe("it-it locale", function () {
            it("send conversationUpdate and check the card is received with the it-it locale", function (done) {
                botTestBase.getTestAdapterDefault().then((testAdapter) => {
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
                    locale: "it-it"
                })
                .assertReply(function (activity, description) {
                    assert.strictEqual(activity.attachments[0].contentType, 'application/vnd.microsoft.card.adaptive');
					assert.deepStrictEqual(activity.attachments[0].content, localizationJsonIt);
                });

                return testNock.resolveWithMocks('localization_response_it-it', done, flow);
            });
        });
    });
	describe("en-us locale", function () {
            it("send conversationUpdate and check the card is received with the en-us locale", function (done) {
                botTestBase.getTestAdapterDefault().then((testAdapter) => {
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
                    locale: "en-us"
                })
                .assertReply(function (activity, description) {
                    assert.strictEqual(activity.attachments[0].contentType, 'application/vnd.microsoft.card.adaptive');
					assert.deepStrictEqual(activity.attachments[0].content, localizationJson);
                });

                return testNock.resolveWithMocks('localization_response_en-us', done, flow);
            });
        });
    });
	describe("zh-cn locale", function () {
            it("send conversationUpdate and check the card is received with the zh-cn locale", function (done) {
                botTestBase.getTestAdapterDefault().then((testAdapter) => {
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
                    locale: "zh-cn"
                })
                .assertReply(function (activity, description) {
                    assert.strictEqual(activity.attachments[0].contentType, 'application/vnd.microsoft.card.adaptive');
					assert.deepStrictEqual(activity.attachments[0].content, localizationJsonZh);
                });

                return testNock.resolveWithMocks('localization_response_zh-cn', done, flow);
            });
        });
    });    
});