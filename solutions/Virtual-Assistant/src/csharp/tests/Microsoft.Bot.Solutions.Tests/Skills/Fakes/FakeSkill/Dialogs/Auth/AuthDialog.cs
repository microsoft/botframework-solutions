using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using FakeSkill.Dialogs.Sample.Resources;
using FakeSkill.Dialogs.Shared;
using FakeSkill.ServiceClients;

namespace FakeSkill.Dialogs.Auth
{
    public class AuthDialog : SkillTemplateDialog
    {
        public AuthDialog(
            SkillConfigurationBase services,
            IStatePropertyAccessor<SkillConversationState> conversationStateAccessor,
            IStatePropertyAccessor<SkillUserState> userStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(AuthDialog), services, conversationStateAccessor, userStateAccessor, serviceManager, telemetryClient)
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
