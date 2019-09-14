using System.Collections.Generic;

namespace Microsoft.Bot.Builder.Solutions.Skills
{
    public class SkillDialogOption
    {
        public string Action { get; set; }

        public IDictionary<string, object> Slots { get; } = new Dictionary<string, object>();
    }
}
