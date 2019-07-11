// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using NewsSkill.Models;
using NewsSkill.Responses.Main;
using NewsSkill.Services;

namespace NewsSkill.Dialogs
{
    public class NewsDialogBase : ComponentDialog
    {
        protected const string LuisResultKey = "LuisResult";
        private MainResponses _responder = new MainResponses();
        private AzureMapsService _mapsService;

        public NewsDialogBase(
            string dialogId,
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            UserState userState,
            AzureMapsService mapsService,
            IBotTelemetryClient telemetryClient)
            : base(dialogId)
        {
            Services = services;
            ConvAccessor = conversationState.CreateProperty<NewsSkillState>(nameof(NewsSkillState));
            UserAccessor = userState.CreateProperty<NewsSkillUserState>(nameof(NewsSkillUserState));
            TelemetryClient = telemetryClient;

            var mapsKey = settings.AzureMapsKey ?? throw new Exception("The AzureMapsKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");
            _mapsService = mapsService;
            _mapsService.InitKeyAsync(mapsKey);
        }

        protected BotServices Services { get; set; }

        protected IStatePropertyAccessor<NewsSkillState> ConvAccessor { get; set; }

        protected IStatePropertyAccessor<NewsSkillUserState> UserAccessor { get; set; }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        public async Task<Exception> HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(MainStrings.ERROR));

            await sc.CancelAllDialogsAsync();

            return ex;
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ConvAccessor.GetAsync(dc.Context);

            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ConvAccessor.GetAsync(dc.Context);

            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        protected async Task<DialogTurnResult> GetMarket(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserAccessor.GetAsync(sc.Context, () => new NewsSkillUserState());

            // Check if there's already a location
            if (!string.IsNullOrWhiteSpace(userState.Market))
            {
                return await sc.NextAsync(userState.Market);
            }

            // Prompt user for location
            return await sc.PromptAsync(nameof(TextPrompt), new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(sc.Context, sc.Context.Activity.Locale, MainResponses.MarketPrompt),
                RetryPrompt = await _responder.RenderTemplate(sc.Context, sc.Context.Activity.Locale, MainResponses.MarketRetryPrompt)
            });
        }

        protected async Task<DialogTurnResult> SetMarket(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserAccessor.GetAsync(sc.Context, () => new NewsSkillUserState());

            if (string.IsNullOrWhiteSpace(userState.Market))
            {
                string country = (string)sc.Result;

                // use AzureMaps API to get country code from country input by user
                userState.Market = await _mapsService.GetCountryCodeAsync(country);
            }

            return await sc.NextAsync();
        }

        protected async Task<bool> MarketPromptValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var country = promptContext.Recognized.Value;

            // check for valid country code
            country = await _mapsService.GetCountryCodeAsync(country);
            if (country != null)
            {
                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }
    }
}