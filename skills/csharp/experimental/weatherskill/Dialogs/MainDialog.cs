// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Spatial;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using WeatherSkill.Models;
using WeatherSkill.Responses.Main;
using WeatherSkill.Responses.Shared;
using WeatherSkill.Services;
using SkillServiceLibrary.Utilities;
using WeatherSkill.Utilities;

namespace WeatherSkill.Dialogs
{
    public class MainDialog : RouterDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private LocaleTemplateEngineManager _localeTemplateEngineManager;
        private IStatePropertyAccessor<SkillState> _stateAccessor;
        private IStatePropertyAccessor<SkillContext> _contextAccessor;

        public MainDialog(
            BotSettings settings,
            BotServices services,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            UserState userState,
            ConversationState conversationState,
            IBotTelemetryClient telemetryClient,
            IHttpContextAccessor httpContext)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            _localeTemplateEngineManager = localeTemplateEngineManager;
            TelemetryClient = telemetryClient;

            // Initialize state accessor
            _stateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));
            _contextAccessor = userState.CreateProperty<SkillContext>(nameof(SkillContext));

            // Register dialogs
            AddDialog(new ForecastDialog(_settings, _services, _localeTemplateEngineManager, conversationState, TelemetryClient, httpContext));
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var locale = CultureInfo.CurrentUICulture;
            await dc.Context.SendActivityAsync(_localeTemplateEngineManager.GetResponse(MainResponses.WelcomeMessage));
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // get current activity locale
            var localeConfig = _services.GetCognitiveModels();

            // Populate state from SkillContext slots as required
            await PopulateStateFromSkillContext(dc.Context);

            // Get skill LUIS model from configuration
            localeConfig.LuisServices.TryGetValue("WeatherSkill", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var result = await luisService.RecognizeAsync<WeatherSkillLuis>(dc.Context, CancellationToken.None);
                var intent = result?.TopIntent().intent;

                switch (intent)
                {
                    case WeatherSkillLuis.Intent.CheckWeatherValue:
                        {
                            await dc.BeginDialogAsync(nameof(ForecastDialog));
                            break;
                        }

                    case WeatherSkillLuis.Intent.None:
                        {
                            // No intent was identified, send confused message
                            await dc.Context.SendActivityAsync(_localeTemplateEngineManager.GetResponse(SharedResponses.DidntUnderstandMessage));
                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            await dc.Context.SendActivityAsync(_localeTemplateEngineManager.GetResponse(MainResponses.FeatureNotAvailable));
                            break;
                        }
                }
            }
        }

        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // workaround. if connect skill directly to teams, the following response does not work.
            if (dc.Context.IsSkill() || Channel.GetChannelId(dc.Context) != Channels.Msteams)
            {
                var response = dc.Context.Activity.CreateReply();
                response.Type = ActivityTypes.EndOfConversation;

                await dc.Context.SendActivityAsync(response);
            }

            await dc.EndDialogAsync(result);
        }

        protected override async Task OnEventAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            switch (dc.Context.Activity.Name)
            {
                case Events.SkillBeginEvent:
                    {
                        var state = await _stateAccessor.GetAsync(dc.Context, () => new SkillState());

                        if (dc.Context.Activity.Value is Dictionary<string, object> userData)
                        {
                            // Capture user data from event if needed
                        }

                        break;
                    }

                case Events.TokenResponseEvent:
                    {
                        // Auth dialog completion
                        var result = await dc.ContinueDialogAsync();

                        // If the dialog completed when we sent the token, end the skill conversation
                        if (result.Status != DialogTurnStatus.Waiting)
                        {
                            var response = dc.Context.Activity.CreateReply();
                            response.Type = ActivityTypes.EndOfConversation;

                            await dc.Context.SendActivityAsync(response);
                        }

                        break;
                    }

                case Events.Location:
                    {
                        // Test trigger with
                        // /event:{ "Name": "Location", "Value": "34.05222222222222,-118.2427777777777" }
                        var value = dc.Context.Activity.Value.ToString();

                        if (!string.IsNullOrEmpty(value))
                        {
                            var coords = value.Split(',');
                            if (coords.Length == 2)
                            {
                                if (double.TryParse(coords[0], out var lat) && double.TryParse(coords[1], out var lng))
                                {
                                    var state = await _stateAccessor.GetAsync(dc.Context, () => new SkillState());
                                    state.Latitude = lat;
                                    state.Longitude = lng;
                                }
                            }
                        }

                        break;
                    }
            }
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = InterruptionAction.NoAction;

            if (dc.Context.Activity.Type == ActivityTypes.Message)
            {
                // get current activity locale
                var localeConfig = _services.GetCognitiveModels();

                // check general luis intent
                localeConfig.LuisServices.TryGetValue("General", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The specified LUIS Model could not be found in your Skill configuration.");
                }
                else
                {
                    var luisResult = await luisService.RecognizeAsync<GeneralLuis>(dc.Context, cancellationToken);
                    var topIntent = luisResult.TopIntent();

                    if (topIntent.score > 0.5)
                    {
                        switch (topIntent.intent)
                        {
                            case GeneralLuis.Intent.Cancel:
                                {
                                    result = await OnCancel(dc);
                                    break;
                                }

                            case GeneralLuis.Intent.Help:
                                {
                                    result = await OnHelp(dc);
                                    break;
                                }

                            case GeneralLuis.Intent.Logout:
                                {
                                    result = await OnLogout(dc);
                                    break;
                                }
                        }
                    }
                }
            }

            return result;
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_localeTemplateEngineManager.GetResponse(MainResponses.CancelMessage));
            await CompleteAsync(dc);
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.End;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_localeTemplateEngineManager.GetResponse(MainResponses.HelpMessage));
            return InterruptionAction.Resume;
        }

        private async Task<InterruptionAction> OnLogout(DialogContext dc)
        {
            BotFrameworkAdapter adapter;
            var supported = dc.Context.Adapter is BotFrameworkAdapter;
            if (!supported)
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
            else
            {
                adapter = (BotFrameworkAdapter)dc.Context.Adapter;
            }

            await dc.CancelAllDialogsAsync();

            // Sign out user
            var tokens = await adapter.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);
            foreach (var token in tokens)
            {
                await adapter.SignOutUserAsync(dc.Context, token.ConnectionName);
            }

            await dc.Context.SendActivityAsync(_localeTemplateEngineManager.GetResponse(MainResponses.LogOut));

            return InterruptionAction.End;
        }

        private async Task PopulateStateFromSkillContext(ITurnContext context)
        {
            // Populating local state with data passed through semanticAction out of Activity
            var activity = context.Activity;
            var semanticAction = activity.SemanticAction;
            if (semanticAction != null && semanticAction.Entities.ContainsKey("location"))
            {
                var location = semanticAction.Entities["location"];
                var locationObj = location.Properties["location"].ToString();

                var coords = locationObj.Split(',');
                if (coords.Length == 2 && double.TryParse(coords[0], out var lat) && double.TryParse(coords[1], out var lng))
                {
                    var state = await _stateAccessor.GetAsync(context, () => new SkillState());
                    state.Latitude = lat;
                    state.Longitude = lng;
                }
                else
                {
                    // In case name has ','
                    var state = await _stateAccessor.GetAsync(context, () => new SkillState());
                    state.Geography = locationObj;
                }
            }
        }

        private class Events
        {
            public const string TokenResponseEvent = "tokens/response";
            public const string SkillBeginEvent = "skillBegin";
            public const string Location = "Location";
        }
    }
}