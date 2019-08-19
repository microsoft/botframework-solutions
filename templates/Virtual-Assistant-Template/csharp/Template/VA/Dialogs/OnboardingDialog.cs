// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using $safeprojectname$.Models;
using $safeprojectname$.Responses.Onboarding;
using $safeprojectname$.Services;

namespace $safeprojectname$.Dialogs
{
    public class OnboardingDialog : ComponentDialog
    {
        private static OnboardingResponses _responder = new OnboardingResponses();
        private IStatePropertyAccessor<OnboardingState> _accessor;
        private OnboardingState _state;
        private BotServices _services;

        public OnboardingDialog(
            BotServices botServices,
            UserState userState,
            IBotTelemetryClient telemetryClient)
            : base(nameof(OnboardingDialog))
        {
            _accessor = userState.CreateProperty<OnboardingState>(nameof(OnboardingState));
            InitialDialogId = nameof(OnboardingDialog);
            _services = botServices;

            var onboarding = new WaterfallStep[]
            {
                AskForName,
                FinishOnboardingDialog,
            };

            // To capture built-in waterfall dialog telemetry, set the telemetry client
            // to the new waterfall dialog and add it to the component dialog
            TelemetryClient = telemetryClient;
            AddDialog(new WaterfallDialog(InitialDialogId, onboarding) { TelemetryClient = telemetryClient });
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
                return await sc.PromptAsync(DialogIds.NamePrompt, new PromptOptions()
                {
                    Prompt = await _responder.RenderTemplate(sc.Context, sc.Context.Activity.Locale, OnboardingResponses.ResponseIds.NamePrompt),
                });
            }
        }

        public async Task<DialogTurnResult> FinishOnboardingDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            _state = await _accessor.GetAsync(sc.Context, () => new OnboardingState());
            var name = _state.Name;
            if (string.IsNullOrEmpty(name))
            {
                name = _state.Name = (string)sc.Result;
                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var cognitiveModels = _services.CognitiveModelSets[locale];
                cognitiveModels.LuisServices.TryGetValue("Onboarding", out var luisService);
                if (luisService != null)
                {
                    var luisResult = await luisService.RecognizeAsync<OnboardingLuis>(sc.Context, cancellationToken);
                    var intent = luisResult.TopIntent().intent;
                    var score = luisResult.TopIntent().score;
                    if (intent == OnboardingLuis.Intent.NameExtraction && score > 0.5 && luisResult.Entities.personName != null)
                    {
                        name = _state.Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(luisResult.Entities.personName[0]);
                    }
                }
            }

            await _accessor.SetAsync(sc.Context, _state, cancellationToken);
            await _responder.ReplyWith(sc.Context, OnboardingResponses.ResponseIds.HaveNameMessage, new { name });
            return await sc.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string NamePrompt = "namePrompt";
        }
    }
}
