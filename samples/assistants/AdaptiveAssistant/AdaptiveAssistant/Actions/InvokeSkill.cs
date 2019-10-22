using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Expressions.Parser;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdaptiveAssistant.Actions
{
    public class InvokeSkill : BaseInvokeDialog
    {
        private List<SkillManifest> _skills;

        public InvokeSkill(List<SkillManifest> skills)
        {
            _skills = skills;
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var exp = new ExpressionEngine().Parse("turn.recognized");
            (var recognizedIntent, var error) = exp.TryEvaluate(dc.State);

            // Convert to recognizer result
            var result = (recognizedIntent as JObject).ToObject<RecognizerResult>();
            (var intent, var score) = result.GetTopScoringIntent();

            // Threshold for triggering a skill
            if (score > 0.5)
            {
                var identifiedSkill = SkillRouter.IsSkill(_skills, intent);

                if (identifiedSkill != null)
                {
                    var boundOptions = BindOptions(dc, options);

                    return await dc.BeginDialogAsync(identifiedSkill.Id, options: boundOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }

            return await dc.EndDialogAsync();
        }
    }
}
