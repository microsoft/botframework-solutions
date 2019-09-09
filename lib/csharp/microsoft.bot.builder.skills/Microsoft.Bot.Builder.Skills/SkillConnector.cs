using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Schema;

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
        public SkillConnector(SkillConnectionConfiguration skillConnectionConfiguration, ISkillTransport skillTransport)
        {
        }

        /// <summary>
        /// Forward incoming request to the skill.
        /// </summary>
        /// <param name="activity">Activity object to forward.</param>
        /// <param name="skillResponseHandler">Handler that handles response back from skill.</param>
        /// <returns>Response activity of the forwarded activity to the skill.</returns>
        public abstract Task<Activity> ForwardToSkillAsync(Activity activity, ISkillResponseHandler skillResponseHandler);

        /// <summary>
        /// Cancel the remote skill dialogs on the stack.
        /// </summary>
        /// <returns>Task.</returns>
        public abstract Task CancelRemoteDialogsAsync();
    }
}