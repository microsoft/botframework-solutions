using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.UserAuth;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    public interface ISkillProtocolHandler
    {
        /// <summary>
        /// Handler to call when received an event type activity with name tokens/request.
        /// </summary>
        /// <param name="activity">TokenRequest activity.</param>
        /// <returns>Task.</returns>
        Task<ProviderTokenResponse> HandleTokenRequest(Activity activity);

        /// <summary>
        /// Handler to call when received an event type activity with name skill/fallbackrequest.
        /// </summary>
        /// <param name="activity">Fallback activity.</param>
        /// <returns>Task.</returns>
        Task HandleFallback(Activity activity);
    }
}