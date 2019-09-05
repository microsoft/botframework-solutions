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

        /// <summary>
        /// 2 actions: one excuted before await next() and another excuted after.
        /// </summary>
        /// <param name="beforeTurnAction">beforeTurnAction.</param>
        /// <param name="afterTurnAction">afterTurnAction.</param>
        public void Register(Action<ITurnContext> beforeTurnAction, Action<ITurnContext> afterTurnAction)
        {
            BeforeTurnActions.Add(beforeTurnAction);
            AfterTurnActions.Add(afterTurnAction);
        }
    }
}
