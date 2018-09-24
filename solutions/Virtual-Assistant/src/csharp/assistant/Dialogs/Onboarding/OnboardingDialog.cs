// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace VirtualAssistant
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

        public OnboardingDialog(BotServices botServices, IStatePropertyAccessor<OnboardingState> accessor)
            : base(botServices, nameof(OnboardingDialog))
        {
            _accessor = accessor;
            InitialDialogId = nameof(OnboardingDialog);

            var onboarding = new WaterfallStep[]
            {
                AskForName,
                AskForLocation,
                FinishOnboardingDialog,
            };

            AddDialog(new WaterfallDialog(InitialDialogId, onboarding));
            AddDialog(new TextPrompt(NamePrompt));
            AddDialog(new TextPrompt(LocationPrompt));
        }

        public async Task<DialogTurnResult> AskForName(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.PromptAsync(NamePrompt, new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(sc.Context, "en", OnboardingResponses._namePrompt),
            });
        }

        public async Task<DialogTurnResult> AskForLocation(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            _state = await _accessor.GetAsync(sc.Context);
            _state.Name = (string)sc.Result;

            return await sc.PromptAsync(LocationPrompt, new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(sc.Context, "en", OnboardingResponses._locationPrompt, new { _state.Name }),
            });
        }

        public async Task<DialogTurnResult> FinishOnboardingDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            _state = await _accessor.GetAsync(sc.Context, () => new OnboardingState());
            _state.Location = (string)sc.Result;

            await _responder.ReplyWith(sc.Context, OnboardingResponses._haveLocation, new { _state.Location });
            await _responder.ReplyWith(sc.Context, OnboardingResponses._linkedAccountsInfo);

            return await sc.EndDialogAsync();
        }
    }
}
