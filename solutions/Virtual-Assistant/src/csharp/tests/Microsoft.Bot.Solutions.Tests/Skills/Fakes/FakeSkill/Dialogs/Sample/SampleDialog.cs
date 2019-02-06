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

namespace FakeSkill.Dialogs.Sample
{
    public class SampleDialog : SkillTemplateDialog
    {
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
            var prompt = stepContext.Context.Activity.CreateReply(SampleResponses.MessagePrompt);
            return await stepContext.PromptAsync(DialogIds.MessagePrompt, new PromptOptions { Prompt = prompt });
        }

        private async Task<DialogTurnResult> PrintMessage(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var tokens = new StringDictionary
            {
                { "Message", stepContext.Result.ToString() },
            };

            var response = stepContext.Context.Activity.CreateReply(SampleResponses.MessageResponse, ResponseBuilder, tokens);
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
