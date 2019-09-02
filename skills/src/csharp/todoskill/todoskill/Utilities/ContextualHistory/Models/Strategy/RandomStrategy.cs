using System;
using System.Collections.Generic;

namespace ToDoSkill.Utilities.ContextualHistory.Models.Strategy
{
    public class RandomStrategy : IReplacementStrategy
    {
        public void Replace(List<PreviousQuestion> questions, PreviousQuestion question)
        {
            var rand = new Random();
            questions.RemoveAt(rand.Next(questions.Count));
            questions.Add(question);
        }
    }
}
