// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using VirtualAssistant.Dialogs.Shared;

namespace VirtualAssistant.Dialogs.Onboarding
{
    public class OnboardingDialog : EnterpriseDialog
    {
        // Constants
        public const string NamePrompt = "namePrompt";
        public const string LocationPrompt = "locationPrompt";

        // Fields
        private static OnboardingResponses _responder = new OnboardingResponses();
        private IStatePropertyAccessor<OnboardingState> _accessor;
        private OnboardingState _state;

        public OnboardingDialog(BotServices botServices, IStatePropertyAccessor<OnboardingState> accessor, IBotTelemetryClient telemetryClient)
            : base(botServices, nameof(OnboardingDialog), telemetryClient)
        {
            _accessor = accessor;
            InitialDialogId = nameof(OnboardingDialog);

            var onboarding = new WaterfallStep[]
            {
                AskForName,
                AskForLocation,
                FinishOnboardingDialog,
            };

            // To capture built-in waterfall dialog telemetry, set the telemetry client
            // to the new waterfall dialog and add it to the component dialog
            TelemetryClient = telemetryClient;
            AddDialog(new WaterfallDialog(InitialDialogId, onboarding) { TelemetryClient = telemetryClient });
            AddDialog(new TextPrompt(NamePrompt));
            AddDialog(new TextPrompt(LocationPrompt));
        }

        public async Task<DialogTurnResult> AskForName(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.PromptAsync(NamePrompt, new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(sc.Context, "en", OnboardingResponses.ResponseIds.NamePrompt),
            });
        }

        public async Task<DialogTurnResult> AskForLocation(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            _state = await _accessor.GetAsync(sc.Context, () => new OnboardingState());
            _state.Name = (string)sc.Result;

            return await sc.PromptAsync(LocationPrompt, new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(sc.Context, "en", OnboardingResponses.ResponseIds.LocationPrompt, new { _state.Name }),
            });
        }

        public async Task<DialogTurnResult> FinishOnboardingDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            _state = await _accessor.GetAsync(sc.Context, () => new OnboardingState());
            _state.Location = (string)sc.Result;

            await _responder.ReplyWith(sc.Context, OnboardingResponses.ResponseIds.HaveLocation, new { _state.Location });
            await _responder.ReplyWith(sc.Context, OnboardingResponses.ResponseIds.AddLinkedAccountsMessage);

            return await sc.EndDialogAsync();
        }
    }
}