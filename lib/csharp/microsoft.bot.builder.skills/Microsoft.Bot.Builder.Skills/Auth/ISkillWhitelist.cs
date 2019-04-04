using System.Collections.Generic;

namespace Microsoft.Bot.Builder.Skills.Auth
{
    public interface ISkillWhitelist
    {
        List<string> SkillWhiteList { get; }
    }
}