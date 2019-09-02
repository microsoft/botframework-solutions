using System;

namespace ToDoSkill.Utilities.ContextualHistory.Models
{
    public class PreviousQuestion
    {
        public string Utterance { get; set; }

        public string Intent { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
