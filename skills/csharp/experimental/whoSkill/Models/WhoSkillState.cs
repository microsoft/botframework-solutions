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

        public string TargetName { get; set; }

        public bool AlreadySearched { get; set; }

        public List<Candidate> Candidates { get; set; }

        public int PageIndex { get; set; }

        public int Ordinal { get; set; }

        public void Init()
        {
            TargetName = null;
            AlreadySearched = false;
            Candidates = new List<Candidate>();
            PageIndex = 0;
            Ordinal = int.MinValue;
        }
    }
}
