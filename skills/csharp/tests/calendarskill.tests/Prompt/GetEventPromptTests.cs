// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Prompts;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Test.Flow.Fakes;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkill.Test.Prompt
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class GetEventPromptTests
    {
        private static TimeZoneInfo mockUserTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");

        [TestMethod]
        public async Task BasicGetEventPromptTest()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            TestAdapter adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            // Create new DialogSet.
            var dialogs = new DialogSet(dialogState);

            // Create and add number prompt to DialogSet.
            var getEventPrompt = new GetEventPrompt("GetEventPrompt", defaultLocale: Culture.English);
            dialogs.Add(getEventPrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);
                var service = new MockCalendarService(MockCalendarService.FakeDefaultEvents());

                var results = await dc.ContinueDialogAsync();
                if (results.Status == DialogTurnStatus.Empty)
                {
                    var options = new GetEventOptions(service, mockUserTimeZone)
                    {
                        Prompt = new Activity { Type = ActivityTypes.Message, Text = "Which event you are searching?" },
                    };
                    await dc.PromptAsync("GetEventPrompt", options, cancellationToken);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    var resolution = (IList<EventModel>)results.Result;
                    var reply = MessageFactory.Text($"Get {resolution.Count} event");
                    await turnContext.SendActivityAsync(reply, cancellationToken);
                }
            })
            .Send("hello")
            .AssertReply("Which event you are searching?")
            .Send("today")
            .AssertReply(GetEventResponse())
            .StartTestAsync();
        }

        public string GetEventResponse()
        {
            return "Get 1 event";
        }
    }
}