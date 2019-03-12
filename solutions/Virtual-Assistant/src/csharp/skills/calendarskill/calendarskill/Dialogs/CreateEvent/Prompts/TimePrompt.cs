﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.CreateEvent.Prompts.Options;
using CalendarSkill.Dialogs.CreateEvent.Resources;
using CalendarSkill.Util;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DateTime;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Dialogs.CreateEvent.Prompts
{
    public class TimePrompt : Prompt<IList<DateTimeResolution>>
    {
        public TimePrompt(string dialogId, PromptValidator<IList<DateTimeResolution>> validator = null, string defaultLocale = null)
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

            if (!(options is NoSkipPromptOptions))
            {
                throw new Exception(nameof(options) + " should be NoSkipPromptOptions");
            }

            NoSkipPromptOptions noSkipOption = (NoSkipPromptOptions)options;

            if (isRetry && noSkipOption.RetryPrompt != null && !IsSkip)
            {
                await turnContext.SendActivityAsync(noSkipOption.RetryPrompt, cancellationToken).ConfigureAwait(false);
            }
            else if (IsSkip && noSkipOption.NoSkipPrompt != null)
            {
                await turnContext.SendActivityAsync(noSkipOption.NoSkipPrompt, cancellationToken).ConfigureAwait(false);
            }
            else if (noSkipOption.Prompt != null)
            {
                await turnContext.SendActivityAsync(noSkipOption.Prompt, cancellationToken).ConfigureAwait(false);
            }
        }

        protected override async Task<PromptRecognizerResult<IList<DateTimeResolution>>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var result = new PromptRecognizerResult<IList<DateTimeResolution>>();
            IsSkip = false;
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var message = turnContext.Activity.AsMessageActivity();
                var culture = turnContext.Activity.Locale ?? DefaultLocale ?? English;
                IList<DateTimeResolution> date = GetTimeFromMessage(message.Text, culture);
                if (date.Count > 0)
                {
                    result.Succeeded = true;
                    result.Value = date;
                }
            }

            return await Task.FromResult(result);
        }

        private IList<DateTimeResolution> GetTimeFromMessage(string message, string culture)
        {
            if (CreateEventWhiteList.IsSkip(message))
            {
                // Can not skip
                IsSkip = true;
                return new List<DateTimeResolution>();
            }

            IList<DateTimeResolution> results = RecognizeDateTime(message, culture);

            return results;
        }

        private List<DateTimeResolution> RecognizeDateTime(string dateTimeString, string culture)
        {
            var results = DateTimeRecognizer.RecognizeDateTime(DateTimeHelper.ConvertNumberToDateTimeString(dateTimeString, false), culture, options: DateTimeOptions.CalendarMode);
            if (results.Count > 0)
            {
                // Return list of resolutions from first match
                var result = new List<DateTimeResolution>();
                var values = (List<Dictionary<string, string>>)results[0].Resolution["values"];
                foreach (var value in values)
                {
                    if (ContainsTime(value))
                    {
                        result.Add(ReadResolution(value));
                    }
                }

                return result;
            }

            return new List<DateTimeResolution>();
        }

        private bool ContainsTime(IDictionary<string, string> resolution)
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
