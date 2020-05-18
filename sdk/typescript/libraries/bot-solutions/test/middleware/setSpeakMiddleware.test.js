/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { MemoryStorage, ConversationState, TestAdapter } = require("botbuilder");
const { SetSpeakMiddleware } = require("../../lib/middleware");
const { ActivityEx } = require("../../lib/extensions/activityEx");
const { xml2js } = require("xml-js");
const { Channels } = require("botframework-schema");
 
describe("setSpeak middleware", function() {
    it("should default options to default", async function() {
        const storage = new MemoryStorage();
        const convState = new ConversationState(storage);

        const response = "Response";

        const testAdapter = new TestAdapter(async function(context) {
            context.activity.channelId = Channels.DirectlineSpeech;
            const reply = ActivityEx.createReply(context.activity, response, '');
            await context.sendActivity(reply);
        })
        .use(new SetSpeakMiddleware());

        await testAdapter.send("foo")
        .assertReply((activity) => {
            const elements = xml2js(activity.speak, { compact: false });
            const rootElement = elements.elements[0];
            strictEqual(rootElement.name, "speak");
            strictEqual(rootElement.attributes["xml:lang"], "en-us");
            const voiceElement = rootElement.elements[0];
            strictEqual(voiceElement.attributes.name, "Microsoft Server Speech Text to Speech Voice (en-US, Jessa24kRUS)");
            strictEqual(voiceElement.elements[0].text, response);
        })
        .startTest();
    });

    it("should default options to default", async function() {
        const storage = new MemoryStorage();
        const convState = new ConversationState(storage);

        const response = "Response";

        const testAdapter = new TestAdapter(async function(context) {
            context.activity.channelId = Channels.DirectlineSpeech;
            const reply = ActivityEx.createReply(context.activity, response, 'invalidLocale');
            await context.sendActivity(reply);
        })
        .use(new SetSpeakMiddleware());

        await testAdapter.send("foo")
        .assertReply((activity) => {
            const elements = xml2js(activity.speak, { compact: false });
            const rootElement = elements.elements[0];
            strictEqual(rootElement.name, "speak");
            strictEqual(rootElement.attributes["xml:lang"], "en-us");
            const voiceElement = rootElement.elements[0];
            strictEqual(voiceElement.attributes.name, "Microsoft Server Speech Text to Speech Voice (en-US, Jessa24kRUS)");
            strictEqual(voiceElement.elements[0].text, response);
        })
        .startTest();
    });

    it("should default options to incorrect case", async function() {
        const storage = new MemoryStorage();
        const convState = new ConversationState(storage);

        const response = "Response";

        const testAdapter = new TestAdapter(async function(context) {
            context.activity.channelId = Channels.DirectlineSpeech;
            const reply = ActivityEx.createReply(context.activity, response, 'zh-cn');
            await context.sendActivity(reply);
        }).use(new SetSpeakMiddleware()).send("foo")
            .assertReply((activity) => {
                const elements = xml2js(activity.speak, { compact: false });
                const rootElement = elements.elements[0];
                strictEqual(rootElement.name, "speak");
                strictEqual(rootElement.attributes["xml:lang"], "zh-cn");
                const voiceElement = rootElement.elements[0];
                strictEqual(voiceElement.attributes.name, "Microsoft Server Speech Text to Speech Voice (zh-CN, HuihuiRUS)");
                strictEqual(voiceElement.elements[0].text, response);
            })
            .startTest();
    });
});
