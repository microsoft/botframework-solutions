﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace VirtualAssistantSample.Tests
{
    [TestClass]
    public class LocalizationTests : BotTestBase
    {
        [TestMethod]
        public async Task Test_Localization_Spanish()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("es-es");

            var allIntroCardTitleVariations = TemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("NewUserIntroCardTitle");

            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("user") }
                })
                .AssertReply(activity =>
                {
                    // Assert there is a card in the message
                    Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count);

                    // Assert the intro card has been localized
                    var content = JsonConvert.SerializeObject(activity.AsMessageActivity().Attachments[0].Content);
                    var card = AdaptiveCard.FromJson(content).Card;

                    Assert.IsTrue(card.Body.Any(i => i.Type == "Container" && ((AdaptiveContainer)i).Items.Any(t => t.Type == "TextBlock" && allIntroCardTitleVariations.Contains(((AdaptiveTextBlock)t).Text))));
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Localization_German()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("de-de");

            var allIntroCardTitleVariations = TemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("NewUserIntroCardTitle");

            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("user") }
                })
                .AssertReply(activity =>
                {
                    // Assert there is a card in the message
                    Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count);

                    // Assert the intro card has been localized
                    var content = JsonConvert.SerializeObject(activity.AsMessageActivity().Attachments[0].Content);
                    var card = AdaptiveCard.FromJson(content).Card;

                    Assert.IsTrue(card.Body.Any(i => i.Type == "Container" && ((AdaptiveContainer)i).Items.Any(t => t.Type == "TextBlock" && allIntroCardTitleVariations.Contains(((AdaptiveTextBlock)t).Text))));
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Localization_French()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-fr");

            var allIntroCardTitleVariations = TemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("NewUserIntroCardTitle");

            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("user") }
                })
                .AssertReply(activity =>
                {
                    // Assert there is a card in the message
                    Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count);

                    // Assert the intro card has been localized
                    var content = JsonConvert.SerializeObject(activity.AsMessageActivity().Attachments[0].Content);
                    var card = AdaptiveCard.FromJson(content).Card;

                    Assert.IsTrue(card.Body.Any(i => i.Type == "Container" && ((AdaptiveContainer)i).Items.Any(t => t.Type == "TextBlock" && allIntroCardTitleVariations.Contains(((AdaptiveTextBlock)t).Text))));
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Localization_Italian()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("it-it");

            var allIntroCardTitleVariations = TemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("NewUserIntroCardTitle");

            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("user") }
                })
                .AssertReply(activity =>
                {
                    // Assert there is a card in the message
                    Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count);

                    // Assert the intro card has been localized
                    var content = JsonConvert.SerializeObject(activity.AsMessageActivity().Attachments[0].Content);
                    var card = AdaptiveCard.FromJson(content).Card;

                    Assert.IsTrue(card.Body.Any(i => i.Type == "Container" && ((AdaptiveContainer)i).Items.Any(t => t.Type == "TextBlock" && allIntroCardTitleVariations.Contains(((AdaptiveTextBlock)t).Text))));
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Localization_Chinese()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("zh-cn");

            var allIntroCardTitleVariations = TemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("NewUserIntroCardTitle");

            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("user") }
                })
                .AssertReply(activity =>
                {
                    // Assert there is a card in the message
                    Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count);

                    // Assert the intro card has been localized
                    var content = JsonConvert.SerializeObject(activity.AsMessageActivity().Attachments[0].Content);
                    var card = AdaptiveCard.FromJson(content).Card;

                    Assert.IsTrue(card.Body.Any(i => i.Type == "Container" && ((AdaptiveContainer)i).Items.Any(t => t.Type == "TextBlock" && allIntroCardTitleVariations.Contains(((AdaptiveTextBlock)t).Text))));
                })
                .StartTestAsync();
        }
    }
}
