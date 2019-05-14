/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const skillTestBase = require('./skillTestBase');
const testNock = require('../testBase');

describe("localization", function() {
    beforeEach(async function() {
        await skillTestBase.initialize();
    });

	describe("de locale", function () {
		it("send conversationUpdate and check the intro message is received with the de locale", function (done) {
			const testAdapter = skillTestBase.getTestAdapter();
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
				locale: 'de'
			})
			.assertReply('[Geben Sie Ihre Intro-Nachricht ein.]');

			return testNock.resolveWithMocks('localization_de_response', done, flow);
		});
	});

	describe("es locale", function () {
		it("send conversationUpdate and check the intro message is received with the es locale", function (done) {
			const testAdapter = skillTestBase.getTestAdapter();
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
			.assertReply('[Introduzca aquí su mensaje introductorio]');

			return testNock.resolveWithMocks('localization_es_response', done, flow);
		});
	});

	describe("fr locale", function () {
		it("send conversationUpdate and check the intro message is received with the fr locale", function (done) {
			const testAdapter = skillTestBase.getTestAdapter();
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
			.assertReply(`[Entrez votre message d'intro ici]`);

			return testNock.resolveWithMocks('localization_fr_response', done, flow);
		});
	});

	describe("it locale", function () {
		it("send conversationUpdate and check the intro message is received with the it locale", function (done) {
			const testAdapter = skillTestBase.getTestAdapter();
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
			.assertReply('[Inserisci qui il tuo messaggio introduttivo]');

			return testNock.resolveWithMocks('localization_it_response', done, flow);
		});
	});

	describe("en locale", function () {
		it("send conversationUpdate and check the intro message is received with the en locale", function (done) {
			const testAdapter = skillTestBase.getTestAdapter();
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
			.assertReply('[Enter your intro message here]');

			return testNock.resolveWithMocks('localization_en_response', done, flow);
		});
	});

	describe("zh locale", function () {
		it("send conversationUpdate and check the intro message is received with the zh locale", function (done) {
			const testAdapter = skillTestBase.getTestAdapter();
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
			.assertReply('[在此输入您的简介信息]');

			return testNock.resolveWithMocks('localization_zh_response', done, flow);
		});
	});
});