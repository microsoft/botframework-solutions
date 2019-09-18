using Microsoft.Bot.Builder.Solutions.Contextual.Actions;
using Microsoft.Bot.Builder.Solutions.Contextual.Services;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Solutions.Contextual
{
    public class SkillContextualMiddleware : IMiddleware
    {
        private List<Action<ITurnContext>> BeforeTurnActions { get; set; } = new List<Action<ITurnContext>>();

        private List<Action<ITurnContext>> AfterTurnActions { get; set; } = new List<Action<ITurnContext>>();

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var action in BeforeTurnActions)
            {
                action(turnContext);
            }

            await next(cancellationToken).ConfigureAwait(false);

            foreach (var action in AfterTurnActions)
            {
                action(turnContext);
            }
        }

        public void RegisterAction(SkillContextualActionBase actions)
        {
            BeforeTurnActions.Add(actions.BeforeTurnAction);
            AfterTurnActions.Add(actions.AfterTurnAction);
        }
    }
}
