// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
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
    public class DatePromptTests
    {
        private static TimeZoneInfo mockUserTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");

        [TestMethod]
        public async Task DatePromptWithRelativeDate()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            TestAdapter adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            // Create new DialogSet.
            var dialogs = new DialogSet(dialogState);

            // Create and add number prompt to DialogSet.
            var dateTimePrompt = new DatePrompt("DatePrompt", defaultLocale: Culture.English);
            dialogs.Add(dateTimePrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync();
                if (results.Status == DialogTurnStatus.Empty)
                {
                    var options = new DatePromptOptions
                    {
                        Prompt = new Activity { Type = ActivityTypes.Message, Text = "What date would you like?" },
                        TimeZone = mockUserTimeZone
                    };
                    await dc.PromptAsync("DatePrompt", options, cancellationToken);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    var resolution = ((IList<DateTimeResolution>)results.Result).First();
                    var reply = MessageFactory.Text($"Value:'{resolution.Value}'");
                    await turnContext.SendActivityAsync(reply, cancellationToken);
                }
            })
            .Send("hello")
            .AssertReply("What date would you like?")
            .Send("today")
            .AssertReply(TodayResponse())
            .StartTestAsync();
        }

        public string TodayResponse()
        {
            var userTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mockUserTimeZone);
            return $"Value:'{string.Format("{0:yyyy-MM-dd}", userTime.Date)}'";
        }
    }
}