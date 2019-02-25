using AdaptiveCards;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseBotSampleTests
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
                    Assert.IsTrue(card.Body.Any(i => i.Type == "TextBlock" && ((AdaptiveTextBlock)i).Text == "¡Bienvenido a Bot Framework!"));
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
                    Assert.IsTrue(card.Body.Any(i => i.Type == "TextBlock" && ((AdaptiveTextBlock)i).Text == "Willkommen bei Bot Framework!"));
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
                    Assert.IsTrue(card.Body.Any(i => i.Type == "TextBlock" && ((AdaptiveTextBlock)i).Text == "Bienvenue à Bot Framework!"));
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
                    Assert.IsTrue(card.Body.Any(i => i.Type == "TextBlock" && ((AdaptiveTextBlock)i).Text == "Benvenuti a Bot Framework!"));
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
                    Assert.IsTrue(card.Body.Any(i => i.Type == "TextBlock" && ((AdaptiveTextBlock)i).Text == "欢迎来到博特框架!"));
                })
                .StartTestAsync();
        }

    }
}
