using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Skills;
using FakeSkill.Dialogs.Shared;
using FakeSkill.ServiceClients;
using Microsoft.Bot.Solutions.Responses;

namespace FakeSkill.Dialogs.Auth
{
    public class AuthDialog : SkillTemplateDialog
    {
        public AuthDialog(
            SkillConfigurationBase services,
            ResponseManager responseManager,
            IStatePropertyAccessor<SkillConversationState> conversationStateAccessor,
            IStatePropertyAccessor<SkillUserState> userStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(AuthDialog), services, responseManager, conversationStateAccessor, userStateAccessor, serviceManager, telemetryClient)
        {
            var sample = new WaterfallStep[]
            {
                GetAuthToken,
                End,
            };

            AddDialog(new WaterfallDialog(nameof(AuthDialog), sample));
        }

        private Task<DialogTurnResult> End(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return stepContext.EndDialogAsync();
        }

        private class DialogIds
        {
        }
    }
}
