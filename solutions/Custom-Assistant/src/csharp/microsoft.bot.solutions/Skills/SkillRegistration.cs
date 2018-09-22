using System.Collections.Generic;

namespace Microsoft.Bot.Solutions.Skills
{
    public class SkillRegistration
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string DispatcherModelName { get; set; }

        public string Assembly { get; set; }

        public string AuthConnectionName { get; set; }

        public string[] Parameters { get; set; }

        public Dictionary<string, string> Configuration {get;set;}
    }
}
