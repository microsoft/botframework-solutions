using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.Shared.Resources.Strings;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using static CalendarSkill.Models.CreateEventStateModel;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Dialogs.CreateEvent.Prompts
{
    public class GetRecreateInfoPrompt : Prompt<RecreateEventState?>
    {
        public GetRecreateInfoPrompt(string dialogId, PromptValidator<RecreateEventState?> validator = null, string defaultLocale = null)
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
        }

        protected override async Task<PromptRecognizerResult<RecreateEventState?>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var result = new PromptRecognizerResult<RecreateEventState?>();
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var message = turnContext.Activity.AsMessageActivity();
                var culture = turnContext.Activity.Locale ?? DefaultLocale ?? English;
                RecreateEventState? recreateState = GetStateFromMessage(message.Text, culture);
                if (recreateState != null)
                {
                    result.Succeeded = true;
                    result.Value = recreateState;
                }
            }

            return await Task.FromResult(result);
        }

        private RecreateEventState? GetStateFromMessage(string message, string culture)
        {
            RecreateEventState? result = null;
            message = message.ToLower();

            // useregex and  exactly match for now. We may discuss about to use white list or Luis in future.
            // check is no or cancel
            Regex regex = new Regex(CalendarCommonStrings.CancelAdjust);
            if (regex.IsMatch(message))
            {
                result = RecreateEventState.Cancel;
                return result;
            }

            // check is change time
            regex = new Regex(CalendarCommonStrings.AdjustTime);
            if (regex.IsMatch(message))
            {
                result = RecreateEventState.Time;
                return result;
            }

            // check is change duration
            regex = new Regex(CalendarCommonStrings.AdjustDuration);
            if (regex.IsMatch(message))
            {
                result = RecreateEventState.Duration;
                return result;
            }

            // check is change subject
            regex = new Regex(CalendarCommonStrings.AdjustSubject);
            if (regex.IsMatch(message))
            {
                result = RecreateEventState.Subject;
                return result;
            }

            // check is change content
            regex = new Regex(CalendarCommonStrings.AdjustContent);
            if (regex.IsMatch(message))
            {
                result = RecreateEventState.Content;
                return result;
            }

            // check is change location
            regex = new Regex(CalendarCommonStrings.AdjustLocation);
            if (regex.IsMatch(message))
            {
                result = RecreateEventState.Location;
                return result;
            }

            // check is change participants
            regex = new Regex(CalendarCommonStrings.AdjustParticipants);
            if (regex.IsMatch(message))
            {
                result = RecreateEventState.Participants;
                return result;
            }

            // no match, go to exactly match
            try
            {
                result = (RecreateEventState)Enum.Parse(typeof(RecreateEventState), message, true);
            }
            catch (ArgumentException)
            {
                // user input can not be recognize as recreate state.
                // do nothing, just let the result be null and it will prompt again.
            }
            catch (Exception e)
            {
                // other exception
                // will handle exception and log outside.
                throw e;
            }

            return result;
        }
    }
}
