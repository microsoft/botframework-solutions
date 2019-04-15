using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// The Skill middleware is responsible for processing Skill mode specifics, for example the skillBegin event used to signal the start of a skill conversation.
    /// </summary>
    public class SkillMiddleware : IMiddleware
    {
        private UserState _userState;

        public SkillMiddleware(UserState userState)
        {
            _userState = userState;
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            // The skillBegin event signals the start of a skill conversation to a Bot.
            var activity = turnContext.Activity;
            if (activity != null && activity.Type == ActivityTypes.Event && activity.Name == SkillEvents.SkillBeginEventName && activity.Value != null)
            {
                var skillContext = activity.Value as SkillContext;
                if (skillContext != null)
                {
                    var accessor = _userState.CreateProperty<SkillContext>(nameof(SkillContext));
                    await accessor.SetAsync(turnContext, skillContext);
                }
            }

            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}
