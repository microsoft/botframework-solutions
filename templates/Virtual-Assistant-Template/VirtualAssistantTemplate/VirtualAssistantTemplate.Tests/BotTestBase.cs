using System.Collections.Generic;
using System.Threading;
using VirtualAssistantTemplate.Tests.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualAssistantTemplate.Services;
using VirtualAssistantTemplate.Bots;
using VirtualAssistantTemplate.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Shared.Telemetry;
using Microsoft.Bot.Builder.Solutions.Testing;

namespace VirtualAssistantTemplate.Tests
{
    public class BotTestBase
    {
        private IServiceCollection services;

        [TestInitialize]
        public virtual void Initialize()
        {
            services = new ServiceCollection();
            services.AddSingleton(new BotSettings());
            services.AddSingleton(new BotServices()
            {
                CognitiveModelSets = new Dictionary<string, CognitiveModelSet>
                {
                    { "en", new CognitiveModelSet
                        {
                            DispatchService = DispatchTestUtil.CreateRecognizer(),
                            LuisServices = new Dictionary<string, IRecognizer>
                            {
                                { "general", GeneralTestUtil.CreateRecognizer() }
                            },
                            QnAServices = new Dictionary<string, ITelemetryQnAMaker>
                            {
                                { "faq", FaqTestUtil.CreateRecognizer() },
                                { "chitchat", ChitchatTestUtil.CreateRecognizer() }
                            }
                        }
                    }
                }
            });

            services.AddSingleton<IBotTelemetryClient, NullBotTelemetryClient>();
            services.AddSingleton(new MicrosoftAppCredentials("appId", "password"));
            services.AddSingleton(new UserState(new MemoryStorage()));
            services.AddSingleton(new ConversationState(new MemoryStorage()));
            services.AddSingleton(sp =>
            {
                var userState = sp.GetService<UserState>();
                var conversationState = sp.GetService<ConversationState>();
                return new BotStateSet(userState, conversationState);
            });

            services.AddSingleton<TestAdapter, DefaultTestAdapter>();
            services.AddTransient<MainDialog>();
            services.AddTransient<IBot, DialogBot<MainDialog>>();
        }

        public TestFlow GetTestFlow()
        {
            var sp = services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = sp.GetService<IBot>();
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }
    }
}
