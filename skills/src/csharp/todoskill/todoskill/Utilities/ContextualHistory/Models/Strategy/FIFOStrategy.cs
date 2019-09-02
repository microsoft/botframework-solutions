using System.Collections.Generic;

namespace ToDoSkill.Utilities.ContextualHistory.Models.Strategy
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
