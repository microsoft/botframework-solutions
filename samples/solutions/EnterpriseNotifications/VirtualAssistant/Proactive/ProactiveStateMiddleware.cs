// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualAssistant.Proactive
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
                var conversationReference = turnContext.Activity.GetConversationReference();
                proactiveState.Conversation = conversationReference;
                await _proactiveStateAccessor.SetAsync(turnContext, proactiveState).ConfigureAwait(false);
                await _proactiveState.SaveChangesAsync(turnContext).ConfigureAwait(false);
            }

            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}