// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Solutions.Util;

namespace Microsoft.Bot.Builder.Solutions.Proactive
{
    /// <summary>
    /// A Middleware for saving the proactive model data
    /// This middleware will refresh user's latest conversation reference and save it to state.
    /// </summary>
    public class ProactiveStateMiddleware : IMiddleware
    {
        private ProactiveState _proactiveState;
        private IStatePropertyAccessor<ProactiveModel> _proactiveStateAccessor;

        public ProactiveStateMiddleware(ProactiveState proactiveState)
        {
            _proactiveState = proactiveState;
            _proactiveStateAccessor = _proactiveState.CreateProperty<ProactiveModel>(nameof(ProactiveModel));
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = turnContext.Activity;

            if (activity.From.Properties["role"].ToString().Equals("user", StringComparison.InvariantCultureIgnoreCase))
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