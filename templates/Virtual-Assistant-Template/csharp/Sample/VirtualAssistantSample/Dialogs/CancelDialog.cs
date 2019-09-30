// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.LanguageGeneration.Generators;

namespace VirtualAssistantSample.Dialogs
{
    public class CancelDialog : ComponentDialog
    {
        private TemplateEngine _templateEngine;
        private ILanguageGenerator _langGenerator;
        private TextActivityGenerator _activityGenerator;

        public CancelDialog(
            TemplateEngine templateEngine,
            ILanguageGenerator langGenerator,
            TextActivityGenerator activityGenerator)
            : base(nameof(CancelDialog))
        {
            _templateEngine = templateEngine;
            _langGenerator = langGenerator;
            _activityGenerator = activityGenerator;
            InitialDialogId = nameof(CancelDialog);

            var cancel = new WaterfallStep[]
            {
                AskToCancel,
                FinishCancelDialog,
            };

            AddDialog(new WaterfallDialog(InitialDialogId, cancel));
            AddDialog(new ConfirmPrompt(DialogIds.CancelPrompt));
        }

        protected override async Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            var doCancel = (bool)result;

            if (doCancel)
            {
                // If user chose to cancel
                var template = _templateEngine.EvaluateTemplate("cancelConfirmedMessage");
                var activity = await _activityGenerator.CreateActivityFromText(template, null, outerDc.Context, _langGenerator);
                await outerDc.Context.SendActivityAsync(activity);

                // Cancel all in outer stack of component i.e. the stack the component belongs to
                return await outerDc.CancelAllDialogsAsync();
            }
            else
            {
                // else if user chose not to cancel
                var template = _templateEngine.EvaluateTemplate("cancelDeniedMessage");
                var activity = await _activityGenerator.CreateActivityFromText(template, null, outerDc.Context, _langGenerator);
                await outerDc.Context.SendActivityAsync(activity);

                // End this component. Will trigger reprompt/resume on outer stack
                return await outerDc.EndDialogAsync();
            }
        }

        private async Task<DialogTurnResult> AskToCancel(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var template = _templateEngine.EvaluateTemplate("cancelPrompt");
            var activity = await _activityGenerator.CreateActivityFromText(template, null, sc.Context, _langGenerator);

            return await sc.PromptAsync(DialogIds.CancelPrompt, new PromptOptions()
            {
                Prompt = activity
            });
        }

        private async Task<DialogTurnResult> FinishCancelDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.EndDialogAsync((bool)sc.Result);
        }

        private class DialogIds
        {
            public const string CancelPrompt = "cancelPrompt";
        }
    }
}
