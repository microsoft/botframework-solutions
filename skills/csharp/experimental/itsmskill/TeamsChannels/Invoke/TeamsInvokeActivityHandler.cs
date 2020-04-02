namespace ITSMSkill.TeamsChannels.Invoke
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ITSMSkill.Extensions;
    using ITSMSkill.Extensions.Teams.TaskModule;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;

    public abstract class TeamsInvokeActivityHandlerFactory
    {
        protected IDictionary<string, Func<ITeamsInvokeActivityHandler<TaskEnvelope>>> TaskModuleHandlerMap { get; set; }
            = new Dictionary<string, Func<ITeamsInvokeActivityHandler<TaskEnvelope>>>();

        /// <summary>
        /// Router for getting Invoke Handler.
        /// </summary>
        public async Task<ITeamsInvokeEnvelope> GetInvokeEnvelope(ITurnContext context, CancellationToken cancellationToken)
        {
            if (context.Activity.IsTaskModuleActivity() || context.Activity.IsExtensionActionActivity())
            {
                return await this.GetTaskEnvelope(context, cancellationToken);
            }

            return null;
        }

        protected virtual async Task<ITeamsInvokeEnvelope> GetTaskEnvelope(ITurnContext context, CancellationToken cancellationToken)
        {
            ITeamsInvokeActivityHandler<TaskEnvelope> taskModuleHandler = this.GetTaskModuleHandler(context.Activity);
            return await taskModuleHandler.Handle(context, cancellationToken);
        }

        protected ITeamsInvokeActivityHandler<TaskEnvelope> GetTaskModuleHandler(Activity activity) =>
            this.GetTaskModuleHandler(activity.GetTaskModuleMetadata<TaskModuleMetadata>().TaskModuleFlowType);

        /// <summary>
        /// Gets Teams task module handler by registered name.
        /// </summary>
        /// <param name="handlerName">Handler name.</param>
        /// <returns>Message extension handler.</returns>
        /// <exception cref="NotImplementedException">Message Extension flow type undefined for handler.</exception>
        protected ITeamsInvokeActivityHandler<TaskEnvelope> GetTaskModuleHandler(string handlerName) =>
            this.TaskModuleHandlerMap.TryGetValue(handlerName, out Func<ITeamsInvokeActivityHandler<TaskEnvelope>> handlerFactory)
                ? handlerFactory()
                : throw new NotImplementedException($"Message Extension flow type undefined for handler {handlerName}");
    }
}
