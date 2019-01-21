using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.CreateEvent.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DateTime;
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

            // use exactly match for now. We may discuss about to use white list or Luis in future.
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
