using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.Shared.Resources.Strings;
using EmailSkill.Responses.Shared;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Schema;
using static EmailSkill.Models.SendEmailStateModel;

namespace EmailSkill.Dialogs.SendEmail.Prompts
{
    public class GetRecreateInfoPrompt : Prompt<ResendEmailState?>
    {
        public GetRecreateInfoPrompt(string dialogId, PromptValidator<ResendEmailState?> validator = null, string defaultLocale = null)
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

        protected override async Task<PromptRecognizerResult<ResendEmailState?>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var result = new PromptRecognizerResult<ResendEmailState?>();
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var message = turnContext.Activity.AsMessageActivity();
                ResendEmailState? recreateState = GetStateFromMessage(message.Text);

                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(message.Text, turnContext.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                {
                    recreateState = ResendEmailState.Cancel;
                }

                if (recreateState != null)
                {
                    result.Succeeded = true;
                    result.Value = recreateState;
                }
            }

            return await Task.FromResult(result);
        }

        private ResendEmailState? GetStateFromMessage(string message)
        {
            ResendEmailState? result = null;

            if (message.ToLowerInvariant().Equals(EmailCommonStrings.Participants))
            {
                result = ResendEmailState.Participants;
            }
            else if (message.ToLowerInvariant().Equals(EmailCommonStrings.Subject))
            {
                result = ResendEmailState.Subject;
            }
            else if (message.ToLowerInvariant().Equals(EmailCommonStrings.Content))
            {
                result = ResendEmailState.Content;
            }

            return result;
        }
    }
}
