using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace VirtualAssistantSample.Models
{
    public class SummaryState
    {
        public int SkillIndex { get; set; } = 0;

        public List<SummaryInfo> SummaryInfos { get; set; }

        public int TotalEventCount { get; set; } = 0;

        public int TotalEventKinds { get; set; } = 0;

        public int TotalShowEventCount { get; set; } = 0;

        public class SummaryInfo
        {
            public Dictionary<string, Entity> SkillResults { get; set; }

            public string SkillIds { get; set; }

            public string ActionIds { get; set; }
        }
    }
}
