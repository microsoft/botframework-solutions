using EnterpriseBotSample.Dialogs.Main.Resources;
using EnterpriseBotSampleTests.Utterances;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnterpriseBotSampleTests
{
    [TestClass]
    public class MainDialogTests : BotTestBase
    {
        [TestMethod]
        public async Task Test_Intro_Message()
        {
            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("bot") }
                })
                .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Greeting_Message()
        {
            await GetTestFlow()
                .Send(ChitchatUtterances.Greeting)
                .AssertReply("Hello.")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Help_Intent()
        {
            await GetTestFlow()
                .Send(GeneralUtterances.Help)
                .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Escalate_Intent()
        {
            await GetTestFlow()
                .Send(GeneralUtterances.Escalate)
                .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Unhandled_Message()
        {
            await GetTestFlow()
                .Send("Unhandled message")
                .AssertReply(MainStrings.CONFUSED)
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_FAQ()
        {
            await GetTestFlow()
                .Send(FaqUtterances.Overview)
                .AssertReply("Creation of a high quality conversational experience requires a foundational set of capabilities. To help you succeed with building great conversational experiences, we have created an **Enterprise Bot Template**. This template brings together all of the best practices and supporting components we've identified through building of conversational experiences.\n[Learn more](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-enterprise-template-overview?view=azure-bot-service-4.0)")
                .StartTestAsync();
        }
    }
}