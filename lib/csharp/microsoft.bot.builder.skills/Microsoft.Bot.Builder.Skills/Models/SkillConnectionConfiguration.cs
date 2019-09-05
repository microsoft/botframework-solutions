using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models.Manifest;

namespace Microsoft.Bot.Builder.Skills.Models
{
    public class SkillConnectionConfiguration
    {
        public SkillManifest SkillManifest { get; set; }

        public IServiceClientCredentials ServiceClientCredentials { get; set; }
    }
}