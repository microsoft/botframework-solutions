using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Responses.Shared;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Schema;

namespace ITSMSkill.Prompts
{
    public class AttributePrompt : Prompt<AttributeType?>
    {
        private readonly bool hasYesNo;
        private readonly AttributeType[] attributes;

        public AttributePrompt(string dialogId, AttributeType[] attributes, bool hasYesNo, PromptValidator<AttributeType?> validator = null, string defaultLocale = null)
       : base(dialogId, validator)
        {
            this.hasYesNo = hasYesNo;
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

                if (hasYesNo)
                {
                    var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(message.Text, turnContext.Activity.Locale);
                    if (promptRecognizerResult.Succeeded)
                    {
                        result.Succeeded = true;
                        if (promptRecognizerResult.Value)
                        {
                            result.Value = AttributeType.None;
                        }
                        else
                        {
                            result.Value = null;
                        }
                    }
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
                case AttributeType.Description: return message.Equals(SharedStrings.AttributeDescription);
                case AttributeType.Urgency: return message.Equals(SharedStrings.AttributeUrgency);
                default: return false;
            }
        }
    }
}
