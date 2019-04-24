using System.Collections.Generic;
using System.Threading;
using $safeprojectname$.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using $ext_safeprojectname$.Services;
using $ext_safeprojectname$.Bots;
using $ext_safeprojectname$.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Testing;
using Microsoft.Bot.Builder.Solutions.Responses;
using $ext_safeprojectname$.Responses.Main;
using $ext_safeprojectname$.Responses.Shared;
using $ext_safeprojectname$.Responses.Sample;
using Microsoft.Bot.Builder.Solutions.Middleware;
using $safeprojectname$.Mocks;

namespace $safeprojectname$
{
    public class SkillTestBase : BotTestBase
    {
        public IServiceCollection Services { get; set; }

        [TestInitialize]
        public virtual void Initialize()
        {
            Services = new ServiceCollection();
            Services.AddSingleton(new BotSettings());
            Services.AddSingleton(new BotServices()
            {
                CognitiveModelSets = new Dictionary<string, CognitiveModelSet>
                {
                    { "en", new CognitiveModelSet
                        {
                            LuisServices = new Dictionary<string, IRecognizer>
                            {
                                { "general", GeneralTestUtil.CreateRecognizer() },
                                { "$ext_safeprojectname$", SkillTestUtil.CreateRecognizer() }
                            }
                        }
                    }
                }
            });

            Services.AddSingleton<IBotTelemetryClient, NullBotTelemetryClient>();
            Services.AddSingleton(new MicrosoftAppCredentials("appId", "password"));
            Services.AddSingleton(new UserState(new MemoryStorage()));
            Services.AddSingleton(new ConversationState(new MemoryStorage()));
            Services.AddSingleton(sp =>
            {
                var userState = sp.GetService<UserState>();
                var conversationState = sp.GetService<ConversationState>();
                return new BotStateSet(userState, conversationState);
            });

            ResponseManager = new ResponseManager(
                new string[] { "en", "de", "es", "fr", "it", "zh" },
                new MainResponses(),
                new SharedResponses(),
                new SampleResponses());

            Services.AddSingleton(ResponseManager);
            Services.AddTransient<MainDialog>();
            Services.AddTransient<SampleDialog>();
            Services.AddSingleton<TestAdapter, DefaultTestAdapter>();
            Services.AddTransient<IBot, DialogBot<MainDialog>>();
        }

        public TestFlow GetTestFlow()
        {
            var sp = Services.BuildServiceProvider();
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