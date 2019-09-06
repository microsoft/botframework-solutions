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
#pragma warning disable CA1801 // Remove unused parameter (disabling for now, need to talk to ted about having these parameter in the base class)
        protected SkillConnector(ISkillTransport skillTransport, ISkillProtocolHandler skillProtocolHandler)
#pragma warning restore CA1801 // Remove unused parameter
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
