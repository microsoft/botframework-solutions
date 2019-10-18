// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.LanguageGeneration.Generators;
using Microsoft.Extensions.DependencyInjection;
using VirtualAssistantSample.Models;

namespace VirtualAssistantSample.Dialogs
{
    /// <summary>
    /// An example on-boarding dialog to greet the user on their first conversation and collection some initial user profile information.
    /// </summary>
    public class OnboardingDialog : ComponentDialog
    {
        private TemplateEngine _templateEngine;
        private ILanguageGenerator _langGenerator;
        private TextActivityGenerator _activityGenerator;
        private IStatePropertyAccessor<UserProfileState> _accessor;

        public OnboardingDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(OnboardingDialog))
        {
            _templateEngine = serviceProvider.GetService<TemplateEngine>();
            _langGenerator = serviceProvider.GetService<ILanguageGenerator>();
            _activityGenerator = serviceProvider.GetService<TextActivityGenerator>();

            var userState = serviceProvider.GetService<UserState>();
            _accessor = userState.CreateProperty<UserProfileState>(nameof(UserProfileState));

            var onboarding = new WaterfallStep[]
            {
                AskForName,
                FinishOnboardingDialog,
            };

            // To capture built-in waterfall dialog telemetry, set the telemetry client
            // to the new waterfall dialog and add it to the component dialog
            TelemetryClient = telemetryClient;
            AddDialog(new WaterfallDialog(nameof(onboarding), onboarding) { TelemetryClient = telemetryClient });
            AddDialog(new TextPrompt(DialogIds.NamePrompt));
        }

        public async Task<DialogTurnResult> AskForName(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await _accessor.GetAsync(sc.Context, () => new UserProfileState());

            if (!string.IsNullOrEmpty(state.Name))
            {
                return await sc.NextAsync(state.Name);
            }
            else
            {
                var template = _templateEngine.EvaluateTemplate("namePrompt");
                var activity = await _activityGenerator.CreateActivityFromText(template, null, sc.Context, _langGenerator);

                return await sc.PromptAsync(DialogIds.NamePrompt, new PromptOptions()
                {
                    Prompt = activity,
                });
            }
        }

        public async Task<DialogTurnResult> FinishOnboardingDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await _accessor.GetAsync(sc.Context, () => new UserProfileState());

            // Ensure the name is capitalised ready for future use.
            var name = (string)sc.Result;
            state.Name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());

            await _accessor.SetAsync(sc.Context, state, cancellationToken);

            var template = _templateEngine.EvaluateTemplate("haveNameMessage", state);
            var activity = await _activityGenerator.CreateActivityFromText(template, state, sc.Context, _langGenerator);
            await sc.Context.SendActivityAsync(activity);
            return await sc.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string NamePrompt = "namePrompt";
        }
    }
}
