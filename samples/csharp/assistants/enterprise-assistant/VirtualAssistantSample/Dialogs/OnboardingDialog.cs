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
using Newtonsoft.Json.Linq;
using VirtualAssistantSample.Models;

namespace VirtualAssistantSample.Dialogs
{
    public class OnboardingDialog : ComponentDialog
    {
        private TemplateEngine _templateEngine;
        private ILanguageGenerator _langGenerator;
        private TextActivityGenerator _activityGenerator;
        private IStatePropertyAccessor<OnboardingState> _accessor;
        private OnboardingState _state;

        public OnboardingDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(OnboardingDialog))
        {
            _templateEngine = serviceProvider.GetService<TemplateEngine>();
            _langGenerator = serviceProvider.GetService<ILanguageGenerator>();
            _activityGenerator = serviceProvider.GetService<TextActivityGenerator>();

            var userState = serviceProvider.GetService<UserState>();
            _accessor = userState.CreateProperty<OnboardingState>(nameof(OnboardingState));

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
            _state = await _accessor.GetAsync(sc.Context, () => new OnboardingState());

            if (!string.IsNullOrEmpty(_state.Name))
            {
                return await sc.NextAsync(_state.Name);
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
            _state = await _accessor.GetAsync(sc.Context, () => new OnboardingState());
            var name = _state.Name = (string)sc.Result;
            await _accessor.SetAsync(sc.Context, _state, cancellationToken);

            dynamic data = new JObject();
            data.name = name;
            var template = _templateEngine.EvaluateTemplate("haveNameMessage", data);
            var activity = await _activityGenerator.CreateActivityFromText(template, data, sc.Context, _langGenerator);
            await sc.Context.SendActivityAsync(activity);
            return await sc.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string NamePrompt = "namePrompt";
        }
    }
}
