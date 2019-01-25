using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.CreateEvent.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DateTime;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Dialogs.CreateEvent.Prompts
{
    public class DatePrompt : Prompt<IList<DateTimeResolution>>
    {
        public DatePrompt(string dialogId, PromptValidator<IList<DateTimeResolution>> validator = null, string defaultLocale = null)
               : base(dialogId, validator)
        {
            DefaultLocale = defaultLocale;
        }

        public string DefaultLocale { get; set; }

        private bool IsSkip { get; set; } = false;

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
        }

        protected override async Task<PromptRecognizerResult<IList<DateTimeResolution>>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var result = new PromptRecognizerResult<IList<DateTimeResolution>>();
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var message = turnContext.Activity.AsMessageActivity();
                var culture = turnContext.Activity.Locale ?? DefaultLocale ?? English;
                IList<DateTimeResolution> date = GetDateFromMessage(message.Text, culture);
                if (date.Count > 0)
                {
                    result.Succeeded = true;
                    result.Value = date;
                }
            }

            return await Task.FromResult(result);
        }

        private IList<DateTimeResolution> GetDateFromMessage(string message, string culture)
        {
            if (CreateEventWhiteList.IsSkip(message))
            {
                message = "today";

                // log is this one skip. may change logic in future.
                // no use for now
                IsSkip = true;
            }

            IList<DateTimeResolution> results = RecognizeDateTime(message, culture);

            return results;
        }

        private List<DateTimeResolution> RecognizeDateTime(string dateTimeString, string culture)
        {
            var results = DateTimeRecognizer.RecognizeDateTime(dateTimeString, culture);
            if (results.Count > 0)
            {
                // Return list of resolutions from first match
                var result = new List<DateTimeResolution>();
                var values = (List<Dictionary<string, string>>)results[0].Resolution["values"];
                foreach (var value in values)
                {
                    if (ContainsDate(value))
                    {
                        result.Add(ReadResolution(value));
                    }
                }

                return result;
            }

            return new List<DateTimeResolution>();
        }

        private bool ContainsDate(IDictionary<string, string> resolution)
        {
            if (resolution.TryGetValue("value", out var value))
            {
                try
                {
                    var dateTime = DateTime.Parse(value);
                    if (dateTime != null)
                    {
                        return true;
                    }
                }
                catch
                {
                }
            }

            return false;
        }

        private DateTimeResolution ReadResolution(IDictionary<string, string> resolution)
        {
            var result = new DateTimeResolution();

            if (resolution.TryGetValue("timex", out var timex))
            {
                result.Timex = timex;
            }

            if (resolution.TryGetValue("value", out var value))
            {
                result.Value = value;
            }

            if (resolution.TryGetValue("start", out var start))
            {
                result.Start = start;
            }

            if (resolution.TryGetValue("end", out var end))
            {
                result.End = end;
            }

            return result;
        }
    }
}
