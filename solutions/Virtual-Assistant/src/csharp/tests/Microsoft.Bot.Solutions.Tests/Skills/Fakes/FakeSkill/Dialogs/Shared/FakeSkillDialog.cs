using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Prompts;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Newtonsoft.Json.Linq;
using FakeSkill.Dialogs.Shared.DialogOptions;
using FakeSkill.Dialogs.Shared.Resources;
using FakeSkill.ServiceClients;

namespace FakeSkill.Dialogs.Shared
{
    public class SkillTemplateDialog : ComponentDialog
    {
        public SkillTemplateDialog(
            string dialogId,
            SkillConfigurationBase services,
            IStatePropertyAccessor<SkillConversationState> conversationStateAccessor,
            IStatePropertyAccessor<SkillUserState> userStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(dialogId)
        {
            Services = services;
            ConversationStateAccessor = conversationStateAccessor;
            UserStateAccessor = userStateAccessor;
            ServiceManager = serviceManager;
            TelemetryClient = telemetryClient;

            // NOTE: Uncomment the following if your skill requires authentication
            //if (!Services.AuthenticationConnections.Any())
            //{
            //    throw new Exception("You must configure an authentication connection in your bot file before using this component.");
            //}

            //AddDialog(new EventPrompt(DialogIds.SkillModeAuth, "tokens/response", TokenResponseValidator));
            //AddDialog(new MultiProviderAuthDialog(services));
        }

        protected SkillConfigurationBase Services { get; set; }

        protected IStatePropertyAccessor<SkillConversationState> ConversationStateAccessor { get; set; }

        protected IStatePropertyAccessor<SkillUserState> UserStateAccessor { get; set; }

        protected IServiceManager ServiceManager { get; set; }

        protected SkillTemplateResponseBuilder ResponseBuilder { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DigestLuisResult(dc);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DigestLuisResult(dc);
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
                    // TODO Error handling - if we get a new activity that isn't an event
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
                    return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions() { RetryPrompt = sc.Context.Activity.CreateReply(SharedResponses.NoAuth, ResponseBuilder) });
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
        protected Task DigestLuisResult(DialogContext dc)
        {
            return Task.CompletedTask;
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
            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SharedResponses.ErrorMessage));

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