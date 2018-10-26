using System.Collections.Generic;
using System.Threading;
using Autofac;
using EmailSkill;
using EmailSkillTest.Flow.Fakes;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Extensions.Configuration;
using TestFramework;

namespace EmailSkillTest.Flow
{
    public class EmailBotTestBase : BotTestBase
    {
        public IStatePropertyAccessor<EmailSkillState> EmailStateAccessor { get; set; }

        public ConversationState ConversationState { get; set; }

        public UserState UserState { get; set; }

        public IMailSkillServiceManager ServiceManager { get; set; }

        public ISkillConfiguration Services { get; set; }

        public BotConfiguration Options { get; set; }

        public override void Initialize()
        {
            this.Configuration = new BuildConfig().Configuration;
            var builder = new ContainerBuilder();
            builder.RegisterInstance<IConfiguration>(this.Configuration);
            var botFilePath = this.Configuration.GetSection("botFilePath")?.Value;
            var botFileSecret = this.Configuration.GetSection("botFileSecret")?.Value;
            var options = BotConfiguration.Load(botFilePath ?? @".\EmailSkillTest.bot", botFileSecret);
            builder.RegisterInstance(options);

            this.ConversationState = new ConversationState(new MemoryStorage());
            this.UserState = new UserState(new MemoryStorage());
            this.EmailStateAccessor = this.ConversationState.CreateProperty<EmailSkillState>(nameof(EmailSkillState));

            var parameters = this.Configuration.GetSection("Parameters")?.Get<string[]>();
            var configuration = this.Configuration.GetSection("Configuration")?.Get<Dictionary<string, object>>();
            this.Services = new MockSkillConfiguration();

            builder.RegisterInstance(new BotStateSet(this.UserState, this.ConversationState));
            var fakeServiceManager = new MockMailServiceManager();
            builder.RegisterInstance<IMailSkillServiceManager>(fakeServiceManager);

            this.Container = builder.Build();
            this.ServiceManager = fakeServiceManager;
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
                var bot = this.BuildBot() as EmailSkill.EmailSkill;
                var state = await this.EmailStateAccessor.GetAsync(context, () => new EmailSkillState());
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return new EmailSkill.EmailSkill(this.Services, this.ConversationState, this.UserState,  this.ServiceManager);
        }
    }
}
