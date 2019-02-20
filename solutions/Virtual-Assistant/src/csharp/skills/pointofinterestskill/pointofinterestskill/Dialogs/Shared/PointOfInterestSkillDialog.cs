// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using PointOfInterestSkill.Dialogs.Route;
using PointOfInterestSkill.Dialogs.Shared.Resources;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.ServiceClients;

namespace PointOfInterestSkill.Dialogs.Shared
{
    public class PointOfInterestSkillDialog : ComponentDialog
    {
        // Constants
        public const string SkillModeAuth = "SkillAuth";
        public const string LocalModeAuth = "LocalAuth";
        private const string FallbackPointOfInterestImageFileName = "default_pointofinterest.jpg";
        private IHttpContextAccessor _httpContext;

        public PointOfInterestSkillDialog(
            string dialogId,
            SkillConfigurationBase services,
            ResponseManager responseManager,
            IStatePropertyAccessor<PointOfInterestSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            IHttpContextAccessor httpContext)
            : base(dialogId)
        {
            Services = services;
            ResponseManager = responseManager;
            Accessor = accessor;
            ServiceManager = serviceManager;
            TelemetryClient = telemetryClient;
            _httpContext = httpContext;

            AddDialog(new TextPrompt(Actions.Prompt, CustomPromptValidatorAsync));
            AddDialog(new ConfirmPrompt(Actions.ConfirmPrompt) { Style = ListStyle.Auto, });
        }

        protected SkillConfigurationBase Services { get; set; }

        protected IStatePropertyAccessor<PointOfInterestSkillState> Accessor { get; set; }

        protected IServiceManager ServiceManager { get; set; }

