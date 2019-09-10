using System;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Actions
{
    public class SkillContextualActionBase
    {
        public Action<ITurnContext> BeforeTurnAction { get; set; } = (turncontext) => { };

        public Action<ITurnContext> AfterTurnAction { get; set; } = (turncontext) => { };
    }
}
