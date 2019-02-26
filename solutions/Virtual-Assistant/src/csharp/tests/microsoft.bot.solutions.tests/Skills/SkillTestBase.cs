﻿using System.Collections.Generic;
using Autofac;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Bot.Solutions.Telemetry;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.Bot.Solutions.Testing.Mocks;
using Microsoft.Bot.Solutions.Tests.Skills.Fakes.FakeSkill.Dialogs.Auth.Resources;
using Microsoft.Bot.Solutions.Tests.Skills.Fakes.FakeSkill.Dialogs.Main.Resources;
using Microsoft.Bot.Solutions.Tests.Skills.Fakes.FakeSkill.Dialogs.Sample.Resources;
using Microsoft.Bot.Solutions.Tests.Skills.Fakes.FakeSkill.Dialogs.Shared.Resources;
using Microsoft.Bot.Solutions.Tests.Skills.LuisTestUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests.Skills
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

        public SkillConfigurationBase Services { get; set; }

        public Dictionary<string, SkillConfigurationBase> SkillConfigurations { get; set; }

        public SkillDialogOptions SkillDialogOptions { get; set; }

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
            SkillConfigurations = new Dictionary<string, SkillConfigurationBase>();

            // Add the LUIS model fakes used by the Skill
            Services = new MockSkillConfiguration();
            Services.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>
                {
                    { "general", GeneralTestUtil.CreateRecognizer() },
                    { "FakeSkill", FakeSkillTestUtil.CreateRecognizer() }
                }
            });

            Services.LocaleConfigurations.Add("es", new LocaleConfiguration()
            {
                Locale = "es-mx",
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>
                {
                    { "general", GeneralTestUtil.CreateRecognizer() },
                    { "FakeSkill", FakeSkillTestUtil.CreateRecognizer() }
                }
            });

            // Dummy Authentication connection for Auth testing
            Services.AuthenticationConnections = new Dictionary<string, string>
            {
                { "DummyAuth", "DummyAuthConnection" }
            };

            builder.RegisterInstance(new BotStateSet(UserState, ConversationState));
            Container = builder.Build();

            Dialogs = new DialogSet(DialogState);

            var locales = new string[] { "en-us", "de-de", "es-es", "fr-fr", "it-it", "zh-cn" };
            ResponseManager = new ResponseManager(
                locales,
                new SampleAuthResponses(),
                new MainResponses(),
                new SharedResponses(),
                new SampleResponses());

            // Manually mange the conversation metadata when we need finer grained control
            ConversationReference = new ConversationReference
            {
                ChannelId = "test",
                ServiceUrl = "https://test.com",
            };

            ConversationReference.User = new ChannelAccount("user1", "User1");
            ConversationReference.Bot = new ChannelAccount("bot", "Bot");
            ConversationReference.Conversation = new ConversationAccount(false, "convo1", "Conversation1");
        }

        /// <summary>
        /// Create a TestFlow which spins up a CustomSkillDialog ready for the tests to execute against
        /// </summary>
        /// <param name="locale"></param>
        /// <param name="overrideSkillDialogOptions"></param>
        /// <returns></returns>
        public TestFlow GetTestFlow(string locale = null, SkillDialogOptions overrideSkillDialogOptions = null)
        {
            var adapter = new TestAdapter(sendTraceActivity: true)
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
                    var options = overrideSkillDialogOptions ?? SkillDialogOptions;
                    await dc.BeginDialogAsync(options.SkillDefinition.Id, options);
                    var result = await dc.ContinueDialogAsync();
                }
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return new FakeSkill.FakeSkill(Services, EndpointService, ConversationState, UserState, ProactiveState, TelemetryClient, BackgroundTaskQueue, true, ResponseManager, null);
        }
    }
}