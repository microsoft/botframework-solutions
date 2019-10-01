using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BingSearchSkill.Models
{
    public class FactModel
    {
        public string id { get; set; }
        public Contractualrule[] contractualRules { get; set; }
        public Attribution[] attributions { get; set; }
        public Value[] value { get; set; }
    }

    public class Contractualrule
    {
        public string _type { get; set; }
        public string text { get; set; }
        public string url { get; set; }
    }

    public class Attribution
    {
        public string providerDisplayName { get; set; }
        public string seeMoreUrl { get; set; }
    }

    public class Value
    {
        public string description { get; set; }
        public string subjectName { get; set; }
    }

}
