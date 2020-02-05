// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Bot.Schema;

namespace ITSMSkill.Prompts
{
    public class AttributeWithNoPrompt : Prompt<AttributeType?>
    {
        private readonly AttributeType[] attributes;

        public AttributeWithNoPrompt(string dialogId, AttributeType[] attributes, PromptValidator<AttributeType?> validator = null, string defaultLocale = null)
       : base(dialogId, validator)
        {
            this.attributes = attributes;
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

        protected override async Task<PromptRecognizerResult<AttributeType?>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var result = new PromptRecognizerResult<AttributeType?>();
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var message = turnContext.Activity.AsMessageActivity();

                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(message.Text, turnContext.Activity.Locale);
                if (promptRecognizerResult.Succeeded && !promptRecognizerResult.Value)
                {
                    result.Succeeded = true;
                    result.Value = null;
                }

                if (!result.Succeeded)
                {
                    var text = message.Text.ToLowerInvariant();
                    foreach (var attribute in attributes)
                    {
                        if (IsMessageAttributeMatch(text, attribute))
                        {
                            result.Succeeded = true;
                            result.Value = attribute;
                            break;
                        }
                    }
                }
            }

            return await Task.FromResult(result);
        }

        private bool IsMessageAttributeMatch(string message, AttributeType attribute)
        {
            switch (attribute)
            {
                case AttributeType.None: return false;
                default: return message.Equals(attribute.ToLocalizedString());
            }
        }
    }
}
