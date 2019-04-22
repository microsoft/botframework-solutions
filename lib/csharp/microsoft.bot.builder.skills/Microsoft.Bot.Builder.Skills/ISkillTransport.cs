using System;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    public interface ISkillTransport
    {
        Task<bool> ForwardToSkillAsync(ITurnContext dialogContext, Activity activity, Func<Activity, Activity> tokenRequestHandler = null);

        Task CancelRemoteDialogsAsync(ITurnContext turnContext);

        void Disconnect();
    }
}