using System.Collections.Generic;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    ///  Context to share state between Bots and Skills.
    /// </summary>
    public class SkillContext : Dictionary<string, object>
    {
        public SkillContext()
        {
        }

        public SkillContext(IDictionary<string, object> collection)
            : base(collection)
        {
        }
    }
}