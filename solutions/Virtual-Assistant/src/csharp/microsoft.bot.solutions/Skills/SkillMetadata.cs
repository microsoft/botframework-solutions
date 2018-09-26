using Microsoft.Bot.Builder;
using Microsoft.Bot.Configuration;
using System.Collections.Generic;

namespace Microsoft.Bot.Solutions.Skills
{
    /// <summary>
    ///  Skills are invoked "in-process" at this time. We have already performed the Luis evaluation so pass this on to avoid duplication
    ///  Skills don't have access to their own configuration file so we enable transfer of settings from the Skill registration 
    ///  Skills also need user information to personalise the experience, for example Timezone or Location. This is all under strict control
    ///  by the Custom Assistant developer.
    /// </summary>
    public class SkillMetadata
    {
        public SkillMetadata(IRecognizerConvert luisResult, LuisService luisService, Dictionary<string, string> configuration, Dictionary<string,object> parameters)
        {
            LuisResult = luisResult;
            LuisService = luisService;
            Parameters = parameters;
        }

        public Dictionary<string, string> Configuration { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public IRecognizerConvert LuisResult { get; set; }
        public LuisService LuisService { get; set; }
    }
}