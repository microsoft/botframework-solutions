// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Telemetry;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Schema;
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
        private const string FallbackPointOfInterestImageFileName = "default_pointofinterest.png";
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

            AddDialog(new TextPrompt(Actions.CurrentLocationPrompt));
            AddDialog(new TextPrompt(Actions.Prompt));
            AddDialog(new ConfirmPrompt(Actions.ConfirmPrompt) { Style = ListStyle.Auto, });
            AddDialog(new ChoicePrompt(Actions.SelectPointOfInterestPrompt) { Style = ListStyle.SuggestedAction, ChoiceOptions = new ChoiceFactoryOptions { InlineSeparator = string.Empty, InlineOr = string.Empty, InlineOrMore = string.Empty, IncludeNumbers = true } });
        }

        protected SkillConfigurationBase Services { get; set; }

        protected IStatePropertyAccessor<PointOfInterestSkillState> Accessor { get; set; }

        protected IServiceManager ServiceManager { get; set; }

        protected ResponseManager ResponseManager { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(dc.Context);
            await DigestLuisResult(dc, state.LuisResult);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(dc.Context);
            if (!dc.ActiveDialog.Id.Equals(Actions.CurrentLocationPrompt))
            {
                await DigestLuisResult(dc, state.LuisResult);
            }

            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        /// <summary>
        /// Looks up the current location and prompts user to select one.
        /// </summary>
        /// <param name="sc">Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        protected async Task<DialogTurnResult> ConfirmCurrentLocation(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var service = ServiceManager.InitAddressMapsService(Services);

                var pointOfInterestList = await service.GetPointOfInterestListByAddressAsync(double.NaN, double.NaN, sc.Result.ToString());
                var cards = await GetPointOfInterestLocationCards(sc, pointOfInterestList);

                if (cards.Count() == 0)
                {
                    var replyMessage = ResponseManager.GetResponse(POISharedResponses.NoLocationsFound);
                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else if (cards.Count == 1)
                {
                    return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions { Prompt = ResponseManager.GetCardResponse(POISharedResponses.CurrentLocationSingleSelection, cards) });
                }
                else
                {
                    PromptOptions options = GetPointOfInterestPrompt(POISharedResponses.CurrentLocationMultipleSelection, pointOfInterestList, cards);

                    return await sc.PromptAsync(Actions.SelectPointOfInterestPrompt, options);
                }

                state.ClearLuisResults();

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        /// <summary>
        /// Process result from choice prompt to select current location.
        /// </summary>
        /// <param name="sc">Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        protected async Task<DialogTurnResult> ProcessCurrentLocationSelection(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var cancelMessage = ResponseManager.GetResponse(POISharedResponses.CancellingMessage);

                if (sc.Result != null)
                {
                    var userSelectIndex = 0;

                    if (sc.Result is bool)
                    {
                        // If true, update the current coordinates state. If false, end dialog.
                        if ((bool)sc.Result)
                        {
                            state.CurrentCoordinates = state.LastFoundPointOfInterests[userSelectIndex].Geolocation;
                            state.LastFoundPointOfInterests = null;
                        }
                        else
                        {
                            await sc.Context.SendActivityAsync(cancelMessage);

                            return await sc.EndDialogAsync();
                        }
                    }
                    else if (sc.Result is FoundChoice)
                    {
                        // Update the current coordinates state with user choice.
                        userSelectIndex = (sc.Result as FoundChoice).Index;

                        state.CurrentCoordinates = state.LastFoundPointOfInterests[userSelectIndex].Geolocation;
                        state.LastFoundPointOfInterests = null;
                    }

                    return await sc.NextAsync();
                }

                await sc.Context.SendActivityAsync(cancelMessage);

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        /// <summary>
        /// Look up points of interest, render cards, and ask user which to route to.
        /// </summary>
        /// <param name="sc">Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        protected async Task<DialogTurnResult> GetPointOfInterestLocations(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                var service = ServiceManager.InitMapsService(Services, sc.Context.Activity.Locale);
                var addressMapsService = ServiceManager.InitAddressMapsService(Services, sc.Context.Activity.Locale);

                var pointOfInterestList = new List<PointOfInterestModel>();
                var cards = new List<Card>();

                if (string.IsNullOrEmpty(state.Keyword) && string.IsNullOrEmpty(state.Address))
                {
                    // No entities identified, find nearby locations
                    pointOfInterestList = await service.GetNearbyPointOfInterestListAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude);
                    cards = await GetPointOfInterestLocationCards(sc, pointOfInterestList);
                }
                else if (!string.IsNullOrEmpty(state.Keyword) && !string.IsNullOrEmpty(state.Address))
                {
                    // Get first POI matched with address, if there are multiple this could be expanded to confirm which address to use
                    var pointOfInterestAddressList = await addressMapsService.GetPointOfInterestListByAddressAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Address);

                    if (pointOfInterestAddressList.Any())
                    {
                        var pointOfInterest = pointOfInterestAddressList[0];
                        pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(pointOfInterest.Geolocation.Latitude, pointOfInterest.Geolocation.Longitude, state.Keyword);
                        cards = await GetPointOfInterestLocationCards(sc, pointOfInterestList);
                    }
                    else
                    {
                        // No POIs found from address - search near current coordinates
                        pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Keyword);
                        cards = await GetPointOfInterestLocationCards(sc, pointOfInterestList);
                    }
                }
                else if (!string.IsNullOrEmpty(state.Keyword))
                {
                    // Fuzzy query search with keyword
                    pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Keyword);
                    cards = await GetPointOfInterestLocationCards(sc, pointOfInterestList);
                }
                else if (!string.IsNullOrEmpty(state.Address))
                {
                    // Fuzzy query search with address
                    pointOfInterestList = await service.GetPointOfInterestListByAddressAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Address);
                    cards = await GetPointOfInterestLocationCards(sc, pointOfInterestList);
                }

                if (cards.Count() == 0)
                {
                    var replyMessage = ResponseManager.GetResponse(POISharedResponses.NoLocationsFound);
                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else if (cards.Count == 1)
                {
                    return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions { Prompt = ResponseManager.GetCardResponse(POISharedResponses.PromptToGetRoute, cards) });
                }
                else
                {
                    PromptOptions options = GetPointOfInterestPrompt(POISharedResponses.MultipleLocationsFound, pointOfInterestList, cards);

                    return await sc.PromptAsync(Actions.SelectPointOfInterestPrompt, options);
                }

                state.ClearLuisResults();

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        /// <summary>
        /// Process result from choice prompt and begin route direction dialog.
        /// </summary>
        /// <param name="sc">Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        protected async Task<DialogTurnResult> ProcessPointOfInterestSelection(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var defaultReplyMessage = ResponseManager.GetResponse(POISharedResponses.GetRouteToActiveLocationLater);

                if (sc.Result != null)
                {
                    var userSelectIndex = 0;

                    if (sc.Result is bool)
                    {
                        // If true, update the destination state. If false, end dialog.
                        if ((bool)sc.Result)
                        {
                            state.Destination = state.LastFoundPointOfInterests[userSelectIndex];
                            state.LastFoundPointOfInterests = null;
                        }
                        else
                        {
                            await sc.Context.SendActivityAsync(defaultReplyMessage);

                            return await sc.EndDialogAsync();
                        }
                    }
                    else if (sc.Result is FoundChoice)
                    {
                        // Update the destination state with user choice.
                        userSelectIndex = (sc.Result as FoundChoice).Index;

                        state.Destination = state.LastFoundPointOfInterests[userSelectIndex];
                        state.LastFoundPointOfInterests = null;
                    }

                    if (sc.ActiveDialog.Id.Equals(Actions.FindAlongRoute) || sc.ActiveDialog.Id.Equals(Actions.FindPointOfInterestBeforeRoute))
                    {
                        return await sc.NextAsync();
                    }
                    else
                    {
                        return await sc.ReplaceDialogAsync(nameof(RouteDialog));
                    }
                }

                await sc.Context.SendActivityAsync(defaultReplyMessage);

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        /// <summary>
        /// Gets ChoicePrompt options with a formatted display name if there are identical locations.
        /// </summary>
        /// <param name="prompt">Prompt string.</param>
        /// <param name="pointOfInterestList">List of PointOfInterestModels.</param>
        /// <param name="cards">List of Cards.</param>
        /// <returns>PromptOptions.</returns>
        protected PromptOptions GetPointOfInterestPrompt(string prompt, List<PointOfInterestModel> pointOfInterestList, List<Card> cards = null)
        {
            var options = new PromptOptions()
            {
                Choices = new List<Choice>(),
            };

            for (var i = 0; i < pointOfInterestList.Count; ++i)
            {
                var item = pointOfInterestList[i].Name;
                var address = pointOfInterestList[i].Address;

                List<string> synonyms = new List<string>()
                    {
                        item,
                        address,
                        (i + 1).ToString(),
                    };

                var suggestedActionValue = item;

                // Use response resource to get formatted name if multiple have the same name
                if (pointOfInterestList.Where(x => x.Name == pointOfInterestList[i].Name).Skip(1).Any())
                {
                    var promptTemplate = POISharedResponses.PointOfInterestSuggestedActionName;
                    var promptReplacements = new StringDictionary
                        {
                            { "Name", item },
                            { "Address", address },
                        };
                    suggestedActionValue = ResponseManager.GetResponse(promptTemplate, promptReplacements).Text;
                }

                var choice = new Choice()
                {
                    Value = suggestedActionValue,
                    Synonyms = synonyms,
                };
                options.Choices.Add(choice);
            }

            options.Prompt = cards == null ? ResponseManager.GetResponse(prompt) : ResponseManager.GetCardResponse(prompt, cards);
            options.Prompt.Speak = SpeechUtility.ListToSpeechReadyString(options.Prompt);

            return options;
        }

        // Validators
        protected async Task<List<PointOfInterestModel>> CurrentLocationValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var result = promptContext.Recognized.Value;
            var service = ServiceManager.InitMapsService(Services);

            var pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(double.NaN, double.NaN, result);

            return await Task.FromResult(pointOfInterestList);
        }

        protected async Task<List<Card>> GetPointOfInterestLocationCards(DialogContext sc, List<PointOfInterestModel> pointOfInterestList)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var service = ServiceManager.InitMapsService(Services);
            var addressService = ServiceManager.InitAddressMapsService(Services);
            var cards = new List<Card>();

            if (pointOfInterestList != null && pointOfInterestList.Count > 0)
            {
                for (int i = 0; i < pointOfInterestList.Count; i++)
                {
                    if (sc.ActiveDialog.Id.Equals(Actions.CheckForCurrentLocation))
                    {
                        pointOfInterestList[i] = await addressService.GetPointOfInterestDetailsAsync(pointOfInterestList[i]);
                    }
                    else
                    {
                        pointOfInterestList[i] = await service.GetPointOfInterestDetailsAsync(pointOfInterestList[i]);
                    }

                    // Increase by one to avoid zero based options to the user which are confusing
                    pointOfInterestList[i].Index = i + 1;

                    if (string.IsNullOrEmpty(pointOfInterestList[i].PointOfInterestImageUrl))
                    {
                        pointOfInterestList[i].PointOfInterestImageUrl = GetCardImageUri(FallbackPointOfInterestImageFileName);
                    }

                    if (string.IsNullOrEmpty(pointOfInterestList[i].Name))
                    {
                        // Show address as the name
                        pointOfInterestList[i].Name = pointOfInterestList[i].Address;
                        pointOfInterestList[i].Address = string.Empty;
                    }

                    pointOfInterestList[i].ProviderDisplayText = string.Format($"{PointOfInterestSharedStrings.POWERED_BY} **{{0}}**", pointOfInterestList[i].Provider.Aggregate((j, k) => j + "&" + k).ToString());

                    // If multiple points of interest share the same name, use their combined name & address as the speak property.
                    // Otherwise, just use the name.
                    if (pointOfInterestList.Where(x => x.Name == pointOfInterestList[i].Name).Skip(1).Any())
                    {
                        var promptTemplate = POISharedResponses.PointOfInterestSuggestedActionName;
                        var promptReplacements = new StringDictionary
                        {
                            { "Name", pointOfInterestList[i].Name },
                            { "Address", pointOfInterestList[i].Address },
                        };
                        pointOfInterestList[i].Speak = ResponseManager.GetResponse(promptTemplate, promptReplacements).Text;
                    }
                    else
                    {
                        pointOfInterestList[i].Speak = pointOfInterestList[i].Name;
                    }
                }

                state.LastFoundPointOfInterests = pointOfInterestList;

                foreach (var pointOfInterest in pointOfInterestList)
                {
                    cards.Add(new Card("PointOfInterestDetails", pointOfInterest));
                }
            }

            return cards;
        }

        protected string GetFormattedTravelTimeSpanString(TimeSpan timeSpan)
        {
            var timeString = new StringBuilder();
            if (timeSpan.Hours == 1)
            {
                timeString.Append(timeSpan.Hours + $" {PointOfInterestSharedStrings.HOUR}");
            }
            else if (timeSpan.Hours > 1)
            {
                timeString.Append(timeSpan.Hours + $" {PointOfInterestSharedStrings.HOURS}");
            }

            if (timeString.Length != 0)
            {
                timeString.Append(" and ");
            }

            if (timeSpan.Minutes < 1)
            {
                timeString.Append($" {PointOfInterestSharedStrings.LESS_THAN_A_MINUTE}");
            }
            else if (timeSpan.Minutes == 1)
            {
                timeString.Append(timeSpan.Minutes + $" {PointOfInterestSharedStrings.MINUTE}");
            }
            else if (timeSpan.Minutes > 1)
            {
                timeString.Append(timeSpan.Minutes + $" {PointOfInterestSharedStrings.MINUTES}");
            }

            return timeString.ToString();
        }

        protected string GetFormattedTrafficDelayString(TimeSpan timeSpan)
        {
            var timeString = new StringBuilder();
            if (timeSpan.Hours == 1)
            {
                timeString.Append(timeSpan.Hours + $" {PointOfInterestSharedStrings.HOUR}");
            }
            else if (timeSpan.Hours > 1)
            {
                timeString.Append(timeSpan.Hours + $" {PointOfInterestSharedStrings.HOURS}");
            }

            if (timeString.Length != 0)
            {
                timeString.Append(" and ");
            }

            if (timeSpan.Minutes < 1)
            {
                timeString.Append($"{PointOfInterestSharedStrings.LESS_THAN_A_MINUTE}");
            }
            else if (timeSpan.Minutes == 1)
            {
                timeString.Append(timeSpan.Minutes + $" {PointOfInterestSharedStrings.MINUTE}");
            }
            else if (timeSpan.Minutes > 1)
            {
                timeString.Append(timeSpan.Minutes + $" {PointOfInterestSharedStrings.MINUTES}");
            }

            var timeReplacements = new StringDictionary
                {
                    { "Time", timeString.ToString() }
                };

            if (timeString.Length != 0)
            {
                var timeTemplate = POISharedResponses.TrafficDelay;

                return ResponseManager.GetResponse(timeTemplate, timeReplacements).Text;
            }
            else
            {
                var timeTemplate = POISharedResponses.NoTrafficDelay;

                return ResponseManager.GetResponse(timeTemplate, timeReplacements).Text;
            }
        }

        protected string GetShortTravelTimespanString(TimeSpan timeSpan)
        {
            var timeString = new StringBuilder();
            if (timeSpan.Hours != 0)
            {
                timeString.Append(timeSpan.Hours + $" {PointOfInterestSharedStrings.HOUR_ABBREVIATION}");
            }

            if (timeSpan.Minutes < 1)
            {
                timeString.Append($"< 1 {PointOfInterestSharedStrings.MINUTE_ABBREVIATION}");
            }
            else
            {
                timeString.Append(timeSpan.Minutes + $" {PointOfInterestSharedStrings.MINUTE_ABBREVIATION}");
            }

            return timeString.ToString();
        }

        protected async Task<List<Card>> GetRouteDirectionsViewCards(DialogContext sc, RouteDirections routeDirections)
        {
            var routes = routeDirections.Routes;
            var state = await Accessor.GetAsync(sc.Context);
            var cardData = new List<RouteDirectionsModel>();
            var cards = new List<Card>();
            var routeId = 0;

            if (routes != null)
            {
                state.FoundRoutes = routes.ToList();

                var destination = state.Destination;

                foreach (var route in routes)
                {
                    var travelTimeSpan = TimeSpan.FromSeconds(route.Summary.TravelTimeInSeconds);
                    var trafficTimeSpan = TimeSpan.FromSeconds(route.Summary.TrafficDelayInSeconds);

                    destination.Provider.Add(routeDirections.Provider);

                    // Set card data with formatted time strings and distance converted to miles
                    var routeDirectionsModel = new RouteDirectionsModel()
                    {
                        Name = destination.Name,
                        Address = destination.Address,
                        AvailableDetails = destination.AvailableDetails,
                        Hours = destination.Hours,
                        PointOfInterestImageUrl = destination.PointOfInterestImageUrl,
                        TravelTime = GetShortTravelTimespanString(travelTimeSpan),
                        DelayStatus = GetFormattedTrafficDelayString(trafficTimeSpan),
                        Distance = $"{(route.Summary.LengthInMeters / 1609.344).ToString("N1")} {PointOfInterestSharedStrings.MILES_ABBREVIATION}",
                        ETA = route.Summary.ArrivalTime.ToShortTimeString(),
                        TravelTimeSpeak = GetFormattedTravelTimeSpanString(travelTimeSpan),
                        TravelDelaySpeak = GetFormattedTrafficDelayString(trafficTimeSpan),
                        ProviderDisplayText = string.Format($"{PointOfInterestSharedStrings.POWERED_BY} **{{0}}**", destination.Provider.Aggregate((j, k) => j + " & " + k).ToString()),
                        Speak = GetFormattedTravelTimeSpanString(travelTimeSpan)
                    };

                    cardData.Add(routeDirectionsModel);
                    routeId++;
                }

                foreach (var data in cardData)
                {
                    cards.Add(new Card("PointOfInterestDetailsWithRoute", data));
                }
            }

            return cards;
        }

        protected async Task DigestLuisResult(DialogContext dc, PointOfInterestLU luisResult)
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
    }
}