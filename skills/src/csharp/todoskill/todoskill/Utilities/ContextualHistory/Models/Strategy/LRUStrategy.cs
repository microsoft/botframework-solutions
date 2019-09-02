using System.Collections.Generic;

namespace ToDoSkill.Utilities.ContextualHistory.Models.Strategy
{
    public class LRUStrategy : IReplacementStrategy
    {
        public void Replace(List<PreviousQuestion> questions, PreviousQuestion question)
        {
            questions.Sort(new LRUQuestionComparer());
            questions.RemoveAt(0);
            questions.Add(question);
        }

        private class LRUQuestionComparer : IComparer<PreviousQuestion>
        {
            public int Compare(PreviousQuestion x, PreviousQuestion y)
            {
                return x.TimeStamp.CompareTo(y.TimeStamp);
            }
        }
    }
}
