using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Configuration;
using System.Collections.Generic;

namespace Microsoft.Bot.Solutions.Skills
{
    public class SkillDialogOptions
    {
        public SkillService MatchedSkill { get; set; }

        public LuisService LuisService { get; set; }

        public IRecognizerConvert LuisResult { get; set; }

        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}