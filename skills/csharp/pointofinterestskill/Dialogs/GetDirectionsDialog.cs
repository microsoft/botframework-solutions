// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Responses;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Services;
using PointOfInterestSkill.Utilities;

namespace PointOfInterestSkill.Dialogs
{
    public class GetDirectionsDialog : PointOfInterestDialogBase
    {
        public GetDirectionsDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            IHttpContextAccessor httpContext)
            : base(nameof(GetDirectionsDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, httpContext)
        {
            TelemetryClient = telemetryClient;

            var checkCurrentLocation = new WaterfallStep[]
            {
                CheckForCurrentCoordinatesBeforeGetDirections,
                ConfirmCurrentLocation,
                ProcessCurrentLocationSelection,
                RouteToFindPointOfInterestDialog
            };

            var findPointOfInterest = new WaterfallStep[]
            {
                GetPointOfInterestLocations,
                SendEvent,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CheckForCurrentLocation, checkCurrentLocation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindPointOfInterest, findPointOfInterest) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.CheckForCurrentLocation;
        }

        protected async Task<DialogTurnResult> CheckForCurrentCoordinatesBeforeGetDirections(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);
            var hasCurrentCoordinates = state.CheckForValidCurrentCoordinates();
            if (hasCurrentCoordinates || !string.IsNullOrEmpty(state.Address))
            {
                return await sc.ReplaceDialogAsync(Actions.FindPointOfInterest);
            }

            return await sc.PromptAsync(Actions.CurrentLocationPrompt, new PromptOptions { Prompt = ResponseManager.GetResponse(POISharedResponses.PromptForCurrentLocation) });
        }

        protected async Task<DialogTurnResult> RouteToFindPointOfInterestDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.ReplaceDialogAsync(Actions.FindPointOfInterest);
        }

        protected async Task<DialogTurnResult> SendEvent(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var userSelectIndex = 0;

            if (sc.Result is bool)
            {
                state.Destination = state.LastFoundPointOfInterests[userSelectIndex];
                state.LastFoundPointOfInterests = null;
            }
            else if (sc.Result is FoundChoice)
            {
                // Update the destination state with user choice.
                userSelectIndex = (sc.Result as FoundChoice).Index;

                if (userSelectIndex < 0 || userSelectIndex >= state.LastFoundPointOfInterests.Count)
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(POISharedResponses.CancellingMessage));
                    return await sc.EndDialogAsync();
                }

                state.Destination = state.LastFoundPointOfInterests[userSelectIndex];
                state.LastFoundPointOfInterests = null;
            }

            if (SupportOpenDefaultAppReply(sc.Context))
            {
                await sc.Context.SendActivityAsync(CreateOpenDefaultAppReply(sc.Context.Activity, state.Destination, OpenDefaultAppType.Map));
            }

            var response = ConvertToResponse(state.Destination);

            return await sc.NextAsync(response);
        }
    }
}
