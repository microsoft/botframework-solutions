using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    public interface ISkillTransport
    {
        Task<Activity> ForwardToSkillAsync(SkillManifest skillManifest, IServiceClientCredentials serviceClientCredentials, ITurnContext dialogContext, Activity activity, Action<Activity> tokenRequestHandler = null, Action<Activity> fallbackHandler = null, CancellationToken cancellationToken = default);

        Task CancelRemoteDialogsAsync(SkillManifest skillManifest, IServiceClientCredentials serviceClientCredentials, ITurnContext turnContext, CancellationToken cancellationToken = default);

        void Disconnect();
    }
}
