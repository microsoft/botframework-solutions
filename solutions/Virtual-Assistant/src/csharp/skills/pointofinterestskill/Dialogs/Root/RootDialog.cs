// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Middleware;
using Microsoft.Bot.Solutions.Skills;
using PointOfInterestSkill.Dialogs.Shared.Resources;

namespace PointOfInterestSkill
{
    public class RootDialog : RouterDialog
    {
        private const string CancelCode = "cancel";
        private bool _skillMode;
        private PointOfInterestSkillAccessors _accessors;
        private PointOfInterestSkillResponses _responder;
        private PointOfInterestSkillServices _services;
        private IServiceManager _serviceManager;

        public RootDialog(bool skillMode, PointOfInterestSkillServices services, PointOfInterestSkillAccessors pointOfInterestSkillAccessors, IServiceManager serviceManager)
            : base(nameof(RootDialog))
        {
            _skillMode = skillMode;
            _accessors = pointOfInterestSkillAccessors;
            _serviceManager = serviceManager;
            _responder = new PointOfInterestSkillResponses();
            _services = services;

            // Initialize dialogs
            RegisterDialogs();
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Get the conversation state from the turn context
            var state = await _accessors.PointOfInterestSkillState.GetAsync(dc.Context, () => new PointOfInterestSkillState());
            var dialogState = await _accessors.ConversationDialogState.GetAsync(dc.Context, () => new DialogState());

            PointOfInterest luisResult = null;

            if (_services?.LuisRecognizer != null)
            {
                // When run in normal mode or 2+ turn of a skill we use LUIS ourselves as the parent Dispatcher doesn't do this
                luisResult = await _services.LuisRecognizer.RecognizeAsync<PointOfInterest>(dc.Context, cancellationToken);
            }
            else if (_skillMode && state.LuisResultPassedFromSkill != null)
            {
                // If invoked by a Skill we get the Luis IRecognizerConvert passed to us on first turn so we don't have to do that locally
                luisResult = (PointOfInterest)state.LuisResultPassedFromSkill;
            }
            else
            {
                throw new Exception("PointOfInterestSkill: Could not get Luis Recognizer result.");
            }

            await DigestPointOfInterestLuisResult(dc, luisResult);

            var intent = luisResult?.TopIntent().intent;

            var skillOptions = new PointOfInterestSkillDialogOptions
            {
                SkillMode = _skillMode,
            };

            var result = EndOfTurn;

            switch (intent)
            {
                case PointOfInterest.Intent.NAVIGATION_ROUTE_FROM_X_TO_Y:
                    {
                        result = await dc.BeginDialogAsync(nameof(RouteDialog), skillOptions);

                        break;
                    }

                case PointOfInterest.Intent.NAVIGATION_CANCEL_ROUTE:
                    {
                        result = await dc.BeginDialogAsync(nameof(CancelRouteDialog), skillOptions);
                        break;
                    }

                case PointOfInterest.Intent.NAVIGATION_FIND_POINTOFINTEREST:
                    {
                        result = await dc.BeginDialogAsync(nameof(FindPointOfInterestDialog), skillOptions);
                        break;
                    }

                case PointOfInterest.Intent.None:
                case null:
                default:
                    {
                        result = new DialogTurnResult(DialogTurnStatus.Complete);
                        await _responder.ReplyWith(dc.Context, PointOfInterestSkillResponses.Confused);
                        break;
                    }
            }

            if (result.Status == DialogTurnStatus.Complete)
            {
                await CompleteAsync(dc);
            }
        }

        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = dc.Context.Activity.CreateReply();
            response.Type = ActivityTypes.EndOfConversation;

            await dc.Context.SendActivityAsync(response);

            // End active dialog
            await dc.EndDialogAsync(result);
        }

        protected override async Task OnEventAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _accessors.PointOfInterestSkillState.GetAsync(dc.Context, () => new PointOfInterestSkillState());
            var skillMetadata = dc.Context.Activity.Value as SkillMetadata;

