// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.Number;
using static Microsoft.Recognizers.Text.Culture;

namespace ITSMSkill.Prompts
{
    public class TicketNumberPrompt : Prompt<string>
    {
        public TicketNumberPrompt(string dialogId, PromptValidator<string> validator = null, string defaultLocale = null)
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

        protected override async Task<PromptRecognizerResult<string>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var result = new PromptRecognizerResult<string>();

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var message = turnContext.Activity.AsMessageActivity().Text.ToUpper();

                var regex = new Regex("INC[0-9]{7}");
                if (regex.IsMatch(message))
                {
                    result.Succeeded = true;
                    result.Value = message;
                }

                if (!result.Succeeded)
                {
                    var culture = turnContext.Activity.Locale ?? DefaultLocale ?? English;
                    var results = NumberRecognizer.RecognizeNumber(message, culture);
                    if (results.Count > 0)
                    {
                        var text = results[0].Resolution["value"].ToString();
                        if (int.TryParse(text, NumberStyles.Any, new CultureInfo(culture), out var value))
                        {
                            if (value >= 1 && value <= 9999999)
                            {
                                result.Succeeded = true;
                                result.Value = $"INC{value:D7}";
                            }
                        }
                    }
                }
            }

            return await Task.FromResult(result);
        }
    }
}
