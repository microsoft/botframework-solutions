namespace ITSMSkill.Middleware
{
    using System.Threading;
    using System.Threading.Tasks;
    using ITSMSkill.Proactive;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Solutions.Proactive;

    /// <summary>
    /// A Middleware for saving the proactive model data
    /// This middleware will refresh user's latest conversation reference and save it to state.
    /// </summary>
    public class ServiceNowProactiveStateMiddleware : IMiddleware
    {
        private readonly ServiceNowProactiveState _proactiveState;
        private readonly IStatePropertyAccessor<ProactiveModel> _proactiveStateAccessor;

        public ServiceNowProactiveStateMiddleware(ServiceNowProactiveState proactiveState)
        {
            _proactiveState = proactiveState;
            _proactiveStateAccessor = _proactiveState.CreateProperty<ProactiveModel>(nameof(ProactiveModel));
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            ProactiveModel proactiveState = await _proactiveStateAccessor.GetAsync(turnContext, () => new ProactiveModel(), cancellationToken);
            string userId = turnContext.Activity.From.Id;
            ConversationReference conversationReference = turnContext.Activity.GetConversationReference();

            if (proactiveState.TryGetValue(userId, out ProactiveModel.ProactiveData data))
            {
                data.Conversation = conversationReference;
            }
            else
            {
                data = new ProactiveModel.ProactiveData { Conversation = conversationReference };
            }

            proactiveState[userId] = data;
            await _proactiveStateAccessor.SetAsync(turnContext, proactiveState, cancellationToken);
            await _proactiveState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);

            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}
