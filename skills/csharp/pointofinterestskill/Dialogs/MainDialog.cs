// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.Responses.Main;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Services;
using SkillServiceLibrary.Utilities;

namespace PointOfInterestSkill.Dialogs
{
    // Dialog providing activity routing and message/event processing.
    public class MainDialog : ComponentDialog
    {
        private BotServices _services;
        private ResponseManager _responseManager;
        private IStatePropertyAccessor<PointOfInterestSkillState> _stateAccessor;
        private Dialog _routeDialog;
        private Dialog _cancelRouteDialog;
        private Dialog _findPointOfInterestDialog;
        private Dialog _findParkingDialog;
        private Dialog _getDirectionsDialog;

        public MainDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog))
        {
            _services = serviceProvider.GetService<BotServices>();
            _responseManager = serviceProvider.GetService<ResponseManager>();
            TelemetryClient = telemetryClient;

            // Initialize state accessor
            var conversationState = serviceProvider.GetService<ConversationState>();
            _stateAccessor = conversationState.CreateProperty<PointOfInterestSkillState>(nameof(PointOfInterestSkillState));

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
            _routeDialog = serviceProvider.GetService<RouteDialog>() ?? throw new ArgumentNullException(nameof(RouteDialog));
            _cancelRouteDialog = serviceProvider.GetService<CancelRouteDialog>() ?? throw new ArgumentNullException(nameof(CancelRouteDialog));
            _findPointOfInterestDialog = serviceProvider.GetService<FindPointOfInterestDialog>() ?? throw new ArgumentNullException(nameof(FindPointOfInterestDialog));
            _findParkingDialog = serviceProvider.GetService<FindParkingDialog>() ?? throw new ArgumentNullException(nameof(FindParkingDialog));
            _getDirectionsDialog = serviceProvider.GetService<GetDirectionsDialog>() ?? throw new ArgumentNullException(nameof(GetDirectionsDialog));
            AddDialog(_routeDialog);
            AddDialog(_cancelRouteDialog);
            AddDialog(_findPointOfInterestDialog);
            AddDialog(_findParkingDialog);
            AddDialog(_getDirectionsDialog);
        }

        // Runs when the dialog is started.
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("PointOfInterest", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<PointOfInterestLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.POILuisResultKey, skillResult);
                }
                else
                {
                    throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResultKey, generalResult);
                }
                else
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }

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
                localizedServices.LuisServices.TryGetValue("PointOfInterest", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<PointOfInterestLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.POILuisResultKey, skillResult);
                }
                else
                {
                    throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResultKey, generalResult);
                }
                else
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }

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
                var generalResult = innerDc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
                    switch (generalIntent)
                    {
                        case General.Intent.Cancel:
                            {
                                await innerDc.Context.SendActivityAsync(_responseManager.GetResponse(POISharedResponses.CancellingMessage));
                                await innerDc.CancelAllDialogsAsync();
                                await innerDc.BeginDialogAsync(InitialDialogId);
                                interrupted = true;
                                break;
                            }

                        case General.Intent.Help:
                            {
                                await innerDc.Context.SendActivityAsync(_responseManager.GetResponse(POIMainResponses.HelpMessage));
                                await innerDc.RepromptDialogAsync();
                                interrupted = true;
                                break;
                            }

                        case General.Intent.Logout:
                            {
                                // Log user out of all accounts.
                                await LogUserOut(innerDc);

                                await innerDc.Context.SendActivityAsync(_responseManager.GetResponse(POIMainResponses.LogOut));
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
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new PointOfInterestSkillState());

            if (stepContext.Context.IsSkill() || state.ShouldInterrupt)
            {
                // If the bot is in skill mode, skip directly to route and do not prompt
                return await stepContext.NextAsync();
            }
            else
            {
                // If bot is in local mode, prompt with intro or continuation message
                var promptOptions = new PromptOptions
                {
                    Prompt = stepContext.Options as Activity ?? _responseManager.GetResponse(POIMainResponses.PointOfInterestWelcomeMessage)
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
                var result = stepContext.Context.TurnState.Get<PointOfInterestLuis>(StateProperties.POILuisResultKey);
                var intent = result?.TopIntent().intent;

                if (intent != PointOfInterestLuis.Intent.None)
                {
                    var state = await _stateAccessor.GetAsync(stepContext.Context, () => new PointOfInterestSkillState());
                    await DigestLuisResult(stepContext, result);
                }

                // switch on General intents
                switch (intent)
                {
                    case PointOfInterestLuis.Intent.GetDirections:
                        {
                            return await stepContext.BeginDialogAsync(nameof(GetDirectionsDialog));
                        }

                    case PointOfInterestLuis.Intent.FindPointOfInterest:
                        {
                            return await stepContext.BeginDialogAsync(nameof(FindPointOfInterestDialog));
                        }

                    case PointOfInterestLuis.Intent.FindParking:
                        {
                            return await stepContext.BeginDialogAsync(nameof(FindParkingDialog));
                        }

                    case PointOfInterestLuis.Intent.None:
                        {
                            await stepContext.Context.SendActivityAsync(_responseManager.GetResponse(POISharedResponses.DidntUnderstandMessage));
                            break;
                        }

                    default:
                        {
                            await stepContext.Context.SendActivityAsync(_responseManager.GetResponse(POIMainResponses.FeatureNotAvailable));
                            break;
                        }
                }
            }
            else if (activity.Type == ActivityTypes.Event)
            {
                // Handle skill action logic here
                var ev = activity.AsEventActivity();

                if (!string.IsNullOrEmpty(ev.Name))
                {
                    switch (ev.Name)
                    {
                        case "GetDirectionAction":
                            {
                                await DigestActoinInput<GetDirectionInput>(stepContext, ev);
                                return await stepContext.BeginDialogAsync(nameof(GetDirectionsDialog));
                            }

                        case "FindPointOfInterestAction":
                            {
                                await DigestActoinInput<FindPointOfInterestInput>(stepContext, ev);
                                return await stepContext.BeginDialogAsync(nameof(FindPointOfInterestDialog));
                            }

                        case "FindParkingAction":
                            {
                                await DigestActoinInput<FindParkingInput>(stepContext, ev);
                                return await stepContext.BeginDialogAsync(nameof(FindParkingDialog));
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
                return await stepContext.ReplaceDialogAsync(this.Id, _responseManager.GetResponse(POIMainResponses.PointOfInterestWelcomeMessage), cancellationToken);
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

        private async Task DigestLuisResult(DialogContext dc, PointOfInterestLuis luisResult)
        {
            try
            {
                var state = await _stateAccessor.GetAsync(dc.Context, () => new PointOfInterestSkillState());

                if (luisResult != null)
                {
                    state.Clear();

                    var entities = luisResult.Entities;

                    // TODO since we can only search one per search, only the 1st one is considered
                    if (entities.Keyword != null)
                    {
                        if (entities._instance.KeywordCategory == null || !entities._instance.KeywordCategory.Any(c => c.Text.Equals(entities.Keyword[0], StringComparison.InvariantCultureIgnoreCase)))
                        {
                            state.Keyword = entities.Keyword[0];
                        }
                    }

                    // TODO if keyword exists and category exists, whether keyword contains category or a keyword of some category. We will ignore category in these two cases
                    if (string.IsNullOrEmpty(state.Keyword) && entities._instance.KeywordCategory != null)
                    {
                        state.Category = entities._instance.KeywordCategory[0].Text;
                    }

                    if (entities.Address != null)
                    {
                        state.Address = string.Join(" ", entities.Address);
                    }
                    else
                    {
                        // ADDRESS overwrites geographyV2
                        var sb = new StringBuilder();

                        if (entities.geographyV2 != null)
                        {
                            sb.AppendJoin(" ", entities.geographyV2.Select(geography => geography.Location));
                        }

                        if (sb.Length > 0)
                        {
                            state.Address = sb.ToString();
                        }
                    }

                    // TODO only first is used now
                    if (entities.RouteDescription != null)
                    {
                        state.RouteType = entities.RouteDescription[0][0];
                    }

                    if (entities.PoiDescription != null)
                    {
                        state.PoiType = entities.PoiDescription[0][0];
                    }

                    // TODO unused
                    if (entities.number != null)
                    {
                        try
                        {
                            var value = entities.number[0];
                            if (Math.Abs(value - (int)value) < double.Epsilon)
                            {
                                state.UserSelectIndex = (int)value - 1;
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
            }
            catch
            {
                // put log here
            }
        }

        private async Task DigestActoinInput<T>(DialogContext dc, IEventActivity ev)
            where T : ActionBaseInput
        {
            if (ev.Value is JObject eventValue)
            {
                var state = await _stateAccessor.GetAsync(dc.Context, () => new PointOfInterestSkillState());

                T actionData = eventValue.ToObject<T>();
                actionData.DigestActionInput(state);
            }
        }
    }
}
