using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Declarative.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.LanguageGeneration;

namespace AdaptiveCalendarSkill.Tests
{
    [TestClass]
    public class CreateEventTests
    {
        private TestFlow CreateFlow(AdaptiveDialog ruleDialog)
        {
           TypeFactory.Configuration = new ConfigurationBuilder().Build();

            var explorer = new ResourceExplorer();
            var storage = new MemoryStorage();
            var convoState = new ConversationState(storage);
            var userState = new UserState(storage);

            var adapter = new TestAdapter(TestAdapter.CreateConversation(""));
            adapter
                .UseStorage(storage)
                .UseState(userState, convoState)
                .Use(new RegisterClassMiddleware<ResourceExplorer>(explorer))
                .UseAdaptiveDialogs()
                .UseLanguageGeneration(explorer)
                .Use(new TranscriptLoggerMiddleware(new FileTranscriptLogger()));

            DialogManager dm = new DialogManager(ruleDialog);
            return new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                await dm.OnTurnAsync(turnContext, cancellationToken: cancellationToken).ConfigureAwait(false);
            });
        }

    }
}
