using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.Solutions.Skills
{
#pragma warning disable CA1710 // Identifiers should have correct suffix (rename to SkillCOntextDictionary), Ignoring for now, we will probably remove this class, if we keep it consider renaming it and removing this exclude.

    /// <summary>
    ///  Context to share state between Bots and Skills.
    /// </summary>
    public class SkillContext : Dictionary<string, JObject>
#pragma warning restore CA1710 // Identifiers should have correct suffix
    {
        public SkillContext()
        {
        }

        public SkillContext(IDictionary<string, JObject> collection)
            : base(collection)
        {
        }
    }
}
