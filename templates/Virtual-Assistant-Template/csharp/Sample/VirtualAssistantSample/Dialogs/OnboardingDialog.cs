// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.LanguageGeneration.Generators;
using Newtonsoft.Json.Linq;
using VirtualAssistantSample.Models;
using VirtualAssistantSample.Services;

namespace VirtualAssistantSample.Dialogs
{
    public class OnboardingDialog : ComponentDialog
    {
        private IStatePropertyAccessor<OnboardingState> _accessor;
        private OnboardingState _state;

        public OnboardingDialog(
            BotServices botServices,
            UserState userState,
            IBotTelemetryClient telemetryClient)
            : base(nameof(OnboardingDialog))
        {
            _accessor = userState.CreateProperty<OnboardingState>(nameof(OnboardingState));
            InitialDialogId = nameof(OnboardingDialog);

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
            var activityGenerator = sc.Context.TurnState.Get<IActivityGenerator>();
            _state = await _accessor.GetAsync(sc.Context, () => new OnboardingState());

            if (!string.IsNullOrEmpty(_state.Name))
            {
                return await sc.NextAsync(_state.Name);
            }
            else
            {
                var activity = await activityGenerator.Generate(sc.Context, "namePrompt", null);
                return await sc.PromptAsync(DialogIds.NamePrompt, new PromptOptions()
                {
                    Prompt = activity,
                });
            }
        }

        public async Task<DialogTurnResult> FinishOnboardingDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var activityGenerator = sc.Context.TurnState.Get<IActivityGenerator>();

            _state = await _accessor.GetAsync(sc.Context, () => new OnboardingState());
            var name = _state.Name = (string)sc.Result;
            await _accessor.SetAsync(sc.Context, _state, cancellationToken);

            var activity = await activityGenerator.Generate(sc.Context, "haveNameMessage", new { name = _state.Name });
            await sc.Context.SendActivityAsync(activity);
            return await sc.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string NamePrompt = "namePrompt";
        }
    }
}
