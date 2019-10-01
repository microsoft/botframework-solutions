// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.LanguageGeneration.Generators;
using VirtualAssistantSample.Services;

namespace VirtualAssistantSample.Dialogs
{
    public class EscalateDialog : ComponentDialog
    {
        private TemplateEngine _templateEngine;
        private ILanguageGenerator _langGenerator;
        private TextActivityGenerator _activityGenerator;

        public EscalateDialog(
            BotServices botServices,
            TemplateEngine templateEngine,
            ILanguageGenerator langGenerator,
            TextActivityGenerator activityGenerator,
            IBotTelemetryClient telemetryClient)
            : base(nameof(EscalateDialog))
        {
            _templateEngine = templateEngine;
            _langGenerator = langGenerator;
            _activityGenerator = activityGenerator;
            InitialDialogId = nameof(EscalateDialog);

            var escalate = new WaterfallStep[]
            {
                SendPhone,
            };

            AddDialog(new WaterfallDialog(InitialDialogId, escalate));
        }

        private async Task<DialogTurnResult> SendPhone(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var template = _templateEngine.EvaluateTemplate("escalateMessage");
            var activity = await _activityGenerator.CreateActivityFromText(template, null, sc.Context, _langGenerator);
            await sc.Context.SendActivityAsync(activity);
            return await sc.EndDialogAsync();
        }
    }
}
