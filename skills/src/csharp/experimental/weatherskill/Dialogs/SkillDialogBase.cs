using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Builder.Solutions.Telemetry;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using WeatherSkill.Models;
using WeatherSkill.Services;
using Luis;
using WeatherSkill.Responses.Shared;
using Microsoft.Bot.Builder.Skills;

namespace WeatherSkill.Dialogs
{
    public class SkillDialogBase : ComponentDialog
    {
        public SkillDialogBase(
             string dialogId,
             BotSettings settings,
             BotServices services,
             ResponseManager responseManager,
             ConversationState conversationState,
             IBotTelemetryClient telemetryClient)
             : base(dialogId)
        {
            Services = services;
            ResponseManager = responseManager;
            StateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));
            TelemetryClient = telemetryClient;

            // NOTE: Uncomment the following if your skill requires authentication
            // if (!Settings.OAuthConnections.Any())
            // {
            //     throw new Exception("You must configure an authentication connection before using this component.");
            // }
            //
            // AddDialog(new MultiProviderAuthDialog(services));
        }

        protected BotSettings Settings { get; set; }

        protected BotServices Services { get; set; }

        protected IStatePropertyAccessor<SkillState> StateAccessor { get; set; }

        protected ResponseManager ResponseManager { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            await GetLuisResult(dc);
            await DigestLuisResult(dc);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await GetLuisResult(dc);
            await DigestLuisResult(dc);
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        protected async Task<DialogTurnResult> GetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions());
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
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
                var providerTokenResponse = sc.Result as ProviderTokenResponse;

                if (providerTokenResponse != null)
                {
                    var state = await StateAccessor.GetAsync(sc.Context);
                    state.Token = providerTokenResponse.TokenResponse.Token;
                }

                return await sc.NextAsync();
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
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
                var state = await StateAccessor.GetAsync(dc.Context, () => new SkillState());

                // Get luis service for current locale
                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = Services.CognitiveModelSets[locale];
                var luisService = localeConfig.LuisServices["WeatherSkill"];

                // Get intent and entities for activity
                var result = await luisService.RecognizeAsync<WeatherSkillLuis>(dc.Context, CancellationToken.None);
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
            var state = await StateAccessor.GetAsync(sc.Context);
            state.Clear();
        }

        protected async Task DigestLuisResult(DialogContext dc)
        {
            try
            {
                var state = await StateAccessor.GetAsync(dc.Context, () => new SkillState());

                if (state.LuisResult != null)
                {
                    var entities = state.LuisResult.Entities;

                    if (entities.geography != null)
                    {
                        state.Geography = entities.geography[0];
                    }
                }
            }
            catch
            {
                // put log here
            }
        }


        private class DialogIds
        {
            public const string SkillModeAuth = "SkillAuth";
        }
    }
}