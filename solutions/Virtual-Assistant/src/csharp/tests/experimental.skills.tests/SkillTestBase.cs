using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Builder.Solutions.Telemetry;
using Microsoft.Bot.Builder.Solutions.Testing;
using Microsoft.Bot.Builder.Solutions.Testing.Mocks;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestaurantBooking.Dialogs.Main.Resources;
using RestaurantBooking.Dialogs.Shared.Resources;

namespace Experimental.Skills.Tests
{
    /// <summary>
    /// Base class for Skill tests which prepare common configuration such as the LUIS mocks.
    /// </summary>
    [TestClass]
    public class SkillTestBase : BotTestBase
    {
        public DialogSet Dialogs { get; set; }

        public UserState UserState { get; set; }

        public ProactiveState ProactiveState { get; set; }

        public EndpointService EndpointService { get; set; }

        public IBackgroundTaskQueue BackgroundTaskQueue { get; set; }

        public ConversationState ConversationState { get; set; }

        public HttpContext MockHttpContext { get; set; }

        public HttpContextAccessor MockHttpContextAcessor { get; set; }

        public IStatePropertyAccessor<DialogState> DialogState { get; set; }

        public IBotTelemetryClient TelemetryClient { get; set; }

        public SkillConfigurationBase Services { get; set; }

        public Dictionary<string, SkillConfigurationBase> SkillConfigurations { get; set; }

        public SkillDialogOptions SkillDialogOptions { get; set; }

        public ConversationReference ConversationReference { get; set; }

        [TestInitialize]
        public new void Initialize()
        {
            var builder = new ContainerBuilder();

            this.ConversationState = new ConversationState(new MemoryStorage());
            this.DialogState = this.ConversationState.CreateProperty<DialogState>(nameof(this.DialogState));
            this.UserState = new UserState(new MemoryStorage());
            this.ProactiveState = new ProactiveState(new MemoryStorage());
            this.EndpointService = new EndpointService();
            this.TelemetryClient = new NullBotTelemetryClient();
            this.BackgroundTaskQueue = new BackgroundTaskQueue();
            this.SkillConfigurations = new Dictionary<string, SkillConfigurationBase>();

            // Add the LUIS model fakes used by the Skill
            this.Services = new MockSkillConfiguration();
            this.Services.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>
            {
                { "general", LuisTestUtils.GeneralTestUtil.CreateRecognizer() },
                { "restaurant", LuisTestUtils.RestaurantSkillTestUtil.CreateRecognizer() },
                { "news", LuisTestUtils.NewsSkillTestUtil.CreateRecognizer() }
            },
            });

            ResponseManager = new ResponseManager(
                Services.LocaleConfigurations.Keys.ToArray(),
                new RestaurantBookingSharedResponses(),
                new RestaurantBookingMainResponses());

            // Dummy Authentication connection for Auth testing
            this.Services.AuthenticationConnections = new Dictionary<string, string>
            {
                { "DummyAuth", "DummyAuthConnection" }
            };

            this.Services.Properties.Add("BingNewsKey", "DUMMY KEY");
            this.Services.Properties.Add("ImageAssetLocation", "http://localhost");

            builder.RegisterInstance(new BotStateSet(this.UserState, this.ConversationState));
            this.Container = builder.Build();

            this.Dialogs = new DialogSet(this.DialogState);

            // Manually mange the conversation metadata when we need finer grained control
            this.ConversationReference = new ConversationReference
            {
                ChannelId = "test",
                ServiceUrl = "https://test.com",
            };

            this.ConversationReference.User = new ChannelAccount("user1", "User1");
            this.ConversationReference.Bot = new ChannelAccount("bot", "Bot");
            this.ConversationReference.Conversation = new ConversationAccount(false, "convo1", "Conversation1");

            // Mock HttpContext for image path resolution
            MockHttpContext = new DefaultHttpContext();
            MockHttpContext.Request.Scheme = "http";
            MockHttpContext.Request.Host = new HostString("localhost", 3980);

            MockHttpContextAcessor = new HttpContextAccessor
            {
                HttpContext = MockHttpContext
            };
        }

        /// <summary>
        /// Create a SkillDefinition based on Skill Name and Skill Type.
        /// </summary>
        /// <param name="skillName">Skill Name.</param>
        /// <param name="skillType">Skill Type.</param>
        /// <returns>SkillDefinition.</returns>
        public SkillDefinition CreateSkillDefinition(string skillName, Type skillType)
        {
            var skillDefinition = new SkillDefinition
            {
                Assembly = skillType.AssemblyQualifiedName,
                Id = skillName,
                Name = skillName
            };

            return skillDefinition;
        }

        /// <summary>
        /// Create a TestFlow which spins up a SkillDialog ready for the tests to execute against.
        /// </summary>
        /// <param name="locale">Change the locale of generated activities.</param>
        /// <returns>TestFlow.</returns>
        public TestFlow GetTestFlow(string locale = null)
        {
            var adapter = new TestAdapter(sendTraceActivity: true)
                .Use(new AutoSaveStateMiddleware(this.ConversationState));

            var testFlow = new TestFlow(adapter, async (context, cancellationToken) =>
            {
                var dc = await this.Dialogs.CreateContextAsync(context);

                if (dc.ActiveDialog != null)
                {
                    var result = await dc.ContinueDialogAsync();
                }
                else
                {
                    var options = this.SkillDialogOptions;
                    await dc.BeginDialogAsync(options.SkillDefinition.Id, options);
                    var result = await dc.ContinueDialogAsync();
                }
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return null;
        }
    }
}