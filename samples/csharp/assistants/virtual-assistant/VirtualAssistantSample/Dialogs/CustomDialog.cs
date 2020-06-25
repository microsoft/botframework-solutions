// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using VirtualAssistantSample.Models;
using VirtualAssistantSample.Services;

namespace VirtualAssistantSample.Dialogs
{
    // Example onboarding dialog to initial user profile information.
    public class CustomDialog : ComponentDialog
    {
        private readonly BotServices _services;
        private readonly LocaleTemplateManager _templateManager;
        private readonly IStatePropertyAccessor<UserProfileState> _accessor;

        public CustomDialog(
            IServiceProvider serviceProvider)
            : base(nameof(OnboardingDialog))
        {
            _templateManager = serviceProvider.GetService<LocaleTemplateManager>();

            var userState = serviceProvider.GetService<UserState>();
            _accessor = userState.CreateProperty<UserProfileState>(nameof(UserProfileState));
            _services = serviceProvider.GetService<BotServices>();

            var onboarding = new WaterfallStep[]
            {
                AskForNameAsync
            };

            AddDialog(new WaterfallDialog(nameof(onboarding), onboarding));
            AddDialog(new TextPrompt(DialogIds.NamePrompt));
        }

        public async Task<DialogTurnResult> AskForNameAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var activity = sc.Context.Activity;
            activity.Text = "run sample dialog";
            activity.Type = "message";
            var skillDialogArgs = new BeginSkillDialogOptions { Activity = (Activity)activity };

            return await sc.BeginDialogAsync("SampleSkill", skillDialogArgs);
        }

        public async Task<DialogTurnResult> FinishOnboardingDialogAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {

            var userProfile = await _accessor.GetAsync(sc.Context, () => new UserProfileState(), cancellationToken);
            var name = (string)sc.Result;

            var generalResult = sc.Context.TurnState.Get<GeneralLuis>(StateProperties.GeneralResult);
            if (generalResult == null)
            {
                var localizedServices = _services.GetCognitiveModels();
                generalResult = await localizedServices.LuisServices["General"].RecognizeAsync<GeneralLuis>(sc.Context, cancellationToken);
                sc.Context.TurnState.Add(StateProperties.GeneralResult, generalResult);
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

            // Capitalize name
            userProfile.Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());

            await _accessor.SetAsync(sc.Context, userProfile, cancellationToken);

            await sc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale("HaveNameMessage", userProfile), cancellationToken);

            return await sc.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private static class DialogIds
        {
            public const string NamePrompt = "namePrompt";
        }
    }
}
