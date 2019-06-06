using AdaptiveAssistant.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Expressions.Parser;
using Microsoft.Bot.Builder.Skills;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdaptiveAssistant.Steps
{
    public class InvokeSkill : DialogCommand
    {
        private BotSettings _settings;

        public InvokeSkill(BotSettings settings)
        {
            _settings = settings;
        }

        protected override async Task<DialogTurnResult> OnRunCommandAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Get intent from state
            var exp = new ExpressionEngine().Parse("turn.dialogevents.recognizedintent");
            (var recognizedIntent, var error) = exp.TryEvaluate(dc.State);

            // Convert to recognizer result
            var result = (recognizedIntent as JObject).ToObject<RecognizerResult>();
            (var intent, var score) = result.GetTopScoringIntent();

            // Threshold for triggering a skill
            if (score > 0.5)
            {
                var identifiedSkill = SkillRouter.IsSkill(_settings.Skills, intent);

                if (identifiedSkill != null)
                {
                    await dc.Parent.BeginDialogAsync(identifiedSkill.Id);
                    return await dc.Parent.ContinueDialogAsync();
                }
            }

            return await dc.EndDialogAsync();
        }
    }
}
