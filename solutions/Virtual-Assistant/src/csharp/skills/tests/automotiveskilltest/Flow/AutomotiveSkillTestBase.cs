using System.Collections.Generic;
using System.Threading;
using Autofac;
using AutomotiveSkill;
using AutomotiveSkillTest.Flow.Fakes;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Extensions.Configuration;
using TestFramework;

namespace AutomotiveSkillTest.Flow
{
    public class AutomotiveSkillTestBase : BotTestBase
    {
        public IStatePropertyAccessor<AutomotiveSkillState> AutomotiveSkillStateAccessor { get; set; }

        public ConversationState ConversationState { get; set; }

        public UserState UserState { get; set; }

        public ISkillConfiguration Services { get; set; }

        public BotConfiguration Options { get; set; }

        public override void Initialize()
        {
            this.Configuration = new BuildConfig().Configuration;
            var builder = new ContainerBuilder();
            builder.RegisterInstance<IConfiguration>(this.Configuration);
            var botFilePath = this.Configuration.GetSection("botFilePath")?.Value;
            var botFileSecret = this.Configuration.GetSection("botFileSecret")?.Value;
            var options = BotConfiguration.Load(botFilePath ?? @".\AutomotiveSkillTest.bot", botFileSecret);
            builder.RegisterInstance(options);

            this.ConversationState = new ConversationState(new MemoryStorage());
            this.UserState = new UserState(new MemoryStorage());
            this.AutomotiveSkillStateAccessor = this.ConversationState.CreateProperty<AutomotiveSkillState>(nameof(AutomotiveSkillState));

            var parameters = this.Configuration.GetSection("Parameters")?.Get<string[]>();
            var configuration = this.Configuration.GetSection("Configuration")?.Get<Dictionary<string, object>>();
            this.Services = new MockSkillConfiguration();

            builder.RegisterInstance(new BotStateSet(this.UserState, this.ConversationState));           

            this.Container = builder.Build();
            this.Options = options;

            this.BotResponseBuilder = new BotResponseBuilder();
            this.BotResponseBuilder.AddFormatter(new TextBotResponseFormatter());
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
            return new AutomotiveSkill.AutomotiveSkill(this.Services, this.ConversationState, this.UserState,null, true);
        }
    }
}
