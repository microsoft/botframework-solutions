// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkillSample.Responses.Main;
using SkillSample.Responses.Shared;

namespace SkillSample.Tests
{
    [TestClass]
    public class LocalizationTests : SkillTestBase
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
                    var messageActivity = activity.AsMessageActivity();
                    CollectionAssert.Contains(ParseReplies(MainResponses.WelcomeMessage, new StringDictionary()), messageActivity.Text);
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
                    var messageActivity = activity.AsMessageActivity();
                    CollectionAssert.Contains(ParseReplies(MainResponses.WelcomeMessage, new StringDictionary()), messageActivity.Text);
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
                    var messageActivity = activity.AsMessageActivity();
                    CollectionAssert.Contains(ParseReplies(MainResponses.WelcomeMessage, new StringDictionary()), messageActivity.Text);
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
                    var messageActivity = activity.AsMessageActivity();
                    CollectionAssert.Contains(ParseReplies(MainResponses.WelcomeMessage, new StringDictionary()), messageActivity.Text);
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
                    var messageActivity = activity.AsMessageActivity();
                    CollectionAssert.Contains(ParseReplies(MainResponses.WelcomeMessage, new StringDictionary()), messageActivity.Text);
                })
                .StartTestAsync();
        }
    }
}
