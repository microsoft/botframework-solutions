using System.Collections.Generic;

namespace Microsoft.Bot.Builder.Skills.Contextual.Models.Strategy
{
    public class FIFOStrategy : IReplacementStrategy
    {
        public void Replace(List<PreviousQuestion> questions, PreviousQuestion question)
        {
            questions.RemoveAt(0);
            questions.Add(question);
        }
    }
}
