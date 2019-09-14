using Microsoft.Bot.Builder.Solutions.Skills.Auth;
using Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest;

namespace Microsoft.Bot.Builder.Solutions.Skills.Models
{
    public class SkillConnectionConfiguration
    {
        public SkillManifest SkillManifest { get; set; }

        public IServiceClientCredentials ServiceClientCredentials { get; set; }
    }
}
