using System.Collections.Generic;

namespace Microsoft.Bot.Builder.Skills
{
    public class SkillDialogOption
    {
        public string Action { get; set; }

        public IDictionary<string, object> Slots { get; set; }
    }
}