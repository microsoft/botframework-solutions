using Microsoft.Bot.Builder.Skills.Models.Manifest;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Bot.Builder.Skills
{
    public class SkillRouter
    {
        private List<SkillManifest> _registeredSkills;

        public SkillRouter(List<SkillManifest> registeredSkills)
        {
            // Retrieve any Skills that have been registered with the Bot
            _registeredSkills = registeredSkills;
        }

        public SkillManifest IdentifyRegisteredSkill(string skillName)
        {
            SkillManifest matchedSkill = null;

            // Did we find any skills?
            if (_registeredSkills != null)
            {
                // Identify a skill by taking the LUIS model name identified by the dispatcher and matching to the skill luis model name
                // Bug raised on dispatcher to move towards LuisModelId instead perhaps?
                matchedSkill = _registeredSkills.FirstOrDefault(s => s.Name == skillName);
                return matchedSkill;
            }
            else
            {
                return null;
            }
        }
    }
}