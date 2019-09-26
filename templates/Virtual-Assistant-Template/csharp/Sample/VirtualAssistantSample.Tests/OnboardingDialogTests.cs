// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace VirtualAssistantSample.Tests
{
    [TestClass]
    public class OnboardingDialogTests : BotTestBase
    {
        [TestMethod]
        public async Task Test_Onboarding_Flow()
        {
            var testName = "Jane Doe";

            dynamic data = new JObject();
            data.name = testName;

            await GetTestFlow()
                .Send(new Activity()
                {
                    ChannelId = Channels.Emulator,
                    Type = ActivityTypes.Event,
                    Value = new JObject(new JProperty("action", "startOnboarding"))
                })
                .AssertReply(TemplateEngine.EvaluateTemplate("namePrompt"))
                .Send(testName)
                .AssertReply(TemplateEngine.EvaluateTemplate("haveNameMessage", data))
                .StartTestAsync();
        }
    }
}
