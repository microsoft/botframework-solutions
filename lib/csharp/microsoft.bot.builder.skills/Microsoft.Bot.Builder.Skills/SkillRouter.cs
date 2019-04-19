using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.Skills.Models.Manifest;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// Skill Router class that helps Bots identify if a registered Skill matches the identified dispatch intent.
    /// </summary>
    public static class SkillRouter
    {
        /// <summary>
        /// Helper method to go through a SkillManifeste and match the passed dispatch intent to a registered action.
        /// </summary>
        /// <param name="skillConfiguration">The Skill Configuration for the current Bot.</param>
        /// <param name="dispatchIntent">The Dispatch intent to try and match to a skill.</param>
        /// <returns>Whether the intent matches a Skill.</returns>
        public static SkillManifest IsSkill(List<SkillManifest> skillConfiguration, string dispatchIntent)
        {
            return skillConfiguration.SingleOrDefault(s => s.Id == dispatchIntent.ToString());
        }
    }
}