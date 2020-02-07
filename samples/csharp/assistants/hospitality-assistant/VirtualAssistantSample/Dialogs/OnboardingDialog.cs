// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using VirtualAssistantSample.Models;
using VirtualAssistantSample.Services;

namespace VirtualAssistantSample.Dialogs
{
    // Example onboarding dialog to initial user profile information.
    public class OnboardingDialog : ComponentDialog
    {
        private BotServices _services;
        private LocaleTemplateEngineManager _templateEngine;
        private IStatePropertyAccessor<UserProfileState> _accessor;

        public OnboardingDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(OnboardingDialog))
        {
            _templateEngine = serviceProvider.GetService<LocaleTemplateEngineManager>();

            var userState = serviceProvider.GetService<UserState>();
            _accessor = userState.CreateProperty<UserProfileState>(nameof(UserProfileState));
            _services = serviceProvider.GetService<BotServices>();

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
                return await sc.PromptAsync(DialogIds.NamePrompt, new PromptOptions()
                {
                    Prompt = _templateEngine.GenerateActivityForLocale("NamePrompt"),
                });
            }
        }

        public async Task<DialogTurnResult> FinishOnboardingDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userProfile = await _accessor.GetAsync(sc.Context, () => new UserProfileState());
            var name = (string)sc.Result;

            var generalResult = sc.Context.TurnState.Get<GeneralLuis>(StateProperties.GeneralResult);
            if (generalResult == null)
            {
                var localizedServices = _services.GetCognitiveModels();
                generalResult = await localizedServices.LuisServices["General"].RecognizeAsync<GeneralLuis>(sc.Context, cancellationToken);
            }

            (var generalIntent, var generalScore) = generalResult.TopIntent();
            if (generalIntent == GeneralLuis.Intent.ExtractName && generalScore > 0.5)
            {
                if (generalResult.Entities.PersonName_Any != null)
                {
                    name = generalResult.Entities.PersonName_Any[0];
                }
                else if (generalResult.Entities.personName != null)
                {
                    name = generalResult.Entities.personName[0];
                }
            }

            // Captialize name
            userProfile.Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());

            await _accessor.SetAsync(sc.Context, userProfile, cancellationToken);

            await sc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("HaveNameMessage", userProfile));

            return await sc.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string NamePrompt = "namePrompt";
        }

        private class StateProperties
        {
            public const string GeneralResult = "generalResult";
        }
    }
}
