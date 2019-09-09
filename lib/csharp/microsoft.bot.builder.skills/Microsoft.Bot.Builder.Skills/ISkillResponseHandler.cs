using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    public interface ISkillResponseHandler
    {
        Task<ResourceResponse> SendActivityAsync(Activity activity);

        Task<ResourceResponse> UpdateActivityAsync(Activity activity);

        Task DeleteActivityAsync(string activityId);
    }
}