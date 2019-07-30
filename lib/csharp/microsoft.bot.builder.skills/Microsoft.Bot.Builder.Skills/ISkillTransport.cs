using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    public interface ISkillTransport
    {
        Task<bool> ForwardToSkillAsync(SkillManifest skillManifest, IServiceClientCredentials appCredentials, ITurnContext dialogContext, Activity activity, Action<Activity> tokenRequestHandler = null);

        Task CancelRemoteDialogsAsync(SkillManifest skillManifest, IServiceClientCredentials appCredentials, ITurnContext turnContext);

        void Disconnect();
    }
}