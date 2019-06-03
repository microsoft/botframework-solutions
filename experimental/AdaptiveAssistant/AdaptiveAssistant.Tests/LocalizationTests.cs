// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptiveAssistant.Tests
{
    [TestClass]
    public class LocalizationTests : BotTestBase
    {
        [TestMethod]
        public async Task Test_Localization_Spanish()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("es-mx");

            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("bot") }
                })
                .AssertReply(activity =>
                {
                    // Assert there is a card in the message
                    Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count);

                    // Assert the intro card has been localized
                    var card = activity.AsMessageActivity().Attachments[0].Content as AdaptiveCard;
                    Assert.IsTrue(card.Body.Any(i => i.Type == "Container" && ((AdaptiveContainer)i).Items.Any(t => t.Type == "TextBlock" && ((AdaptiveTextBlock)t).Text == "Hola, soy tu Virtual Assistant")));
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Localization_German()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("de-de");

            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("bot") }
                })
                .AssertReply(activity =>
                {
                    // Assert there is a card in the message
                    Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count);

                    // Assert the intro card has been localized
                    var card = activity.AsMessageActivity().Attachments[0].Content as AdaptiveCard;
                    Assert.IsTrue(card.Body.Any(i => i.Type == "Container" && ((AdaptiveContainer)i).Items.Any(t => t.Type == "TextBlock" && ((AdaptiveTextBlock)t).Text == "Hi, ich bin **dein** Virtueller Assistent")));
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Localization_French()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-fr");

            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("bot") }
                })
                .AssertReply(activity =>
                {
                    // Assert there is a card in the message
                    Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count);

                    // Assert the intro card has been localized
                    var card = activity.AsMessageActivity().Attachments[0].Content as AdaptiveCard;
                    Assert.IsTrue(card.Body.Any(i => i.Type == "Container" && ((AdaptiveContainer)i).Items.Any(t => t.Type == "TextBlock" && ((AdaptiveTextBlock)t).Text == "Salut, je suis votre Virtual Assistant")));
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Localization_Italian()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("it-it");

            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("bot") }
                })
                .AssertReply(activity =>
                {
                    // Assert there is a card in the message
                    Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count);

                    // Assert the intro card has been localized
                    var card = activity.AsMessageActivity().Attachments[0].Content as AdaptiveCard;
                    Assert.IsTrue(card.Body.Any(i => i.Type == "Container" && ((AdaptiveContainer)i).Items.Any(t => t.Type == "TextBlock" && ((AdaptiveTextBlock)t).Text == "Ciao, sono il **tuo** Virtual Assistant")));
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Localization_Chinese()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("zh-cn");

            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("bot") }
                })
                .AssertReply(activity =>
                {
                    // Assert there is a card in the message
                    Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count);

                    // Assert the intro card has been localized
                    var card = activity.AsMessageActivity().Attachments[0].Content as AdaptiveCard;
                    Assert.IsTrue(card.Body.Any(i => i.Type == "Container" && ((AdaptiveContainer)i).Items.Any(t => t.Type == "TextBlock" && ((AdaptiveTextBlock)t).Text == "嗨, 我是你的虚拟助理")));
                })
                .StartTestAsync();
        }
    }
}
