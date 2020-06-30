// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Tests.Responses;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests
{
    [TestClass]
    [TestCategory("UnitTests")]
    [ExcludeFromCodeCoverageAttribute]
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

        [TestMethod]
        public void GetCardResponseTest()
        {
            var card = new Card { Name = "Test" };
            var responseManager = new ResponseManager(
                                    new string[] { "en", "es" },
                                    new TestResponses());

            var responseActivity = responseManager.GetCardResponse(card);
            Assert.IsNotNull(responseActivity);
        }

        [TestMethod]
        public void GetCardResponseIEnumerableTest()
        {
            var card = new List<Card>
            {
                new Card { Name = "Test" },
                new Card { Name = "Test" },
            };
            var responseManager = new ResponseManager(
                                    new string[] { "en", "es" },
                                    new TestResponses());

            var responseActivity = responseManager.GetCardResponse(card);
            Assert.IsNotNull(responseActivity);
            Assert.AreEqual(2, responseActivity.Attachments.Count);
        }

        [TestMethod]
        public void GetCardResponseTemplateIDTest()
        {
            var card = new Card { Name = "Test" };
            var responseManager = new ResponseManager(
                                    new string[] { "en", "es" },
                                    new TestResponses());

            var responseActivity = responseManager.GetCardResponse(TestResponses.GetResponseText, card, new StringDictionary { { "T", "Test" } });
            Assert.IsNotNull(responseActivity);
            Assert.AreEqual(1, responseActivity.Attachments.Count);
        }

        [TestMethod]
        public void FormatResponseTest()
        {
            var card = new Card { Name = "Test" };
            var responseManager = new ResponseManager(
                                    new string[] { "en", "es" },
                                    new TestResponses());

            var result = responseManager.Format("Replace {Testing}, with token", new StringDictionary { { "Testing", "Test" } });
            Assert.IsNotNull(result);
            Assert.IsTrue(!result.Contains("{Testing}"));
            Assert.IsTrue(result.Contains("Test"));
        }
    }
}