using Microsoft.Bot.Builder;

namespace NewsSkill
{
    public class NewsSkillState
    {
        public NewsSkillState()
        {
        }

        public Luis.News LuisResult { get; set; }
    }
}
