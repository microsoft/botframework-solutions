using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Contextual.Models.Algorithm;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Dialogs
{
    public class ResolveUnknownUtteranceDialog : ComponentDialog
    {
        private const string _textPrompt = "TextPrompt";
        private const string _resolveUnknownUtteranceDialog = "ResolveUnknownUtterance";
        private const double _threshold = 0.5;

        public ResolveUnknownUtteranceDialog(
            IBotTelemetryClient telemetryClient,
            IList<string> prebuildTriggerUtterances)
            : base(nameof(ResolveUnknownUtteranceDialog))
        {
            TelemetryClient = telemetryClient;

            ResponseManager = new ResponseManager(
                new string[] { "en", "de", "es", "fr", "it", "zh" },
                new ResolveUnknownUtteranceResponses());

            PrebuildUtterances = prebuildTriggerUtterances;

            var resolveUnknownUtterance = new WaterfallStep[]
            {
                ConfirmUnknownIntent,
                AfterConfirmUnknownIntent
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new TextPrompt(_textPrompt));
            AddDialog(new WaterfallDialog(_resolveUnknownUtteranceDialog, resolveUnknownUtterance) { TelemetryClient = telemetryClient });
            InitialDialogId = _resolveUnknownUtteranceDialog;
        }

        protected ResponseManager ResponseManager { get; set; }

        protected IList<string> PrebuildUtterances { get; set; }

        protected static string SimularUtterance { get; set; }

        public async Task<DialogTurnResult> ConfirmUnknownIntent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                sc.Context.Activity.Properties.TryGetValue("OriginText", out var input);
                var originalInput = input != null ? input.ToString() : sc.Context.Activity.Text;

                var result = GetTopSimularUtterance(originalInput);
                SimularUtterance = result;
                if (string.IsNullOrWhiteSpace(result))
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ResolveUnknownUtteranceResponses.DidNotUnderstand));
                    return await sc.EndDialogAsync();
                }

                var prompt = ResponseManager.GetResponse(
                    ResolveUnknownUtteranceResponses.PromptUnknownUtterance,
                    new StringDictionary()
                    {
                        { "SimularUtterance", result },
                    });
                return await sc.PromptAsync(_textPrompt, new PromptOptions { Prompt = prompt });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterConfirmUnknownIntent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                sc.Context.Activity.Properties.TryGetValue("OriginText", out var entity);
                var entityInput = entity != null ? entity.ToString() : sc.Context.Activity.Text;

                // Todo
                if (entityInput == "yes")
                {
                    sc.Context.Activity.Text = SimularUtterance;
                    RouterDialogTurnResult routerDialogTurnResult = new RouterDialogTurnResult(RouterDialogTurnStatus.Restart);
                    return await sc.EndDialogAsync(routerDialogTurnResult);
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        protected async Task HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            // send trace back to emulator
            var trace = new Microsoft.Bot.Schema.Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace);

            // log exception
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });
        }

        private string GetTopSimularUtterance(string userInput)
        {
            string result = string.Empty;
            double maxScore = 0.0;

            // First, try to get result
            foreach (var utterance in PrebuildUtterances)
            {
                var simularity = LevenshteinDistanceSimilarity.CalculateSimilarityByWord(userInput, utterance);

                if (simularity > _threshold && simularity > maxScore)
                {
                    maxScore = simularity;
                    result = utterance;
                }
            }

            // If cannot get result by word, try get simularity by char
            if (string.IsNullOrWhiteSpace(result))
            {
                foreach (var utterance in PrebuildUtterances)
                {
                    var simularity = LevenshteinDistanceSimilarity.CalculateSimilarityByChar(userInput, utterance);

                    if (simularity > _threshold && simularity > maxScore)
                    {
                        maxScore = simularity;
                        result = utterance;
                    }
                }
            }

            return result;
        }
    }
}
