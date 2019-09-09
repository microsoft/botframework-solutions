using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// BotFrameworkSkillConnector that inherits from the base SkillConnector.
    /// </summary>
    /// <remarks>
    /// Its responsibility is to forward a incoming request to the skill and handle
    /// the responses based on Skill Protocol.
    /// </remarks>
    public class BotFrameworkSkillConnector : SkillConnector
    {
        private readonly SkillConnectionConfiguration _skillConnectionConfiguration;
        private readonly ISkillTransport _skillTransport;

        public BotFrameworkSkillConnector(SkillConnectionConfiguration skillConnectionConfiguration, ISkillTransport skillTransport)
            : base(skillConnectionConfiguration, skillTransport)
        {
            _skillConnectionConfiguration = skillConnectionConfiguration ?? throw new ArgumentNullException(nameof(skillConnectionConfiguration));
            _skillTransport = skillTransport ?? throw new ArgumentNullException(nameof(skillTransport));
        }

        public async override Task<Activity> ForwardToSkillAsync(Activity activity, ISkillResponseHandler skillResponseHandler)
        {
            var response = await _skillTransport.ForwardToSkillAsync(_skillConnectionConfiguration.SkillManifest, _skillConnectionConfiguration.ServiceClientCredentials, activity, skillResponseHandler);

            _skillTransport.Disconnect();

            return response;
        }

        public async override Task CancelRemoteDialogsAsync()
        {
            await _skillTransport.CancelRemoteDialogsAsync(_skillConnectionConfiguration.SkillManifest, _skillConnectionConfiguration.ServiceClientCredentials);
        }
    }
}