            switch (dc.Context.Activity.Name)
            {
                case "skillBegin":
                    {
                        if (skillMetadata != null)
                        {
                            var luisService = skillMetadata.LuisService;
                            var luisApp = new LuisApplication(luisService.AppId, luisService.SubscriptionKey, luisService.GetEndpoint());
                            _services.LuisRecognizer = new LuisRecognizer(luisApp);

                            state.LuisResultPassedFromSkill = skillMetadata.LuisResult;
                        }

                        state.LuisResultPassedFromSkill = skillMetadata.LuisResult;

                        // Each skill is configured to explictly request certain items to be passed across
                        if (skillMetadata.Parameters.TryGetValue("IPA.Location", out var location))
                        {
                            var coords = ((string)location).Split(',');
                            if (coords.Length == 2)
                            {
                                if (double.TryParse(coords[0], out var lat) && double.TryParse(coords[1], out var lng))
                                {
                                    var coordinates = new LatLng
                                    {
                                        Latitude = lat,
                                        Longitude = lng,
                                    };
                                    state.CurrentCoordinates = coordinates;
                                }
                            }
                        }
                        else
                        {
                            // TODO Error handling if parameter isn't passed (or default)
                        }

                        break;
                    }

                case "IPA.Location":
                    {
                        // Test trigger with
                        // /event:{ "Name": "IPA.Location", "Value": "34.05222222222222,-118.2427777777777" }
                        var value = dc.Context.Activity.Value.ToString();

                        if (!string.IsNullOrEmpty(value))
                        {
                            var coords = value.Split(',');
                            if (coords.Length == 2)
                            {
                                if (double.TryParse(coords[0], out var lat) && double.TryParse(coords[1], out var lng))
                                {
                                    var coordinates = new LatLng
                                    {
                                        Latitude = lat,
                                        Longitude = lng
                                    };
                                    state.CurrentCoordinates = coordinates;
                                }
                            }
                        }

                        break;
                    }

                case "POI.ActiveLocation":
                    {
                        // Test trigger with...
                        var activeLocationName = dc.Context.Activity.Value.ToString();

                        // Set ActiveLocation if one w/ matching name is found in FoundLocations
                        var activeLocation = state.FoundLocations?.FirstOrDefault(x => x.Name.Contains(activeLocationName, StringComparison.InvariantCultureIgnoreCase));
                        if (activeLocation != null)
                        {
                            state.ActiveLocation = activeLocation;
                        }

                        // Activity should have text to trigger next intent, update Type & Route again
                        if (!string.IsNullOrEmpty(dc.Context.Activity.Text))
                        {
                            dc.Context.Activity.Type = ActivityTypes.Message;
                            await RouteAsync(dc);
                        }

                        break;
                    }

                case "POI.ActiveRoute":
                    {
                        int.TryParse(dc.Context.Activity.Value.ToString(), out var routeId);
                        var activeRoute = state.FoundRoutes[routeId];
                        if (activeRoute != null)
                        {
                            state.ActiveRoute = activeRoute;
                        }

                        var replyMessage = dc.Context.Activity.CreateReply(PointOfInterestBotResponses.SendingRouteDetails);
                        await dc.Context.SendActivityAsync(replyMessage);

                        // Send event with active route data
                        var replyEvent = dc.Context.Activity.CreateReply();
                        replyEvent.Type = ActivityTypes.Event;
                        replyEvent.Name = "ActiveRoute.Directions";
                        replyEvent.Value = state.ActiveRoute.Legs;
                        await dc.Context.SendActivityAsync(replyEvent);
                        break;
                    }
            }
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = dc.Context.Activity;
            var reply = activity.CreateReply(PointOfInterestBotResponses.PointOfInterestWelcomeMessage);
            await dc.Context.SendActivityAsync(reply);
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dc.Context.Activity.Text?.ToLower() == CancelCode)
            {
                await CompleteAsync(dc);

                return InterruptionAction.StartedDialog;
            }
            else
            {
                return InterruptionAction.NoAction;
            }
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            var cancelling = dc.Context.Activity.CreateReply(PointOfInterestBotResponses.CancellingMessage);
            await dc.Context.SendActivityAsync(cancelling);

            var state = await _accessors.PointOfInterestSkillState.GetAsync(dc.Context);
            state.Clear();

            await dc.CancelAllDialogsAsync();

            return InterruptionAction.NoAction;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            var helpMessage = dc.Context.Activity.CreateReply(PointOfInterestBotResponses.HelpMessage);
            await dc.Context.SendActivityAsync(helpMessage);

            return InterruptionAction.MessageSentToUser;
        }

        private void RegisterDialogs()
        {
            AddDialog(new RouteDialog(_services, _accessors, _serviceManager));
            AddDialog(new CancelRouteDialog(_services, _accessors, _serviceManager));
            AddDialog(new FindPointOfInterestDialog(_services, _accessors, _serviceManager));
        }

        private async Task DigestPointOfInterestLuisResult(DialogContext dc, PointOfInterest luisResult)
        {
            try
            {
                var state = await _accessors.PointOfInterestSkillState.GetAsync(dc.Context, () => new PointOfInterestSkillState());

                if (luisResult != null)
                {
                    var entities = luisResult.Entities;

                    if (entities.KEYWORD != null && entities.KEYWORD.Length != 0)
                    {
                        state.SearchText = string.Join(" ", entities.KEYWORD);
                    }

                    if (entities.ADDRESS != null && entities.ADDRESS.Length != 0)
                    {
                        state.SearchAddress = string.Join(" ", entities.ADDRESS);
                    }

                    if (entities.DESCRIPTOR != null && entities.DESCRIPTOR.Length != 0)
                    {
                        state.SearchDescriptor = string.Join(" ", entities.DESCRIPTOR);
                    }
                }
            }
            catch
            {
                // put log here
            }
        }
    }
}
