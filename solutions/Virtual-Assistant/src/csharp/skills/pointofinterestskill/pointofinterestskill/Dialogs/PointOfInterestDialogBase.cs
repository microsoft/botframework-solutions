﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
using Microsoft.Bot.Builder.Solutions.Telemetry;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Schema;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Services;
using PointOfInterestSkill.Utilities;

namespace PointOfInterestSkill.Dialogs
{
    public class PointOfInterestDialogBase : ComponentDialog
    {
        // Constants
        public const string SkillModeAuth = "SkillAuth";
        public const string LocalModeAuth = "LocalAuth";
        private const string FallbackPointOfInterestImageFileName = "default_pointofinterest.png";
        private IHttpContextAccessor _httpContext;

        public PointOfInterestDialogBase(
            string dialogId,
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            IStatePropertyAccessor<PointOfInterestSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            IHttpContextAccessor httpContext)
            : base(dialogId)
        {
            Settings = settings;
            Services = services;
            ResponseManager = responseManager;
            Accessor = accessor;
            ServiceManager = serviceManager;
            TelemetryClient = telemetryClient;
            _httpContext = httpContext;

            AddDialog(new TextPrompt(Utilities.Actions.CurrentLocationPrompt));
            AddDialog(new TextPrompt(Utilities.Actions.Prompt));
            AddDialog(new ConfirmPrompt(Utilities.Actions.ConfirmPrompt) { Style = ListStyle.Auto, });
            AddDialog(new ChoicePrompt(Utilities.Actions.SelectPointOfInterestPrompt) { Style = ListStyle.Auto, ChoiceOptions = new ChoiceFactoryOptions { InlineSeparator = string.Empty, InlineOr = string.Empty, InlineOrMore = string.Empty, IncludeNumbers = true } });
        }

        protected BotSettings Settings { get; set; }

        protected BotServices Services { get; set; }

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
            if (!dc.ActiveDialog.Id.Equals(Utilities.Actions.CurrentLocationPrompt))
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
                var service = ServiceManager.InitAddressMapsService(Settings);

                var pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(double.NaN, double.NaN, sc.Result.ToString());
                pointOfInterestList = await GetPointOfInterestLocationCards(sc, pointOfInterestList);

                if (pointOfInterestList?.ToList().Count == 1)
                {
                    return await sc.PromptAsync(Utilities.Actions.ConfirmPrompt, new PromptOptions { Prompt = ResponseManager.GetResponse(POISharedResponses.CurrentLocationSingleSelection) });
                }
                else
                {
                    var options = GetPointOfInterestChoicePromptOptions(pointOfInterestList);
                    options.Prompt = ResponseManager.GetResponse(POISharedResponses.CurrentLocationMultipleSelection);

                    return await sc.PromptAsync(Utilities.Actions.SelectPointOfInterestPrompt, options);
                }
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

                var service = ServiceManager.InitMapsService(Settings, sc.Context.Activity.Locale);
                var addressMapsService = ServiceManager.InitAddressMapsService(Settings, sc.Context.Activity.Locale);

                var pointOfInterestList = new List<PointOfInterestModel>();

                if (string.IsNullOrEmpty(state.Keyword) && string.IsNullOrEmpty(state.Address))
                {
                    // No entities identified, find nearby locations
                    pointOfInterestList = await service.GetNearbyPointOfInterestListAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude);
                    pointOfInterestList = await GetPointOfInterestLocationCards(sc, pointOfInterestList);
                }
                else if (!string.IsNullOrEmpty(state.Keyword) && !string.IsNullOrEmpty(state.Address))
                {
                    // Get first POI matched with address, if there are multiple this could be expanded to confirm which address to use
                    var pointOfInterestAddressList = await addressMapsService.GetPointOfInterestListByAddressAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Address);

