using System.Collections.Generic;
using Microsoft.Bot.Configuration;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills
{
    public class SkillDefinition
    {
        public string Name { get; set; }

        public string DispatchIntent { get; set; }

        public string Endpoint { get; set; }    

        public string[] SupportedProviders { get; set; }
    }
}
