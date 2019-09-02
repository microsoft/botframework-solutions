using System.Collections.Generic;

namespace ToDoSkill.Utilities.ContextualHistory.Models.Strategy
{
    public interface IReplacementStrategy
    {
        void Replace(List<PreviousQuestion> questions, PreviousQuestion question);
    }
}
