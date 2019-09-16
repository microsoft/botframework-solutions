// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualAssistantSample.Responses.Main;
using VirtualAssistantSample.Tests.Utterances;

namespace VirtualAssistantSample.Tests
{
    [TestClass]
    public class SummaryDialogTests : BotTestBase
    {
        [TestMethod]
        public async Task Test_SummaryEvent()
        {
            await GetTestFlow()
                .Send(this.SendSummaryEvent())
                .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
                .StartTestAsync();
        }

        public Activity SendSummaryEvent()
        {
            return new Activity(ActivityTypes.Event, name: "SummaryEvent");
        }

    }
}