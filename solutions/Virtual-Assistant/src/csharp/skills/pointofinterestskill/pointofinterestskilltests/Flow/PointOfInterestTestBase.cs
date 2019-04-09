using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Shared.Responses;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Builder.Solutions.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PointOfInterestSkill.Bots;
using PointOfInterestSkill.Dialogs;
using PointOfInterestSkill.Responses.CancelRoute;
using PointOfInterestSkill.Responses.Route;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Services;
using System.Collections.Generic;
using System.Threading;

namespace PointOfInterestSkillTests.Flow
{
    public class PointOfInterestTestBase : BotTestBase
    {
        public IServiceCollection Services { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            // Initialize service collection
            Services = new ServiceCollection();
            Services.AddSingleton(new BotSettings());
            Services.AddSingleton(new BotServices()
            {
                CognitiveModelSets = new Dictionary<string, CognitiveModelSet>
                {
                    {
                        "en", new CognitiveModelSet()
                        {
                            LuisServices = new Dictionary<string, IRecognizer>
                            {
                            }
                        }
                    }
                }
            });

            Services.AddSingleton<IBotTelemetryClient, NullBotTelemetryClient>();
            Services.AddSingleton(new UserState(new MemoryStorage()));
            Services.AddSingleton(new ConversationState(new MemoryStorage()));
            Services.AddSingleton(new ProactiveState(new MemoryStorage()));
            Services.AddSingleton(sp =>
            {
                var userState = sp.GetService<UserState>();
                var conversationState = sp.GetService<ConversationState>();
                var proactiveState = sp.GetService<ProactiveState>();
                return new BotStateSet(userState, conversationState);
            });

            ResponseManager = new ResponseManager(
                locales: new string[] { "en", "de", "es", "fr", "it", "zh" },
                responseTemplates: new IResponseIdCollection[]
                {
                    new POISharedResponses(),
                    new RouteResponses(),
                    new CancelRouteResponses()
                });
            Services.AddSingleton(ResponseManager);

            Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            Services.AddSingleton<TestAdapter, DefaultTestAdapter>();
            Services.AddTransient<MainDialog>();
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