using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// This interface defines functions needed to properly handle the skill protocol on the Calling bot side
    /// </summary>
    public interface ISkillProtocolHandler
    {
        /// <summary>
        /// Handler to call when receive an activity with type EndOfConversation.
        /// </summary>
        /// <param name="activity">EndOfConversation activity.</param>
        /// <returns>Task.</returns>
        Task HandleEndOfConversation(Activity activity);

        /// <summary>
        /// Handler to call when received an event type activity with name tokens/request.
        /// </summary>
        /// <param name="activity">TokenRequest activity.</param>
        /// <returns>Task.</returns>
        Task HandleTokenRequest(Activity activity);

        /// <summary>
        /// Handler to call when received an event type activity with name skill/fallbackrequest.
        /// </summary>
        /// <param name="activity">Fallback activity.</param>
        /// <returns>Task.</returns>
        Task HandleFallback(Activity activity);
    }
}