/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License
 */

const assert = require('assert');
const botTestBase = require('./helpers/botTestBase');
const testNock = require('./helpers/testBase');
const localizationJsonDe = require('../src/content/NewUserGreeting.de.json');
const localizationJsonEs = require('../src/content/NewUserGreeting.es.json');
const localizationJsonFr = require('../src/content/NewUserGreeting.fr.json');
const localizationJsonIt = require('../src/content/NewUserGreeting.it.json');
const localizationJson = require('../src/content/NewUserGreeting.json');
const localizationJsonZh = require('../src/content/NewUserGreeting.zh.json');

describe("Localization", function() {
	describe("de locale", function () {
            it("send conversationUpdate and check the card is received with the de locale", function (done) {
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
                    locale: "de"
                })
                .assertReply(function (activity, description) {
                    assert.equal(activity.attachments[0].contentType, 'application/vnd.microsoft.card.adaptive');
					assert.deepEqual(activity.attachments[0].content, localizationJsonDe);
                });

                return testNock.resolveWithMocks('localization_response_de', done, flow);
            });
        });
    });
	describe("es locale", function () {
        it("send conversationUpdate and check the card is received with the es locale", function (done) {
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
                    locale: 'es-es'
                })
                .assertReply(function (activity, description) {
                    assert.equal(activity.attachments[0].contentType, 'application/vnd.microsoft.card.adaptive');
					assert.deepEqual(activity.attachments[0].content, localizationJsonEs);
                });

                return testNock.resolveWithMocks('localization_response_es', done, flow);
            });
        });
    });
	describe("fr locale", function () {
            it("send conversationUpdate and check the card is received with the fr locale", function (done) {
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
                    locale: 'fr'
                })
                .assertReply(function (activity, description) {
                    assert.equal(activity.attachments[0].contentType, 'application/vnd.microsoft.card.adaptive');
					assert.deepEqual(activity.attachments[0].content, localizationJsonFr);
                });

                return testNock.resolveWithMocks('localization_response_fr', done, flow);
            });
        });
    });
	describe("it locale", function () {
            it("send conversationUpdate and check the card is received with the it locale", function (done) {
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
                    locale: 'it'
                })
                .assertReply(function (activity, description) {
                    assert.equal(activity.attachments[0].contentType, 'application/vnd.microsoft.card.adaptive');
					assert.deepEqual(activity.attachments[0].content, localizationJsonIt);
                });

                return testNock.resolveWithMocks('localization_response_it', done, flow);
            });
        });
    });
	describe("en locale", function () {
            it("send conversationUpdate and check the card is received with the en locale", function (done) {
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
                    locale: 'en'
                })
                .assertReply(function (activity, description) {
                    assert.equal(activity.attachments[0].contentType, 'application/vnd.microsoft.card.adaptive');
					assert.deepEqual(activity.attachments[0].content, localizationJson);
                });

                return testNock.resolveWithMocks('localization_response_en', done, flow);
            });
        });
    });
	describe("zh locale", function () {
            it("send conversationUpdate and check the card is received with the zh locale", function (done) {
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
                    locale: 'zh'
                })
                .assertReply(function (activity, description) {
                    assert.equal(activity.attachments[0].contentType, 'application/vnd.microsoft.card.adaptive');
					assert.deepEqual(activity.attachments[0].content, localizationJsonZh);
                });

                return testNock.resolveWithMocks('localization_response_zh', done, flow);
            });
        });
    });    
});