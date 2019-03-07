﻿using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Telemetry;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Newtonsoft.Json.Linq;
using $safeprojectname$.Dialogs.Shared.DialogOptions;
using $safeprojectname$.Dialogs.Shared.Resources;
using $safeprojectname$.ServiceClients;

namespace $safeprojectname$.Dialogs.Shared
{
    public class SkillDialogBase : ComponentDialog
    {
        public SkillDialogBase(
            string dialogId,
            SkillConfigurationBase services,
            ResponseManager responseManager,
            IStatePropertyAccessor<SkillConversationState> conversationStateAccessor,
            IStatePropertyAccessor<SkillUserState> userStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(dialogId)
        {
            Services = services;
            ResponseManager = responseManager;
            ConversationStateAccessor = conversationStateAccessor;
            UserStateAccessor = userStateAccessor;
            ServiceManager = serviceManager;
            TelemetryClient = telemetryClient;

            // NOTE: Uncomment the following if your skill requires authentication
            // if (!Services.AuthenticationConnections.Any())
            // {
            //     throw new Exception("You must configure an authentication connection in your bot file before using this component.");
            // }
            //
            // AddDialog(new EventPrompt(DialogIds.SkillModeAuth, "tokens/response", TokenResponseValidator));
            // AddDialog(new MultiProviderAuthDialog(services));
        }

        protected SkillConfigurationBase Services { get; set; }

        protected IStatePropertyAccessor<SkillConversationState> ConversationStateAccessor { get; set; }

        protected IStatePropertyAccessor<SkillUserState> UserStateAccessor { get; set; }

        protected IServiceManager ServiceManager { get; set; }

        protected ResponseManager ResponseManager { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            await GetLuisResult(dc);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await GetLuisResult(dc);
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        // Shared steps
        protected async Task<DialogTurnResult> GetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (SkillTemplateDialogOptions)sc.Options;

                // If in Skill mode we ask the calling Bot for the token
                if (skillOptions != null && skillOptions.SkillMode)
                {
                    // We trigger a Token Request from the Parent Bot by sending a "TokenRequest" event back and then waiting for a "TokenResponse"
                    var response = sc.Context.Activity.CreateReply();
                    response.Type = ActivityTypes.Event;
                    response.Name = "tokens/request";

                    // Send the tokens/request Event
                    await sc.Context.SendActivityAsync(response);

                    // Wait for the tokens/response event
                    return await sc.PromptAsync(DialogIds.SkillModeAuth, new PromptOptions());
                }
                else
                {
                    return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions() { RetryPrompt = ResponseManager.GetResponse(SharedResponses.NoAuth) });
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterGetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // When the user authenticates interactively we pass on the tokens/Response event which surfaces as a JObject
                // When the token is cached we get a TokenResponse object.
                var skillOptions = (SkillTemplateDialogOptions)sc.Options;
                ProviderTokenResponse providerTokenResponse;
                if (skillOptions != null && skillOptions.SkillMode)
                {
                    var resultType = sc.Context.Activity.Value.GetType();
                    if (resultType == typeof(ProviderTokenResponse))
                    {
                        providerTokenResponse = sc.Context.Activity.Value as ProviderTokenResponse;
                    }
                    else
                    {
                        var tokenResponseObject = sc.Context.Activity.Value as JObject;
                        providerTokenResponse = tokenResponseObject?.ToObject<ProviderTokenResponse>();
                    }
                }
                else
                {
                    providerTokenResponse = sc.Result as ProviderTokenResponse;
                }

                if (providerTokenResponse != null)
                {
                    var state = await ConversationStateAccessor.GetAsync(sc.Context);
                    state.Token = providerTokenResponse.TokenResponse.Token;
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // Validators
        protected Task<bool> TokenResponseValidator(PromptValidatorContext<Activity> pc, CancellationToken cancellationToken)
        {
            var activity = pc.Recognized.Value;
            if (activity != null && activity.Type == ActivityTypes.Event)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        protected Task<bool> AuthPromptValidator(PromptValidatorContext<TokenResponse> promptContext, CancellationToken cancellationToken)
        {
            var token = promptContext.Recognized.Value;
            if (token != null)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        // Helpers
        protected async Task GetLuisResult(DialogContext dc)
        {
            if (dc.Context.Activity.Type == ActivityTypes.Message)
            {
                var state = await ConversationStateAccessor.GetAsync(dc.Context);

                // Get luis service for current locale
                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = Services.LocaleConfigurations[locale];
                var luisService = localeConfig.LuisServices["$safeprojectname$"];

                // Get intent and entities for activity
                var result = await luisService.RecognizeAsync<$safeprojectname$LU>(dc.Context, CancellationToken.None);
                state.LuisResult = result;
            }
        }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        protected async Task HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace);

            // log exception
            TelemetryClient.TrackExceptionEx(ex, sc.Context.Activity, sc.ActiveDialog?.Id);

            // send error message to bot user
            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.ErrorMessage));

            // clear state
            var state = await ConversationStateAccessor.GetAsync(sc.Context);
            state.Clear();
        }

        private class DialogIds
        {
            public const string SkillModeAuth = "SkillAuth";
        }
    }
}