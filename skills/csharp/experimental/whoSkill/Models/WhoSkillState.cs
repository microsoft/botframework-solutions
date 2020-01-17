using System.Collections.Generic;
using Luis;
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

        public string Keyword { get; set; }

        public List<Candidate> Candidates { get; set; }

        public Candidate PickedPerson { get; set; }

        public List<Candidate> Results { get; set; }

        public WhoLuis.Intent TriggerIntent { get; set; }

        public bool Restart { get; set; }

        public int PageIndex { get; set; }

        public int Ordinal { get; set; }

        public void Init()
        {
            Keyword = null;
            Candidates = null;
            PickedPerson = null;
            Results = null;
            TriggerIntent = WhoLuis.Intent.None;
            Restart = false;
            PageIndex = 0;
            Ordinal = 0;
        }
    }
}
