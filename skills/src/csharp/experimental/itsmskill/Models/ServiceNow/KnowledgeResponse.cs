using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Models.ServiceNow
{
    public class KnowledgeResponse
    {
        public string short_description { get; set; }

        public string sys_updated_on { get; set; }

        public string sys_id { get; set; }

        public string text { get; set; }

        public string wiki { get; set; }

        public string number { get; set; }
    }
}