                    if (pointOfInterestAddressList.Any())
                    {
                        var pointOfInterest = pointOfInterestAddressList[0];
                        pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(pointOfInterest.Geolocation.Latitude, pointOfInterest.Geolocation.Longitude, state.Keyword);
                        pointOfInterestList = await GetPointOfInterestLocationCards(sc, pointOfInterestList);
                    }
                    else
                    {
                        // No POIs found from address - search near current coordinates
                        pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Keyword);
                        pointOfInterestList = await GetPointOfInterestLocationCards(sc, pointOfInterestList);
                    }
                }
                else if (!string.IsNullOrEmpty(state.Keyword))
                {
                    // Fuzzy query search with keyword
                    pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Keyword);
                    pointOfInterestList = await GetPointOfInterestLocationCards(sc, pointOfInterestList);
                }
                else if (!string.IsNullOrEmpty(state.Address))
                {
                    // Fuzzy query search with address
                    pointOfInterestList = await service.GetPointOfInterestListByAddressAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Address);
                    pointOfInterestList = await GetPointOfInterestLocationCards(sc, pointOfInterestList);
                }

                if (pointOfInterestList?.ToList().Count == 1)
                {
                    return await sc.PromptAsync(Utilities.Actions.ConfirmPrompt, new PromptOptions { Prompt = ResponseManager.GetResponse(POISharedResponses.PromptToGetRoute) });
                }
                else
                {
                    var options = GetPointOfInterestChoicePromptOptions(pointOfInterestList);

                    return await sc.PromptAsync(Utilities.Actions.SelectPointOfInterestPrompt, options);
                }
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

                    if (sc.ActiveDialog.Id.Equals(Utilities.Actions.FindAlongRoute) || sc.ActiveDialog.Id.Equals(Utilities.Actions.FindPointOfInterestBeforeRoute))
                    {
                        return await sc.NextAsync();
                    }
                    else
                    {
                        return await sc.ReplaceDialogAsync(nameof(Dialogs.RouteDialog));
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
        /// <param name="pointOfInterestList">List of PointOfInterestModels.</param>
        /// <returns>PromptOptions.</returns>
        protected PromptOptions GetPointOfInterestChoicePromptOptions(List<PointOfInterestModel> pointOfInterestList)
        {
            var options = new PromptOptions()
            {
                Choices = new List<Choice>(),
            };

            for (var i = 0; i < pointOfInterestList.Count; ++i)
            {
                var item = pointOfInterestList[i].Name;
                var address = pointOfInterestList[i].Street;

                var synonyms = new List<string>()
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

            options.Prompt = ResponseManager.GetResponse(POISharedResponses.PointOfInterestSelection);
            return options;
        }

        // Validators
        protected async Task<List<PointOfInterestModel>> CurrentLocationValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var result = promptContext.Recognized.Value;
            var service = ServiceManager.InitMapsService(Settings);

            var pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(double.NaN, double.NaN, result);

            return await Task.FromResult(pointOfInterestList);
        }

        protected async Task<List<PointOfInterestModel>> GetPointOfInterestLocationCards(DialogContext sc, List<PointOfInterestModel> pointOfInterestList)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var service = ServiceManager.InitMapsService(Settings);
            var addressService = ServiceManager.InitAddressMapsService(Settings);

            if (pointOfInterestList != null && pointOfInterestList.Count > 0)
            {
                for (var i = 0; i < pointOfInterestList.Count; i++)
                {
                    if (sc.ActiveDialog.Id.Equals(Utilities.Actions.CheckForCurrentLocation))
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
                        pointOfInterestList[i].Name = pointOfInterestList[i].Street;
                    }

                    pointOfInterestList[i].ProviderDisplayText = string.Format($"{PointOfInterestSharedStrings.POWERED_BY} **{{0}}**", pointOfInterestList[i].Provider.Aggregate((j, k) => j + "&" + k).ToString());
                }

                state.LastFoundPointOfInterests = pointOfInterestList;

                if (pointOfInterestList.Count() > 1)
                {
                    var templateId = POISharedResponses.MultipleLocationsFound;
                    var cards = new List<Card>();

                    foreach (var pointOfInterest in pointOfInterestList)
                    {
                        cards.Add(new Card("PointOfInterestDetails", pointOfInterest));
                    }

                    var replyMessage = ResponseManager.GetCardResponse(templateId, cards);

                    replyMessage.Speak = ResponseUtility.BuildSpeechFriendlyPoIResponse(replyMessage);

                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else
                {
                    var templateId = POISharedResponses.SingleLocationFound;

                    var card = new Card("PointOfInterestDetails", state.LastFoundPointOfInterests[0]);
                    var replyMessage = ResponseManager.GetCardResponse(templateId, card, tokens: null);
                    replyMessage.Speak = ResponseUtility.BuildSpeechFriendlyPoIResponse(replyMessage);

                    await sc.Context.SendActivityAsync(replyMessage);
                }
            }
            else
            {
                var replyMessage = ResponseManager.GetResponse(POISharedResponses.NoLocationsFound);
                await sc.Context.SendActivityAsync(replyMessage);
            }

            return pointOfInterestList;
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

        protected async Task GetRouteDirectionsViewCards(DialogContext sc, RouteDirections routeDirections)
        {
            var routes = routeDirections.Routes;
            var state = await Accessor.GetAsync(sc.Context);
            var cardData = new List<RouteDirectionsModel>();
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
                        Street = destination.Street,
                        City = destination.City,
                        AvailableDetails = destination.AvailableDetails,
                        Hours = destination.Hours,
                        PointOfInterestImageUrl = destination.PointOfInterestImageUrl,
                        TravelTime = GetShortTravelTimespanString(travelTimeSpan),
                        DelayStatus = GetFormattedTrafficDelayString(trafficTimeSpan),
                        Distance = $"{(route.Summary.LengthInMeters / 1609.344).ToString("N1")} {PointOfInterestSharedStrings.MILES_ABBREVIATION}",
                        ETA = route.Summary.ArrivalTime.ToShortTimeString(),
                        TravelTimeSpeak = GetFormattedTravelTimeSpanString(travelTimeSpan),
                        TravelDelaySpeak = GetFormattedTrafficDelayString(trafficTimeSpan),
                        ProviderDisplayText = string.Format($"{PointOfInterestSharedStrings.POWERED_BY} **{{0}}**", destination.Provider.Aggregate((j, k) => j + " & " + k).ToString())
                    };

                    cardData.Add(routeDirectionsModel);
                    routeId++;
                }

                if (cardData.Count() > 1)
                {
                    var cards = new List<Card>();
                    foreach (var data in cardData)
                    {
                        cards.Add(new Card("PointOfInterestDetailsWithRoute", data));
                    }

                    var replyMessage = ResponseManager.GetCardResponse(POISharedResponses.MultipleRoutesFound, cards);
                    replyMessage.Speak = ResponseUtility.BuildSpeechFriendlyPoIResponse(replyMessage);
                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else
                {
                    var card = new Card("PointOfInterestDetailsWithRoute", cardData.SingleOrDefault());
                    var replyMessage = ResponseManager.GetCardResponse(POISharedResponses.SingleRouteFound, card, tokens: null);
                    replyMessage.Speak = ResponseUtility.BuildSpeechFriendlyPoIResponse(replyMessage);
                    await sc.Context.SendActivityAsync(replyMessage);
                }
            }
            else
            {
                var replyMessage = ResponseManager.GetResponse(POISharedResponses.NoLocationsFound);
                await sc.Context.SendActivityAsync(replyMessage);
            }
        }

        protected async Task DigestLuisResult(DialogContext dc, PointOfInterestLuis luisResult)
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
                var serverUrl = _httpContext.HttpContext.Request.Scheme + "://" + _httpContext.HttpContext.Request.Host.Value;
                return $"{serverUrl}/images/{imagePath}";
            }
            else
            {
                // In skill-mode we don't have HttpContext and require skills to provide their own storage for assets
                Settings.Properties.TryGetValue("ImageAssetLocation", out var imageUri);

                var imageUriStr = imageUri;
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