using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using System.Collections.Generic;

namespace Microsoft.Bot.Solutions.Skills
{
    public class SkillDialogOptions
    {
        public SkillRegistration MatchedSkill { get; set; }

        public IRecognizerConvert LuisResult { get; set; }

        public CosmosDbStorageOptions ParentBotStorageOptions { get; set; }

        public Dictionary<string, object> UserInfo { get; set; }
    }
}