using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Autofac;
using AutomotiveSkill;
using AutomotiveSkillTest.Flow.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.Extensions.Configuration;

namespace AutomotiveSkillTest.Flow
{
    public class AutomotiveSkillTestBase : BotTestBase
    {
        public IStatePropertyAccessor<AutomotiveSkillState> AutomotiveSkillStateAccessor { get; set; }

        public ConversationState ConversationState { get; set; }

        public UserState UserState { get; set; }

        public SkillConfigurationBase Services { get; set; }

        public BotConfiguration Options { get; set; }

        public HttpContext MockHttpContext { get; set; }

        public IBotTelemetryClient TelemetryClient { get; set; }

        public HttpContextAccessor MockHttpContextAcessor { get; set; }

        public override void Initialize()
        {
           var builder = new ContainerBuilder();

            this.ConversationState = new ConversationState(new MemoryStorage());
            this.UserState = new UserState(new MemoryStorage());
            this.AutomotiveSkillStateAccessor = this.ConversationState.CreateProperty<AutomotiveSkillState>(nameof(AutomotiveSkillState));
            this.Services = new MockSkillConfiguration();

            builder.RegisterInstance(new BotStateSet(this.UserState, this.ConversationState));

            this.Container = builder.Build();
            
            this.BotResponseBuilder = new BotResponseBuilder();
            this.BotResponseBuilder.AddFormatter(new TextBotResponseFormatter());

            this.TelemetryClient = new NullBotTelemetryClient();

            // Mock HttpContext for image path resolution
            MockHttpContext = new DefaultHttpContext();            
            MockHttpContext.Request.Scheme = "http";
            MockHttpContext.Request.Host = new HostString("localhost",3980);

            MockHttpContextAcessor = new HttpContextAccessor();
            MockHttpContextAcessor.HttpContext = MockHttpContext;
        }

        public TestFlow GetTestFlow()
        {
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(this.ConversationState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = this.BuildBot() as AutomotiveSkill.AutomotiveSkill;
                var state = await this.AutomotiveSkillStateAccessor.GetAsync(context, () => new AutomotiveSkillState());
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return new AutomotiveSkill.AutomotiveSkill(this.Services, this.ConversationState, this.UserState, this.TelemetryClient,null, MockHttpContextAcessor, true);
        }
    }
}
