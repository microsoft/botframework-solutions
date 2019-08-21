using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DateTime;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Prompts
{
    /// <summary>
    /// Prompt meeting start time or title to get a list of meetings.
    /// </summary>
    public class GetEventPrompt : Prompt<IList<EventModel>>
    {
        private static ICalendarService calendarService = null;
        private static TimeZoneInfo userTimeZone = null;

        public GetEventPrompt(string dialogId, PromptValidator<IList<EventModel>> validator = null, string defaultLocale = null)
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

            if (!(options is GetEventOptions))
            {
                throw new Exception(nameof(options) + " should be GetEventOptions");
            }

            if (calendarService == null || userTimeZone == null)
            {
                var getEventOptions = (GetEventOptions)options;
                calendarService = getEventOptions.CalendarService;
                userTimeZone = getEventOptions.TimeZone;
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

        protected override async Task<PromptRecognizerResult<IList<EventModel>>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var result = new PromptRecognizerResult<IList<EventModel>>();
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var message = turnContext.Activity.AsMessageActivity();
                var culture = turnContext.Activity.Locale ?? DefaultLocale ?? English;
                var date = GetTimeFromMessage(message.Text, culture);
                if (date.Count > 0)
                {
                    // input is a time
                    var results = await GetEventsWithStartTime(date, message.Text);
                    if (results.Count > 0)
                    {
                        result.Succeeded = true;
                        result.Value = results;
                    }
                }

                if (!result.Succeeded)
                {
                    var results = await GetEventsWithTitle(message.Text);
                    if (results.Count > 0)
                    {
                        result.Succeeded = true;
                        result.Value = results;
                    }
                }
            }

            return await Task.FromResult(result);
        }

        private async Task<IList<EventModel>> GetEventsWithStartTime(IList<DateTimeResolution> dateTimeResolutions, string message)
        {
            IList<EventModel> events = new List<EventModel>();
            if (dateTimeResolutions.Count > 0)
            {
                foreach (var resolution in dateTimeResolutions)
                {
                    if (resolution.Value == null)
                    {
                        continue;
                    }

                    var startTimeValue = DateTime.Parse(resolution.Value);
                    if (startTimeValue == null)
                    {
                        continue;
                    }

                    var dateTimeConvertType = resolution.Timex;
                    var isRelativeTime = IsRelativeTime(message, dateTimeResolutions[0].Value, dateTimeResolutions[0].Timex);
                    startTimeValue = isRelativeTime ? TimeZoneInfo.ConvertTime(startTimeValue, TimeZoneInfo.Local, userTimeZone) : startTimeValue;

                    startTimeValue = TimeConverter.ConvertLuisLocalToUtc(startTimeValue, userTimeZone);
                    events = await calendarService.GetEventsByStartTime(startTimeValue);
                    if (events != null && events.Count > 0)
                    {
                        break;
                    }
                }
            }

            return events;
        }

        private async Task<IList<EventModel>> GetEventsWithTitle(string title)
        {
            IList<EventModel> events = await calendarService.GetEventsByTitle(title);
            return events;
        }

        private bool IsRelativeTime(string userInput, string resolverResult, string timex)
        {
            if (userInput.Contains("ago") ||
                userInput.Contains("before") ||
                userInput.Contains("later") ||
                userInput.Contains("next"))
            {
                return true;
            }

            if (userInput.Contains("today") ||
                userInput.Contains("now") ||
                userInput.Contains("yesterday") ||
                userInput.Contains("tomorrow"))
            {
                return true;
            }

            if (timex == "PRESENT_REF")
            {
                return true;
            }

            return false;
        }

        private IList<DateTimeResolution> GetTimeFromMessage(string message, string culture)
        {
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
