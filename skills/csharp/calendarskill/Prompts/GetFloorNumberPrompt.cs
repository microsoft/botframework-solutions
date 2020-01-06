using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using CalendarSkill.Models;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.Number;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Prompts
{
    /// <summary>
    /// Prompt meeting start time or title to get a list of meetings.
    /// </summary>
    public class GetFloorNumberPrompt : Prompt<int?>
    {
        internal const string AttemptCountKey = "AttemptCount";

        private static int maxReprompt = -1;

        public GetFloorNumberPrompt(
            string dialogId,
            PromptValidator<int?> validator = null,
            string defaultLocale = null)
               : base(dialogId, validator)
        {
            DefaultLocale = defaultLocale;
        }

        public string DefaultLocale { get; set; }

        protected override async Task OnPromptAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, bool isRetry, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (isRetry && options.RetryPrompt != null)
            {
                await turnContext.SendActivityAsync(options.RetryPrompt, cancellationToken).ConfigureAwait(false);
            }
            else if (options.Prompt != null)
            {
                await turnContext.SendActivityAsync(options.Prompt, cancellationToken).ConfigureAwait(false);
            }

            maxReprompt = ((CalendarPromptOptions)options).MaxReprompt;
        }

        protected override async Task<PromptRecognizerResult<int?>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var result = new PromptRecognizerResult<int?>();

            var luisResult = turnContext.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
            if (luisResult != null && luisResult.TopIntent().intent == CalendarLuis.Intent.RejectCalendar && luisResult.TopIntent().score > 0.8)
            {
                result.Succeeded = true;
            }
            else
            {
                var message = turnContext.Activity.AsMessageActivity();
                int? floorNumber = ParseFloorNumber(message.Text, turnContext.Activity.Locale ?? English);
                if (floorNumber != null)
                {
                    result.Succeeded = true;
                    result.Value = floorNumber;
                }
            }

            if (maxReprompt > 0 && Convert.ToInt32(state[AttemptCountKey]) >= maxReprompt)
            {
                result.Succeeded = true;
            }

            return await Task.FromResult(result);
        }

        private int? ParseFloorNumber(string utterance, string culture)
        {
            var model_ordinal = new NumberRecognizer(culture).GetOrdinalModel(culture);
            var result = model_ordinal.Parse(utterance);
            if (result.Any())
            {
                return int.Parse(result.First().Resolution["value"].ToString());
            }
            else
            {
                var model_number = new NumberRecognizer(culture).GetNumberModel(culture);
                result = model_number.Parse(utterance);
                if (result.Any())
                {
                    return int.Parse(result.First().Resolution["value"].ToString());
                }
            }

            return null;
        }
    }
}
