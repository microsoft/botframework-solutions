using System.Collections.Generic;
using Autofac;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Builder.Solutions.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Bot.Builder.Skills;

namespace Microsoft.Bot.Builder.Solutions.Tests.Skills
{
    [TestClass]
    public class SkillTestBase : BotTestBase
    {
        public DialogSet Dialogs { get; set; }

        public UserState UserState { get; set; }

        public ConversationState ConversationState { get; set; }

        public ProactiveState ProactiveState { get; set; }

        public IStatePropertyAccessor<DialogState> DialogState { get; set; }

        public IBotTelemetryClient TelemetryClient { get; set; }
        
        public IBackgroundTaskQueue BackgroundTaskQueue { get; set; }

        public EndpointService EndpointService { get; set; }

        public ConversationReference ConversationReference { get; set; }

        [TestInitialize]
        public new void Initialize()
        {
            var builder = new ContainerBuilder();

            ConversationState = new ConversationState(new MemoryStorage());
            DialogState = ConversationState.CreateProperty<DialogState>(nameof(DialogState));
            UserState = new UserState(new MemoryStorage());
            ProactiveState = new ProactiveState(new MemoryStorage());
            TelemetryClient = new NullBotTelemetryClient();
            BackgroundTaskQueue = new BackgroundTaskQueue();
            EndpointService = new EndpointService();

            builder.RegisterInstance(new BotStateSet(UserState, ConversationState));
            Container = builder.Build();

            Dialogs = new DialogSet(DialogState);
        
        }

        /// <summary>
        /// Create a TestFlow which spins up a CustomSkillDialog ready for the tests to execute against
        /// </summary>
        /// <param name="locale"></param>
        /// <param name="overrideSkillDialogOptions"></param>
        /// <returns></returns>
        public TestFlow GetTestFlow(SkillDefinition skillDefinition, string locale = null)
        {
            var adapter = new TestAdapter(sendTraceActivity: false)
                .Use(new AutoSaveStateMiddleware(ConversationState));

            var testFlow = new TestFlow(adapter, async (context, cancellationToken) =>
            {
                var dc = await Dialogs.CreateContextAsync(context);

                if (dc.ActiveDialog != null)
                {
                    var result = await dc.ContinueDialogAsync();
                }
                else
                {
                    await dc.BeginDialogAsync(skillDefinition.Id, skillDefinition);
                    var result = await dc.ContinueDialogAsync();
                }
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            throw new System.NotImplementedException();
        }
    }
}