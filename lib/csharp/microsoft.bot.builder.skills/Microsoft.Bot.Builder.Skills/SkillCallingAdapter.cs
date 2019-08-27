using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    public class SkillCallingAdapter : BotAdapter, IRemoteDialogCancellation
    {
        private ISkillTransport _skillTransport;
        private SkillManifest _skillManifest;
        private IServiceClientCredentials _serviceClientCredentials;

        public SkillCallingAdapter(
            SkillManifest skillManifest,
            IServiceClientCredentials serviceClientCredentials,
            ISkillProtocolHandler skillProtocolHandler,
            IBotTelemetryClient botTelemetryClient,
            ISkillTransport skillTransport = null)
        {
            _skillManifest = skillManifest ?? throw new ArgumentNullException(nameof(skillManifest));
            _serviceClientCredentials = serviceClientCredentials ?? throw new ArgumentNullException(nameof(serviceClientCredentials));

            _skillTransport = skillTransport ?? new SkillWebSocketTransport(botTelemetryClient, skillProtocolHandler);
        }

        public override Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override async Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
        {
            if (activities == null || activities.Length == 0)
            {
                throw new ArgumentNullException(nameof(activities));
            }

            // we do not support passing multiple activities now for one turn so just pick the first activity
            var activity = activities[0];
            await _skillTransport.ForwardToSkillAsync(_skillManifest, _serviceClientCredentials, turnContext, activity);

            _skillTransport.Disconnect();

            return new ResourceResponse[] { };
        }

        public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task CancelRemoteDialogsAsync(SkillManifest skillManifest, IServiceClientCredentials serviceClientCredentials, ITurnContext turnContext)
        {
            var cancelRemoteDialogEvent = turnContext.Activity.CreateReply();

            cancelRemoteDialogEvent.Type = ActivityTypes.Event;
            cancelRemoteDialogEvent.Name = SkillEvents.CancelAllSkillDialogsEventName;

            await _skillTransport.ForwardToSkillAsync(skillManifest, serviceClientCredentials, turnContext, cancelRemoteDialogEvent);
        }
    }
}