        protected ResponseManager ResponseManager { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(dc.Context);
            await DigestPointOfInterestLuisResult(dc, state.LuisResult);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(dc.Context);
            await DigestPointOfInterestLuisResult(dc, state.LuisResult);
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        protected async Task<DialogTurnResult> GetPointOfInterestLocations(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                var service = ServiceManager.InitMapsService(Services, sc.Context.Activity.Locale ?? "en-us");
                var addressMapsService = ServiceManager.InitAddressMapsService(Services, sc.Context.Activity.Locale ?? "en-us");

                var pointOfInterestList = new List<PointOfInterestModel>();

                state.CheckForValidCurrentCoordinates();

                if (string.IsNullOrEmpty(state.Keyword) && string.IsNullOrEmpty(state.Address))
                {
                    // No entities identified, find nearby locations
                    pointOfInterestList = await service.GetNearbyPointOfInterestListAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude);
                    await GetPointOfInterestLocationViewCards(sc, pointOfInterestList);
                }
                else if (!string.IsNullOrEmpty(state.Keyword) && !string.IsNullOrEmpty(state.Address))
                {
                    // Get first POI matched with address, if there are multiple this could be expanded to confirm which address to use
                    var pointOfInterestAddressList = await addressMapsService.GetPointOfInterestListByAddressAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Address);

                    if (pointOfInterestAddressList.Any())
                    {
                        var pointOfInterest = pointOfInterestAddressList[0];
                        pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(pointOfInterest.Geolocation.Latitude, pointOfInterest.Geolocation.Longitude, state.Keyword);
                        await GetPointOfInterestLocationViewCards(sc, pointOfInterestList);
                    }
                    else
                    {
                        // No POIs found from address - search near current coordinates
                        pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Keyword);
                        await GetPointOfInterestLocationViewCards(sc, pointOfInterestList);
                    }
                }
                else if (!string.IsNullOrEmpty(state.Keyword))
                {
                    // Fuzzy query search with keyword
                    pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Keyword);
                    await GetPointOfInterestLocationViewCards(sc, pointOfInterestList);
                }
                else if (!string.IsNullOrEmpty(state.Address))
                {
                    // Fuzzy query search with address
                    pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Address);
                    await GetPointOfInterestLocationViewCards(sc, pointOfInterestList);
                }

                if (pointOfInterestList?.ToList().Count == 1)
                {
                    return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions { Prompt = ResponseManager.GetResponse(POISharedResponses.PromptToGetRoute) });
                }

                state.ClearLuisResults();

                return await sc.EndDialogAsync(true);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ResponseToGetRoutePrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                if ((bool)sc.Result)
                {
                    if (state.Destination != null)
                    {
                        state.Destination = state.LastFoundPointOfInterests.SingleOrDefault();
                        state.LastFoundPointOfInterests = null;
                    }

                    await sc.EndDialogAsync();
                    return await sc.BeginDialogAsync(nameof(RouteDialog));
                }
                else
                {
                    var replyMessage = ResponseManager.GetResponse(POISharedResponses.GetRouteToActiveLocationLater);
                    await sc.Context.SendActivityAsync(replyMessage);
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // Vaildators
        protected Task<bool> CustomPromptValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var result = promptContext.Recognized.Value;
            return Task.FromResult(true);
        }

        // Helpers
        protected async Task GetPointOfInterestLocationViewCards(DialogContext sc, List<PointOfInterestModel> pointOfInterestList)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var service = ServiceManager.InitMapsService(Services);

            if (pointOfInterestList != null && pointOfInterestList.Count > 0)
            {
                state.LastFoundPointOfInterests = pointOfInterestList;

                for (int i = 0; i < pointOfInterestList.Count; i++)
                {
                    pointOfInterestList[i] = await service.GetPointOfInterestDetailsAsync(pointOfInterestList[i]);
                    pointOfInterestList[i].Index = i;

                    if (string.IsNullOrEmpty(pointOfInterestList[i].ImageUrl))
                    {
                        pointOfInterestList[i].ImageUrl = GetCardImageUri(FallbackPointOfInterestImageFileName);
                    }
                }

                if (pointOfInterestList.Count() > 1)
                {
                    var templateId = string.Empty;
                    var cards = new List<Card>();

                    if (sc.ActiveDialog.Id.Equals(Actions.FindAlongRoute) && state.ActiveRoute != null)
                    {
                        templateId = POISharedResponses.MultipleLocationsFoundAlongActiveRoute;
                    }
                    else
                    {
                        templateId = POISharedResponses.MultipleLocationsFound;
                    }

                    foreach (var pointOfInterest in pointOfInterestList)
                    {
                        cards.Add(new Card("PointOfInterestViewCard", pointOfInterest));
                    }

                    var replyMessage = ResponseManager.GetCardResponse(templateId, cards);
                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else
                {
                    state.Destination = state.LastFoundPointOfInterests.Single();
                    var templateId = string.Empty;

                    if (sc.ActiveDialog.Id.Equals(Actions.FindAlongRoute) && state.ActiveRoute != null)
                    {
                        templateId = POISharedResponses.SingleLocationFoundAlongActiveRoute;
                    }
                    else
                    {
                        templateId = POISharedResponses.SingleLocationFound;
                    }

                    var card = new Card("PointOfInterestViewCard", state.Destination);
                    var replyMessage = ResponseManager.GetCardResponse(templateId, card);
                    await sc.Context.SendActivityAsync(replyMessage);
                }
            }
            else
            {
                var replyMessage = ResponseManager.GetResponse(POISharedResponses.NoLocationsFound);
                await sc.Context.SendActivityAsync(replyMessage);
            }
        }

        protected string GetFormattedTravelTimeSpanString(TimeSpan timeSpan)
        {
            var travelTimeSpanString = new StringBuilder();
            if (timeSpan.Hours == 1)
            {
                travelTimeSpanString.Append(timeSpan.Hours + " hour");
            }
            else if (timeSpan.Hours > 1)
            {
                travelTimeSpanString.Append(timeSpan.Hours + " hours");
            }

            if (travelTimeSpanString.Length != 0)
            {
                travelTimeSpanString.Append(" and ");
            }

            if (timeSpan.Minutes < 1)
            {
                travelTimeSpanString.Append(" less than a minute");
            }
            else if (timeSpan.Minutes == 1)
            {
                travelTimeSpanString.Append(timeSpan.Minutes + " minute");
            }
            else if (timeSpan.Minutes > 1)
            {
                travelTimeSpanString.Append(timeSpan.Minutes + " minutes");
            }

            return travelTimeSpanString.ToString();
        }

        protected string GetFormattedTrafficDelayString(TimeSpan timeSpan)
        {
            var trafficDelayTimeSpanString = new StringBuilder();
            if (timeSpan.Hours == 1)
            {
                trafficDelayTimeSpanString.Append(timeSpan.Hours + " hour");
            }
            else if (timeSpan.Hours > 1)
            {
                trafficDelayTimeSpanString.Append(timeSpan.Hours + " hours");
            }

            if (trafficDelayTimeSpanString.Length != 0)
            {
                trafficDelayTimeSpanString.Append(" and ");
            }

            if (timeSpan.Minutes < 1)
            {
                trafficDelayTimeSpanString.Append(" less than a minute.");
            }
            else if (timeSpan.Minutes == 1)
            {
                trafficDelayTimeSpanString.Append(timeSpan.Minutes + " minute.");
            }
            else if (timeSpan.Minutes > 1)
            {
                trafficDelayTimeSpanString.Append(timeSpan.Minutes + " minutes.");
            }

            if (trafficDelayTimeSpanString.Length != 0)
            {
                trafficDelayTimeSpanString.Insert(0, "There is a traffic delay of ");
            }
            else
            {
                trafficDelayTimeSpanString.Append("There is no delay due to traffic.");
            }

            return trafficDelayTimeSpanString.ToString();
        }

        protected async Task GetRouteDirectionsViewCards(DialogContext sc, RouteDirections routeDirections)
        {
            var routes = routeDirections.Routes;
            var state = await Accessor.GetAsync(sc.Context);
            var cardData = new List<RouteDirectionsModelCardData>();
            var routeId = 0;

            if (routes != null)
            {
                state.FoundRoutes = routes.ToList();

                foreach (var route in routes)
                {
                    var travelTimeSpan = TimeSpan.FromSeconds(route.Summary.TravelTimeInSeconds);
                    var trafficTimeSpan = TimeSpan.FromSeconds(route.Summary.TrafficDelayInSeconds);

                    var routeDirectionsModel = new RouteDirectionsModelCardData()
                    {
                        Location = state.Destination.Name,
                        TravelTime = GetFormattedTravelTimeSpanString(travelTimeSpan),
                        TrafficDelay = GetFormattedTrafficDelayString(trafficTimeSpan),
                        RouteId = routeId,
                    };

                    cardData.Add(routeDirectionsModel);
                    routeId++;
                }

                if (cardData.Count() > 1)
                {
                    var cards = new List<Card>();
                    foreach (var data in cardData)
                    {
                        cards.Add(new Card("RouteDirectionsViewCard", data));
                    }

                    var replyMessage = ResponseManager.GetCardResponse(POISharedResponses.MultipleRoutesFound, cards);
                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else
                {
                    var card = new Card("RouteDirectionsViewCardNoGetStartedButton", cardData.SingleOrDefault());
                    var replyMessage = ResponseManager.GetCardResponse(POISharedResponses.SingleRouteFound, card);
                    await sc.Context.SendActivityAsync(replyMessage);
                }
            }
            else
            {
                var replyMessage = ResponseManager.GetResponse(POISharedResponses.NoLocationsFound);
                await sc.Context.SendActivityAsync(replyMessage);
            }
        }

        private string GetCardImageUri(string imagePath)
        {
            // If we are in local mode we leverage the HttpContext to get the current path to the image assets
            if (_httpContext != null)
            {
                string serverUrl = _httpContext.HttpContext.Request.Scheme + "://" + _httpContext.HttpContext.Request.Host.Value;
                return $"{serverUrl}/images/{imagePath}";
            }
            else
            {
                // In skill-mode we don't have HttpContext and require skills to provide their own storage for assets
                Services.Properties.TryGetValue("ImageAssetLocation", out var imageUri);

                var imageUriStr = (string)imageUri;
                if (string.IsNullOrWhiteSpace(imageUriStr))
                {
                    throw new Exception("ImageAssetLocation Uri not configured on the skill.");
                }
                else
                {
                    return $"{imageUriStr}/{imagePath}";
                }
            }
        }

        protected async Task DigestPointOfInterestLuisResult(DialogContext dc, PointOfInterestLU luisResult)
        {
            try
            {
                var state = await Accessor.GetAsync(dc.Context, () => new PointOfInterestSkillState());

                if (luisResult != null)
                {
                    var entities = luisResult.Entities;

                    if (entities.KEYWORD != null)
                    {
                        state.Keyword = string.Join(" ", entities.KEYWORD);
                    }

                    if (entities.ADDRESS != null)
                    {
                        state.Address = string.Join(" ", entities.ADDRESS);
                    }

                    if (entities.ROUTE_TYPE != null)
                    {
                        state.RouteType = entities.ROUTE_TYPE[0][0];
                    }

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


        // This method is called by any waterfall step that throws an exception to ensure consistency
        protected async Task HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace);

            // log exception
            TelemetryClient.TrackExceptionEx(ex, sc.Context.Activity, sc.ActiveDialog?.Id);

            // send error message to bot user
            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(POISharedResponses.PointOfInterestErrorMessage));

            // clear state
            var state = await Accessor.GetAsync(sc.Context);
            state.Clear();
            await sc.CancelAllDialogsAsync();

            return;
        }

        protected async Task HandleDialogException(WaterfallStepContext sc)
        {
            var state = await Accessor.GetAsync(sc.Context);
            state.Clear();
            await Accessor.SetAsync(sc.Context, state);
            await sc.CancelAllDialogsAsync();
        }
    }
}