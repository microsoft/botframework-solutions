using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// The Skill middleware is responsible for processing Skill mode specifics, for example the skillBegin event used to signal the start of a skill conversation.
    /// </summary>
    public class SkillMiddleware : IMiddleware
    {
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            // The skillBegin event signals the start of a skill conversation to a Bot.
            var activity = turnContext.Activity;
            if (activity != null && activity.Type == ActivityTypes.Event && activity?.Name == SkillEvents.SkillBeginEventName)
            {
                // Slots (parameters) are passed through the Event on the Value property.
                if (activity.Value != null)
                {
                    // Push slots into state.
                    // turnContext.TurnState?
                }
            }

            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}
