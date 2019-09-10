using System.Threading;
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
#pragma warning disable CA1801 // Remove unused parameter (disabling for now, need to talk to ted about having these parameter in the base class)
        public SkillConnector(SkillConnectionConfiguration skillConnectionConfiguration, ISkillTransport skillTransport)
#pragma warning restore CA1801 // Remove unused parameter
        {
        }

        /// <summary>
        /// Forward incoming request to the skill.
        /// </summary>
        /// <param name="activity">Activity object to forward.</param>
        /// <param name="skillResponseHandler">Handler that handles response back from skill.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Response activity of the forwarded activity to the skill.</returns>
        public abstract Task<Activity> ForwardToSkillAsync(Activity activity, ISkillResponseHandler skillResponseHandler, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancel the remote skill dialogs on the stack.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task.</returns>
        public abstract Task CancelRemoteDialogsAsync(CancellationToken cancellationToken = default);
    }
}
