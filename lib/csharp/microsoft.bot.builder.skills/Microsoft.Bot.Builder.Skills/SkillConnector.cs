using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Models;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// SkillConnector is the base class that handles communication with a skill.
    /// </summary>
    /// <remarks>
    /// Its responsibility is to forward a incoming request to the skill and handle
    /// the responses based on Skill Protocol.
    /// </remarks>
    public abstract class SkillConnector
    {
        public SkillConnector(ISkillProtocolHandler skillProtocolHandler)
        {
        }

        /// <summary>
        /// Forward incoming request to the skill.
        /// </summary>
        /// <param name="skillConnectionConfiguration">Skill Connection Configuration.</param>
        /// <returns>Task.</returns>
        public abstract Task ForwardToSkillAsync(SkillConnectionConfiguration skillConnectionConfiguration);
    }
}