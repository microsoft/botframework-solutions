using System;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests.Resources
{
    [TestClass]
    public class ResponseGenerationTests
    {
        [TestMethod]
        public void GetResponseUsingGeneratedAccessor()
        {
            var response = TestResponses.GetResponseText;
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