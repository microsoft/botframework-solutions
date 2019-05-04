using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Tests.Responses;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.Solutions.Tests
{
    [TestClass]
    public class ResponseGenerationTests
    {
        [TestMethod]
        public void GetResponseUsingGeneratedAccessor()
        {
            var responseManager = new ResponseManager(
                new string[] { "en", "es" },
                new TestResponses());
            var response = responseManager.GetResponseTemplate(TestResponses.GetResponseText);
            Assert.AreEqual(InputHints.ExpectingInput, response.InputHint);
            Assert.AreEqual(response.SuggestedActions[0], "Suggestion 1");
            Assert.AreEqual(response.SuggestedActions[1], "Suggestion 2");

            var reply = response.Reply;
            Assert.AreEqual("The text", reply.Text);
            Assert.AreEqual("The speak", reply.Speak);
            Assert.AreEqual("The card text", reply.CardText);
        }
    }
}