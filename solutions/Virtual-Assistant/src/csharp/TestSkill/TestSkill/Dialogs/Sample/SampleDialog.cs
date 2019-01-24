using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Skills;
using TestSkill.Dialogs.Sample.Resources;
using TestSkill.Dialogs.Shared;
using TestSkill.ServiceClients;

namespace TestSkill.Dialogs.Sample
{
    public class SampleDialog : SkillTemplateDialog
    {
        private ResponseTemplateManager _responder = new ResponseTemplateManager(new SampleResponses());

        public SampleDialog(
            SkillConfigurationBase services,
            IStatePropertyAccessor<SkillConversationState> conversationStateAccessor,
            IStatePropertyAccessor<SkillUserState> userStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(SampleDialog), services, conversationStateAccessor, userStateAccessor, serviceManager, telemetryClient)
        {
            var sample = new WaterfallStep[]
            {
                PromptForMessage,
                PrintMessage,
                End,
            };

            AddDialog(new WaterfallDialog(nameof(SampleDialog), sample));
            AddDialog(new TextPrompt(DialogIds.MessagePrompt));
        }

        private async Task<DialogTurnResult> PromptForMessage(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, SampleResponses.NamePrompt);
            return await stepContext.PromptAsync(DialogIds.MessagePrompt, new PromptOptions { Prompt = prompt });
        }

        private async Task<DialogTurnResult> PrintMessage(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await _responder.ReplyWith(stepContext.Context, SampleResponses.HaveNameMessage, new { Name = stepContext.Result });
            return await stepContext.NextAsync();
        }

        private Task<DialogTurnResult> End(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return stepContext.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string MessagePrompt = "messagePrompt";
        }
    }
}
