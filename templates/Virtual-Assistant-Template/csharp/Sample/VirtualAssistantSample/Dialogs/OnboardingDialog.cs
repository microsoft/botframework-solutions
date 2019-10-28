﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using VirtualAssistantSample.Models;

namespace VirtualAssistantSample.Dialogs
{
    // Example onboarding dialog to initial user profile information.
    public class OnboardingDialog : ComponentDialog
    {
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

            // Captialize name
            userProfile.Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());

            await _accessor.SetAsync(sc.Context, userProfile, cancellationToken);

            await sc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("HaveNameMessage", userProfile));
            await sc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("FirstPromptMessage", userProfile));

            sc.SuppressCompletionMessage(true);

            return await sc.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string NamePrompt = "namePrompt";
        }
    }
}
