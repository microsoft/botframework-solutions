using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;

namespace $safeprojectname$.Services
{
    public class SkillIntentRecognizer : ISkillIntentRecognizer
    {
        private BotServices _services;
        private BotSettings _settings;

        public SkillIntentRecognizer(BotServices services, BotSettings settings)
        {
            _services = services;
            _settings = settings;
        }

        public bool ConfirmIntentSwitch { get; } = true;

        public Func<DialogContext, Task<string>> RecognizeSkillIntentAsync
        {
            get { return RecognizeSkillIntentAsyncFunc; }
        }

        public async Task<string> RecognizeSkillIntentAsyncFunc(DialogContext dc)
        {
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var cognitiveModels = _services.CognitiveModelSets[locale];

            // Check dispatch result
            var dispatchResult = await cognitiveModels.DispatchService.RecognizeAsync<DispatchLuis>(dc.Context, CancellationToken.None);
            var intent = dispatchResult.TopIntent().intent;

            // Identify if the dispatch intent matches any action within a Skill if so, we pass to the appropriate SkillDialog to hand-off
            var recognizeSkill = SkillRouter.IsSkill(_settings.Skills, intent.ToString());

            if (recognizeSkill == null)
            {
                if (intent == DispatchLuis.Intent.q_Faq)
                {
                    return "FAQ";
                }
                else if (intent == DispatchLuis.Intent.q_Chitchat)
                {
                    return "Chit chat";
                }
            }
            else
            {
                return recognizeSkill.Id;
            }

            return null;
        }
    }
}