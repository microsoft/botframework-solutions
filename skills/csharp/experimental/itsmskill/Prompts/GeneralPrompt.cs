// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace ITSMSkill.Prompts
{
    public class GeneralPrompt : Prompt<GeneralLuis.Intent>
    {
        private readonly ISet<GeneralLuis.Intent> intents;
        private readonly IStatePropertyAccessor<SkillState> stateAccessor;

        public GeneralPrompt(string dialogId, ISet<GeneralLuis.Intent> intents, IStatePropertyAccessor<SkillState> stateAccessor, PromptValidator<GeneralLuis.Intent> validator = null, string defaultLocale = null)
: base(dialogId, validator)
        {
            this.intents = intents;
            this.stateAccessor = stateAccessor;
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

        protected override async Task<PromptRecognizerResult<GeneralLuis.Intent>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var result = new PromptRecognizerResult<GeneralLuis.Intent>();

            var skillState = await stateAccessor.GetAsync(turnContext);

            if (intents.Contains(skillState.GeneralIntent))
            {
                result.Succeeded = true;
                result.Value = skillState.GeneralIntent;
            }

            return await Task.FromResult(result);
        }
    }
}
