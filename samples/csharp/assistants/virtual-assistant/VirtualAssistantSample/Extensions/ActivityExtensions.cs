using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VirtualAssistantSample.Models;

namespace VirtualAssistantSample.Extensions
{
    public static class ActivityExtensions
    {
        // Fetches skillName from CardAction data if present
        public static string GetSkillName(this Activity activity)
        {
            string skillName = string.Empty;

            try
            {
                if (activity.Type.Equals(ActivityTypes.Message) && activity.Value != null)
                {
                    var data = JsonConvert.DeserializeObject<SkillCardActionData>(activity.Value.ToString());
                    skillName = data.SkillName;
                }
                else if (activity.Type.Equals(ActivityTypes.Invoke) && activity.Value != null)
                {
                    var data = JsonConvert.DeserializeObject<SkillCardActionData>(JObject.Parse(activity.Value.ToString()).SelectToken("data").SelectToken("data").ToString());
                    skillName = data.SkillName;
                }
            }
            catch
            {
                // If not able to retrive skillId, empty skillId should be returned
                // TODO trace, telemetry
            }

            return skillName;
        }
    }
}
