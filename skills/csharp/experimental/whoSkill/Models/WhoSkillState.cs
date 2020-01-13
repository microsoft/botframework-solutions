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

        public string TargetName { get; set; }

        public List<Candidate> CandidatesForDisplay { get; set; }

        public Candidate PickedPerson { get; set; }

        // Some flow need to search twice: Manager, Direct Reports
        public bool SecondSearchCompleted { get; set; }

        public int PageIndex { get; set; }

        public int Ordinal { get; set; }

        public WhoLuis.Intent TriggerIntent { get; set; }

        public bool Restart { get; set; }

        public void Init()
        {
            TargetName = null;
            CandidatesForDisplay = null;
            PickedPerson = null;
            SecondSearchCompleted = false;
            PageIndex = 0;
            Ordinal = 0;
            TriggerIntent = WhoLuis.Intent.None;
            Restart = false;
        }
    }
}
