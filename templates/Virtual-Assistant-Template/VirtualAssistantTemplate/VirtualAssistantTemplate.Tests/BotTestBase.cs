using System.Collections.Generic;
using System.Threading;
using Autofac;
using VirtualAssistantTemplate.Tests.LuisTestUtils;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualAssistantTemplate.Services;
using VirtualAssistantTemplate.Bots;
using VirtualAssistantTemplate.Dialogs;

namespace VirtualAssistantTemplate.Tests
{
    public class BotTestBase
    {
        public IContainer Container { get; set; }

        public BotServices BotServices { get; set; }

        public ConversationState ConversationState { get; set; }

        public UserState UserState { get; set; }

        public IBotTelemetryClient TelemetryClient { get; set; }

        [TestInitialize]
        public virtual void Initialize()
        {
            var builder = new ContainerBuilder();

            ConversationState = new ConversationState(new MemoryStorage());
            UserState = new UserState(new MemoryStorage());
            TelemetryClient = new NullBotTelemetryClient();
            BotServices = new BotServices()
            {
                CognitiveModelSets = new Dictionary<string, CognitiveModelSet>
                {
                    {"en", new CognitiveModelSet
                        {
                            DispatchService = DispatchTestUtil.CreateRecognizer(),
                            LuisServices = new Dictionary<string, IRecognizer>
                            {
                                { "general", GeneralTestUtil.CreateRecognizer() }
                            },
                            QnAServices = null
                        }
                    }
                }
            };

            builder.RegisterInstance(new BotStateSet(UserState, ConversationState));
            Container = builder.Build();
        }

        public TestFlow GetTestFlow()
        {
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(UserState, ConversationState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = BuildBot();
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        public IBot BuildBot()
        {
            // TODO: need to mock IServiceProvider to fix tests
            return new DialogBot<MainDialog>(null, null);
        }
    }
}
