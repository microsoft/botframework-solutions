using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Actions
{
    public class SkillContextualActionsBase
    {
        public Action<ITurnContext> BeforeTurnAction { get; set; } = (turncontext) => { };

        public Action<ITurnContext> AfterTurnAction { get; set; } = (turncontext) => { };
    }
}
