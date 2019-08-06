using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Models
{
    public class SearchKnowledgeResult : ResultBase
    {
        public Knowledge[] Knowledges { get; set; }
    }
}
