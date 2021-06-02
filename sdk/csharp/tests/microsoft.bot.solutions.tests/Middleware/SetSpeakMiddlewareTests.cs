// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Middleware;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests.Middleware
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class SetSpeakMiddlewareTests
    {
        [TestMethod]
        public async Task DefaultOptions_Default()
        {
            var storage = new MemoryStorage();
            var convState = new ConversationState(storage);

            var conversation = TestAdapter.CreateConversation("Name");
            conversation.ChannelId = Connector.Channels.DirectlineSpeech;

            var adapter = new TestAdapter(conversation)
                .Use(new SetSpeakMiddleware());
            adapter.Locale = string.Empty;

            var response = "Response";

            await new TestFlow(adapter, async (context, cancellationToken) =>
            {
                await context.SendActivityAsync(context.Activity.CreateReply(response));
            })
                .Send("foo")
                .AssertReply((reply) =>
                {
                    var activity = (Activity)reply;
                    var rootElement = XElement.Parse(activity.Speak);
                    Assert.AreEqual(rootElement.Name.LocalName, "speak");
                    Assert.AreEqual(rootElement.Attribute(XNamespace.Xml + "lang").Value, "en-US");
                    var voiceElement = rootElement.Element("voice");
                    Assert.AreEqual(voiceElement.Attribute("name").Value, "Microsoft Server Speech Text to Speech Voice (en-US, Jessa24kRUS)");
                    Assert.AreEqual(voiceElement.Value, response);
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task DefaultOptions_Invalid()
        {
            var storage = new MemoryStorage();
            var convState = new ConversationState(storage);

            var conversation = TestAdapter.CreateConversation("Name");
            conversation.ChannelId = Connector.Channels.DirectlineSpeech;

            var adapter = new TestAdapter(conversation)
                .Use(new SetSpeakMiddleware());
            adapter.Locale = "InvalidLocale";

            var response = "Response";

            await new TestFlow(adapter, async (context, cancellationToken) =>
            {
                await context.SendActivityAsync(context.Activity.CreateReply(response));
            })
                .Send("foo")
                .AssertReply((reply) =>
                {
                    var activity = (Activity)reply;
                    var rootElement = XElement.Parse(activity.Speak);
                    Assert.AreEqual(rootElement.Name.LocalName, "speak");
                    Assert.AreEqual(rootElement.Attribute(XNamespace.Xml + "lang").Value, "en-US");
                    var voiceElement = rootElement.Element("voice");
                    Assert.AreEqual(voiceElement.Attribute("name").Value, "Microsoft Server Speech Text to Speech Voice (en-US, Jessa24kRUS)");
                    Assert.AreEqual(voiceElement.Value, response);
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task DefaultOptions_IncorrectCase()
        {
            var storage = new MemoryStorage();
            var convState = new ConversationState(storage);

            var conversation = TestAdapter.CreateConversation("Name");
            conversation.ChannelId = Connector.Channels.DirectlineSpeech;

            var adapter = new TestAdapter(conversation)
                .Use(new SetSpeakMiddleware());
            adapter.Locale = "zh-cn";

            var response = "Response";

            await new TestFlow(adapter, async (context, cancellationToken) =>
            {
                await context.SendActivityAsync(context.Activity.CreateReply(response));
            })
                .Send("foo")
                .AssertReply((reply) =>
                {
                    var activity = (Activity)reply;
                    var rootElement = XElement.Parse(activity.Speak);
                    Assert.AreEqual(rootElement.Name.LocalName, "speak");
                    Assert.AreEqual(rootElement.Attribute(XNamespace.Xml + "lang").Value, "zh-CN");
                    var voiceElement = rootElement.Element("voice");
                    Assert.AreEqual(voiceElement.Attribute("name").Value, "Microsoft Server Speech Text to Speech Voice (zh-CN, HuihuiRUS)");
                    Assert.AreEqual(voiceElement.Value, response);
                })
                .StartTestAsync();
        }
    }
}
