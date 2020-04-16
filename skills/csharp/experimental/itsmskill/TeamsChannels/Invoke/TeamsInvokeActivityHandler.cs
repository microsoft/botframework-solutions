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
    using Microsoft.Bot.Schema.Teams;

    public abstract class TeamsInvokeActivityHandlerFactory
    {
        protected IDictionary<string, Func<ITeamsInvokeActivityHandler<TaskEnvelope>>> TaskModuleHandlerMap { get; set; }
            = new Dictionary<string, Func<ITeamsInvokeActivityHandler<TaskEnvelope>>>();

        protected IDictionary<string, Func<ITeamsTaskModuleHandler<TaskModuleResponse>>> TaskModuleFetchSubmitMap { get; set; }
            = new Dictionary<string, Func<ITeamsTaskModuleHandler<TaskModuleResponse>>>();

        // Handler For TeamsTaskModule
        public async Task<TaskModuleResponse> HandleTaskModuleActivity(ITurnContext context, CancellationToken cancellationToken)
        {
            if (context.Activity.IsTaskModuleFetchActivity() || context.Activity.IsExtensionActionActivity())
            {
                return await this.GetTaskModuleFetch(context, cancellationToken);
            }

            if (context.Activity.IsTaskModuleSubmitActivity() || context.Activity.IsExtensionActionActivity())
            {
                return await this.GetTaskModuleSubmit(context, cancellationToken);
            }

            return null;
        }

        // Get TaskModule Fetch for Fetch Tasks
        protected virtual async Task<TaskModuleResponse> GetTaskModuleFetch(ITurnContext context, CancellationToken cancellationToken)
        {
            ITeamsTaskModuleHandler<TaskModuleResponse> taskModuleHandler = this.GetTaskModuleFetchSubmitHandler(context.Activity);
            return await taskModuleHandler.OnTeamsTaskModuleFetchAsync(context, cancellationToken);
        }

        // Get TaskMobule Submit for Submit Tasks
        protected virtual async Task<TaskModuleResponse> GetTaskModuleSubmit(ITurnContext context, CancellationToken cancellationToken)
        {
            ITeamsTaskModuleHandler<TaskModuleResponse> taskModuleHandler = this.GetTaskModuleFetchSubmitHandler(context.Activity);
            return await taskModuleHandler.OnTeamsTaskModuleSubmitAsync(context, cancellationToken);
        }

        // Get Envelope for handling Fetch and Submit together
        protected virtual async Task<ITeamsInvokeEnvelope> GetTaskEnvelope(ITurnContext context, CancellationToken cancellationToken)
        {
            ITeamsInvokeActivityHandler<TaskEnvelope> taskModuleHandler = this.GetTaskModuleHandler(context.Activity);
            return await taskModuleHandler.Handle(context, cancellationToken);
        }

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

        protected ITeamsInvokeActivityHandler<TaskEnvelope> GetTaskModuleHandler(Activity activity) =>
            this.GetTaskModuleHandler(activity.GetTaskModuleMetadata<TaskModuleMetadata>().TaskModuleFlowType);

        protected ITeamsTaskModuleHandler<TaskModuleResponse> GetTaskModuleFetchSubmitHandler(Activity activity) =>
            this.GetTaskModuleFetchSubmitHandlerMap(activity.GetTaskModuleMetadata<TaskModuleMetadata>().TaskModuleFlowType);

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

        protected ITeamsTaskModuleHandler<TaskModuleResponse> GetTaskModuleFetchSubmitHandlerMap(string handlerName) =>
                this.TaskModuleFetchSubmitMap.TryGetValue(handlerName, out Func<ITeamsTaskModuleHandler<TaskModuleResponse>> handlerFactory)
                    ? handlerFactory()
                    : throw new NotImplementedException($"Message Extension flow type undefined for handler {handlerName}");
    }
}
