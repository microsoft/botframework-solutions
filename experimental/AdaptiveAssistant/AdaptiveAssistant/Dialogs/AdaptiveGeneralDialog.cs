using AdaptiveAssistant.Services;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdaptiveAssistant.Dialogs
{
    public class AdaptiveGeneralDialog : ComponentDialog
    {
        public AdaptiveGeneralDialog(BotServices services)
            : base(nameof(AdaptiveGeneralDialog))
        {
            var localizedServices = services.CognitiveModelSets[CultureInfo.CurrentUICulture.TwoLetterISOLanguageName];

            var generalDialog = new AdaptiveDialog()
            {
                Recognizer = localizedServices.LuisServices["general"],
                Rules = new List<IRule>()
                {
                    new IntentRule(
                        intent: GeneralLuis.Intent.Cancel.ToString(),
                        steps: new List<IDialog>
                        {
                            new TraceActivity(),
                            new SendActivity("cancelConfirmedMessage")
                        }),
                    new IntentRule(
                        intent: GeneralLuis.Intent.Help.ToString(),
                        steps: new List<IDialog>
                        {
                            new TraceActivity(),
                            new SendActivity("helpMessage")
                        }),
                    new IntentRule(
                        intent: GeneralLuis.Intent.Escalate.ToString(),
                        steps: new List<IDialog>
                        {
                            new TraceActivity(),
                            new SendActivity("escalateMessage")
                        })
                }
            };

            AddDialog(generalDialog);
            InitialDialogId = generalDialog.Id;
        }

        public override Task<DialogTurnResult> BeginDialogAsync(DialogContext outerDc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.BeginDialogAsync(outerDc, options, cancellationToken);
        }

        protected override Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.OnBeginDialogAsync(innerDc, options, cancellationToken);
        }

        public override Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.ContinueDialogAsync(dc, cancellationToken);
        }

        protected override Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.OnContinueDialogAsync(innerDc, cancellationToken);
        }
    }
}
