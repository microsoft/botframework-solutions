using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Solutions.Util;

namespace Microsoft.Bot.Solutions.Proactive
{
    /// <summary>
    /// A Middleware for saving the proactive model data
    /// This middleware will refresh user's latest conversation reference and save it to state.
    /// </summary>
    /// <typeparam name="T">Type of ProacitveState.</typeparam>
    public class ProactiveStateMiddleWareTemplate<T> : IMiddleware
        where T : BotState
    {
        private readonly T _proactiveState;
        private readonly IStatePropertyAccessor<ProactiveModel> _proactiveStateAccessor;

        public ProactiveStateMiddleWareTemplate(T proactiveState)
        {
            _proactiveState = proactiveState;
            _proactiveStateAccessor = _proactiveState.CreateProperty<ProactiveModel>(nameof(ProactiveModel));
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
        {
            var activity = turnContext.Activity;

            if (!string.IsNullOrEmpty(activity.From.Role) && activity.From.Role.Equals("user", StringComparison.InvariantCultureIgnoreCase))
            {
                var proactiveState = await _proactiveStateAccessor.GetAsync(turnContext, () => new ProactiveModel()).ConfigureAwait(false);
                ProactiveModel.ProactiveData data;
                var hashedUserId = MD5Util.ComputeHash(turnContext.Activity.From.Id);
                var conversationReference = turnContext.Activity.GetConversationReference();
                var proactiveData = new ProactiveModel.ProactiveData { Conversation = conversationReference };

                if (proactiveState.TryGetValue(hashedUserId, out data))
                {
                    data.Conversation = conversationReference;
                }
                else
                {
                    data = new ProactiveModel.ProactiveData { Conversation = conversationReference };
                }

                proactiveState[hashedUserId] = data;
                await _proactiveStateAccessor.SetAsync(turnContext, proactiveState).ConfigureAwait(false);
                await _proactiveState.SaveChangesAsync(turnContext).ConfigureAwait(false);
            }

            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}
