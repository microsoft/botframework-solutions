// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Extensions.DependencyInjection;
using VirtualAssistantSample.Models;
using ActivityGenerator = Microsoft.Bot.Builder.Dialogs.Adaptive.Generators.ActivityGenerator;

namespace VirtualAssistantSample.Dialogs
{
    /// <summary>
    /// An example on-boarding dialog to greet the user on their first conversation and collection some initial user profile information.
    /// </summary>
    public class OnboardingDialog : ComponentDialog
    {
        private TemplateEngine _templateEngine;
        private ILanguageGenerator _langGenerator;
        private IStatePropertyAccessor<UserProfileState> _accessor;

        public OnboardingDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(OnboardingDialog))
        {
            _templateEngine = serviceProvider.GetService<TemplateEngine>();

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
                var activity = ActivityGenerator.GenerateFromLG(_templateEngine.EvaluateTemplate("NamePrompt"));

                return await sc.PromptAsync(DialogIds.NamePrompt, new PromptOptions()
                {
                    Prompt = activity,
                });
            }
        }

        public async Task<DialogTurnResult> FinishOnboardingDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userProfile = await _accessor.GetAsync(sc.Context, () => new UserProfileState());

            // Ensure the name is capitalised ready for future use.
            var name = (string)sc.Result;
            userProfile.Name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());

            await _accessor.SetAsync(sc.Context, userProfile, cancellationToken);

            await sc.Context.SendActivityAsync(ActivityGenerator.GenerateFromLG(_templateEngine.EvaluateTemplate("HaveNameMessage", userProfile)));
            await sc.Context.SendActivityAsync(ActivityGenerator.GenerateFromLG(_templateEngine.EvaluateTemplate("FirstPromptMessage", userProfile)));

            sc.SuppressCompletionMessage(true);

            return await sc.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string NamePrompt = "namePrompt";
        }
    }
}
