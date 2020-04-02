namespace ITSMSkill.Dialogs.Teams
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ITSMSkill.Extensions.Teams.TaskModule;
    using ITSMSkill.Models;
    using ITSMSkill.Services;
    using ITSMSkill.TeamsChannels.Invoke;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Solutions.Responses;
    using Microsoft.Extensions.DependencyInjection;

    public abstract class ITSMTaskModuleHandlerBase : ITeamsInvokeActivityHandler<TaskEnvelope>
    {
        protected ITSMTaskModuleHandlerBase(
            IServiceProvider serviceProvider)
        {
            Settings = serviceProvider.GetService<BotSettings>();
            Services = serviceProvider.GetService<BotServices>();
            //TemplateManager = serviceProvider.GetService<LocaleTemplateManager>();
            var conversationState = serviceProvider.GetService<ConversationState>();
            StateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));
            ServiceManager = serviceProvider.GetService<IServiceManager>();
        }

        protected BotSettings Settings { get; }

        protected BotServices Services { get; }

        protected IStatePropertyAccessor<SkillState> StateAccessor { get; }

        //protected LocaleTemplateManager TemplateManager { get; }

        protected IServiceManager ServiceManager { get; }

        public abstract Task<TaskEnvelope> Handle(ITurnContext context, CancellationToken cancellationToken);
    }
}
