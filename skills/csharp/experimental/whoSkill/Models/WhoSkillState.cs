using System.Collections.Generic;
using Microsoft.Bot.Builder.Dialogs;

namespace WhoSkill.Models
{
    public class WhoSkillState : DialogState
    {
        public WhoSkillState()
        {
            Init();
        }

        // Set from config
        public int PageSize { get; set; }

        public string PersonName { get; set; }

        public bool AlreadySearched { get; set; }

        public List<Person> Persons { get; set; }

        public int PageIndex { get; set; }

        public int Ordinal { get; set; }

        public void Init()
        {
            PersonName = null;
            AlreadySearched = false;
            Persons = new List<Person>();
            PageIndex = 0;
            Ordinal = int.MinValue;
        }
    }
}
