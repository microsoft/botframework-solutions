using System.Collections.Generic;
using Microsoft.Bot.Builder.Skills.Auth;

namespace NewsSkill
{
    public class SkillWhitelist : ISkillWhitelist
    {
        private readonly List<string> _whiteList = new List<string>
        {
            "7cc1ea17-ceec-42fe-b056-45c25884d7b7"
        };

        public List<string> SkillWhiteList
        {
            get
            {
                return _whiteList;
            }
        }
    }
}