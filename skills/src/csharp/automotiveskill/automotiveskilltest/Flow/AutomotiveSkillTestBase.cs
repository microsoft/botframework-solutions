using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Builder.Solutions.Testing;
using AutomotiveSkill.Responses.VehicleSettings;
using AutomotiveSkill.Responses.Shared;
using AutomotiveSkill.Responses.Main;
using AutomotiveSkill.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Solutions;
using AutomotiveSkill.Services;
using AutomotiveSkill.Dialogs;
using AutomotiveSkill.Bots;
using Microsoft.Bot.Builder.Solutions.Responses;
using AutomotiveSkillTest.Flow.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Bot.Builder.AI.Luis;

namespace AutomotiveSkillTest.Flow
{
    public class AutomotiveSkillTestBase : BotTestBase
    {
        public IServiceCollection Services { get; set; }

        public string ImageAssetLocation { get; set; } = "http://localhost";

        [TestInitialize]
        public override void Initialize()
        {
            // Initialize service collection
            Services = new ServiceCollection();
            Services.AddSingleton(new BotSettings()
            {
                Properties = new Dictionary<string, string>()
                {
                    { "ImageAssetLocation", ImageAssetLocation }
                }
            });

            Services.AddSingleton(new BotServices()
            {
                CognitiveModelSets = new Dictionary<string, CognitiveModelSet>
                {
                    {
                        "en", new CognitiveModelSet()
                        {
                            LuisServices = new Dictionary<string, ITelemetryRecognizer>
                            {
                                { "general", new MockLuisRecognizer() },
                                { "settings", new MockLuisRecognizer() },
                                { "settings_name", new MockLuisRecognizer() },
                                { "settings_value", new MockLuisRecognizer() }
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
                new string[] { "en", "de", "es", "fr", "it", "zh" },
                new AutomotiveSkillMainResponses(),
                new AutomotiveSkillSharedResponses(),
                new VehicleSettingsResponses());
            Services.AddSingleton(ResponseManager);

            Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            // Services.AddSingleton<IServiceManager>(ServiceManager);
            Services.AddSingleton<TestAdapter, DefaultTestAdapter>();
            Services.AddTransient<MainDialog>();
			Services.AddTransient<VehicleSettingsDialog>();
			Services.AddTransient<IBot, DialogBot<MainDialog>>();


            // Mock HttpContext for image path resolution
            var mockHttpContext = new DefaultHttpContext();
            mockHttpContext.Request.Scheme = "http";
            mockHttpContext.Request.Host = new HostString("localhost", 3980);

            var mockHttpContextAcessor = new HttpContextAccessor
            {
                HttpContext = mockHttpContext
            };

            Services.AddSingleton<IHttpContextAccessor>(mockHttpContextAcessor);
        }

        public TestFlow GetTestFlow()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();
            var conversationState = sp.GetService<ConversationState>();
            var stateAccessor = conversationState.CreateProperty<AutomotiveSkillState>(nameof(AutomotiveSkillState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = sp.GetService<IBot>();
                var state = await stateAccessor.GetAsync(context, () => new AutomotiveSkillState());
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }
    }
}