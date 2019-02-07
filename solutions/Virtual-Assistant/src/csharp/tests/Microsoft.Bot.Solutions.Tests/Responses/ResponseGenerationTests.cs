using System;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Tests.Responses;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests
{
    [TestClass]
    public class ResponseGenerationTests
    {
        [TestMethod]
        public void GetResponseUsingGeneratedAccessor()
        {
            var responseManager = new ResponseManager(
                new IResponseIdCollection[] { new TestResponses() }, 
                new string[] { "en", "es" });
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