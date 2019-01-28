using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Skills;
using TestSkill.Dialogs.Sample.Resources;
using TestSkill.Dialogs.Shared;
using TestSkill.ServiceClients;

namespace TestSkill.Dialogs.Sample
{
    public class SampleDialog : SkillTemplateDialog
    {
        public SampleDialog(
            SkillConfigurationBase services,
            ResponseTemplateManager responseManager,
            IStatePropertyAccessor<SkillConversationState> conversationStateAccessor,
            IStatePropertyAccessor<SkillUserState> userStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(SampleDialog), services, responseManager, conversationStateAccessor, userStateAccessor, serviceManager, telemetryClient)
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
            var prompt = ResponseManager.GetResponse(
                SampleResponses.NamePrompt,
                stepContext.Context.Activity.Locale);

            return await stepContext.PromptAsync(DialogIds.MessagePrompt, new PromptOptions { Prompt = prompt });
        }

        private async Task<DialogTurnResult> PrintMessage(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var values = new StringDictionary()
            {
                { "Name", stepContext.Result.ToString() }
            };

            var response = ResponseManager.GetResponse(
                SampleResponses.HaveNameMessage,
                stepContext.Context.Activity.Locale,
                values);

            await stepContext.Context.SendActivityAsync(response);
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
