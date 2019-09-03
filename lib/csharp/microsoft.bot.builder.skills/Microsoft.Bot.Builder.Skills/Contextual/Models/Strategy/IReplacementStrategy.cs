using System.Collections.Generic;

namespace Microsoft.Bot.Builder.Skills.Contextual.Strategy
{
    public interface IReplacementStrategy
    {
        void Replace(List<PreviousQuestion> questions, PreviousQuestion question);
    }
}
