// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SkillSample.Extensions;
using SkillSample.Models;
using SkillSample.Services;

namespace SkillSample.Dialogs
{
    // Dialog providing activity routing and message/event processing.
    public class MainDialog : ComponentDialog
    {
        private BotServices _services;
        private SampleDialog _sampleDialog;
        private SampleAction _sampleAction;
        private LocaleTemplateEngineManager _templateEngine;
        private IStatePropertyAccessor<SkillState> _stateAccessor;

        public MainDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog))
        {
            _services = serviceProvider.GetService<BotServices>();
            _templateEngine = serviceProvider.GetService<LocaleTemplateEngineManager>();
            TelemetryClient = telemetryClient;

            // Create conversation state properties
            var conversationState = serviceProvider.GetService<ConversationState>();
            _stateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));

            var steps = new WaterfallStep[]
            {
                IntroStepAsync,
                RouteStepAsync,
                FinalStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(MainDialog), steps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            InitialDialogId = nameof(MainDialog);

            // Register dialogs
            _sampleDialog = serviceProvider.GetService<SampleDialog>();
            _sampleAction = serviceProvider.GetService<SampleAction>();
            AddDialog(_sampleDialog);
            AddDialog(_sampleAction);
        }

        // Runs when the dialog is started.
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                var skillResult = await localizedServices.LuisServices["SkillSample"].RecognizeAsync<SkillSampleLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.SkillLuisResult, skillResult);

                // Run LUIS recognition on General model and store result in turn state.
                var generalResult = await localizedServices.LuisServices["General"].RecognizeAsync<GeneralLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResult, generalResult);

                // Check for any interruptions
                var interrupted = await InterruptDialogAsync(innerDc, cancellationToken);

                if (interrupted)
                {
                    // If dialog was interrupted, return EndOfTurn
                    return EndOfTurn;
                }
            }

            return await base.OnBeginDialogAsync(innerDc, options, cancellationToken);
        }

        // Runs on every turn of the conversation.
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                var skillResult = await localizedServices.LuisServices["SkillSample"].RecognizeAsync<SkillSampleLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.SkillLuisResult, skillResult);

                // Run LUIS recognition on General model and store result in turn state.
                var generalResult = await localizedServices.LuisServices["General"].RecognizeAsync<GeneralLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResult, generalResult);

                // Check for any interruptions
                var interrupted = await InterruptDialogAsync(innerDc, cancellationToken);

                if (interrupted)
                {
                    // If dialog was interrupted, return EndOfTurn
                    return EndOfTurn;
                }
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        // Runs on every turn of the conversation to check if the conversation should be interrupted.
        protected async Task<bool> InterruptDialogAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            var interrupted = false;
            var activity = innerDc.Context.Activity;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // Get connected LUIS result from turn state.
                var generalResult = innerDc.Context.TurnState.Get<GeneralLuis>(StateProperties.GeneralLuisResult);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
                    switch (generalIntent)
                    {
                        case GeneralLuis.Intent.Cancel:
                            {
                                await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("CancelledMessage"));
                                await innerDc.CancelAllDialogsAsync();
                                await innerDc.BeginDialogAsync(InitialDialogId);
                                interrupted = true;
                                break;
                            }

                        case GeneralLuis.Intent.Help:
                            {
                                await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("HelpCard"));
                                await innerDc.RepromptDialogAsync();
                                interrupted = true;
                                break;
                            }

                        case GeneralLuis.Intent.Logout:
                            {
                                // Log user out of all accounts.
                                await LogUserOut(innerDc);

                                await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("LogoutMessage"));
                                await innerDc.CancelAllDialogsAsync();
                                await innerDc.BeginDialogAsync(InitialDialogId);
                                interrupted = true;
                                break;
                            }
                    }
                }
            }

            return interrupted;
        }

        // Handles introduction/continuation prompt logic.
        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.IsSkill())
            {
                // If the bot is in skill mode, skip directly to route and do not prompt
                return await stepContext.NextAsync();
            }
            else
            {
                // If bot is in local mode, prompt with intro or continuation message
                var promptOptions = new PromptOptions
                {
                    Prompt = stepContext.Options as Activity ?? _templateEngine.GenerateActivityForLocale("FirstPromptMessage")
                };

                return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
            }
        }

        // Handles routing to additional dialogs logic.
        private async Task<DialogTurnResult> RouteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var activity = stepContext.Context.Activity;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // Get current cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Get skill LUIS model from configuration.
                localizedServices.LuisServices.TryGetValue("SkillSample", out var luisService);

                if (luisService != null)
                {
                    var result = stepContext.Context.TurnState.Get<SkillSampleLuis>(StateProperties.SkillLuisResult);
                    var intent = result?.TopIntent().intent;

                    switch (intent)
                    {
                        case SkillSampleLuis.Intent.Sample:
                            {
                                return await stepContext.BeginDialogAsync(_sampleDialog.Id);
                            }

                        case SkillSampleLuis.Intent.None:
                        default:
                            {
                                // intent was identified but not yet implemented
                                await stepContext.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("UnsupportedMessage"));
                                return await stepContext.NextAsync();
                            }
                    }
                }
                else
                {
                    throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
                }
            }
            else if (activity.Type == ActivityTypes.Event)
            {
                var ev = activity.AsEventActivity();

                if (!string.IsNullOrEmpty(ev.Name))
                {
                    switch (ev.Name)
                    {
                        case "SampleAction":
                            {
                                SampleActionInput actionData = null;

                                if (ev.Value is JObject eventValue)
                                {
                                    actionData = eventValue.ToObject<SampleActionInput>();
                                }

                                // Invoke the SampleAction dialog passing input data if available
                                return await stepContext.BeginDialogAsync(nameof(SampleAction), actionData);
                            }

                        default:
                            {
                                await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{ev.Name ?? "undefined"}' was received but not processed."));
                                break;
                            }
                    }
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"An event with no name was received but not processed."));
                }
            }

            // If activity was unhandled, flow should continue to next step
            return await stepContext.NextAsync();
        }

        // Handles conversation cleanup.
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.IsSkill())
            {
                // EndOfConversation activity should be passed back to indicate that VA should resume control of the conversation
                var endOfConversation = new Activity(ActivityTypes.EndOfConversation)
                {
                    Code = EndOfConversationCodes.CompletedSuccessfully,
                    Value = stepContext.Result,
                };

                await stepContext.Context.SendActivityAsync(endOfConversation, cancellationToken);
                return await stepContext.EndDialogAsync();
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(this.Id, _templateEngine.GenerateActivityForLocale("CompletedMessage"), cancellationToken);
            }
        }

        private async Task LogUserOut(DialogContext dc)
        {
            IUserTokenProvider tokenProvider;
            var supported = dc.Context.Adapter is IUserTokenProvider;
            if (supported)
            {
                tokenProvider = (IUserTokenProvider)dc.Context.Adapter;

                // Sign out user
                var tokens = await tokenProvider.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);
                foreach (var token in tokens)
                {
                    await tokenProvider.SignOutUserAsync(dc.Context, token.ConnectionName);
                }

                // Cancel all active dialogs
                await dc.CancelAllDialogsAsync();
            }
            else
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
        }
    }
}