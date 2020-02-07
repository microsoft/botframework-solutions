// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Models;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Util;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.Responses.FindPointOfInterest;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Services;
using PointOfInterestSkill.Utilities;
using SkillServiceLibrary.Models;
using SkillServiceLibrary.Services;
using SkillServiceLibrary.Utilities;
using static Microsoft.Recognizers.Text.Culture;

namespace PointOfInterestSkill.Dialogs
{
    public class PointOfInterestDialogBase : ComponentDialog
    {
        private const string FallbackPointOfInterestImageFileName = "default_pointofinterest.png";

        // Constants
        // TODO consider other languages
        private static readonly Dictionary<string, string> SpeakDefaults = new Dictionary<string, string>()
        {
            { "en-US", "en-US-JessaNeural" },
            { "de-DE", "de-DE-KatjaNeural" },
            { "it-IT", "it-IT-ElsaNeural" },
            { "zh-CN", "zh-CN-XiaoxiaoNeural" }
        };

        // TODO same as the one in ConfirmPrompt
        private static readonly Dictionary<string, string> ChoiceDefaults = new Dictionary<string, string>()
        {
            { Spanish, "Sí" },
            { Dutch, "Ja" },
            { English, "Yes" },
            { French, "Oui" },
            { German, "Ja" },
            { Japanese, "はい" },
            { Portuguese, "Sim" },
            { Chinese, "是的" },
        };

        private IHttpContextAccessor _httpContext;

        public PointOfInterestDialogBase(
            string dialogId,
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            IHttpContextAccessor httpContext)
            : base(dialogId)
        {
            Settings = settings;
            Services = services;
            ResponseManager = responseManager;
            Accessor = conversationState.CreateProperty<PointOfInterestSkillState>(nameof(PointOfInterestSkillState));
            ServiceManager = serviceManager;
            TelemetryClient = telemetryClient;
            _httpContext = httpContext;

            AddDialog(new TextPrompt(Actions.CurrentLocationPrompt));
            AddDialog(new ConfirmPrompt(Actions.ConfirmPrompt) { Style = ListStyle.Auto, });
            AddDialog(new ChoicePrompt(Actions.SelectPointOfInterestPrompt, CanNoInterruptablePromptValidator) { Style = ListStyle.None });
            AddDialog(new ChoicePrompt(Actions.SelectActionPrompt, InterruptablePromptValidator) { Style = ListStyle.None });
            AddDialog(new ChoicePrompt(Actions.SelectRoutePrompt) { ChoiceOptions = new ChoiceFactoryOptions { InlineSeparator = string.Empty, InlineOr = string.Empty, InlineOrMore = string.Empty, IncludeNumbers = true } });
        }

        public enum OpenDefaultAppType
        {
            /// <summary>
            /// Telephone app type.
            /// </summary>
            Telephone,

            /// <summary>
            /// Map app type.
            /// </summary>
            Map,
        }

        protected BotSettings Settings { get; set; }

        protected BotServices Services { get; set; }

        protected IStatePropertyAccessor<PointOfInterestSkillState> Accessor { get; set; }

        protected IServiceManager ServiceManager { get; set; }

        protected ResponseManager ResponseManager { get; set; }

        public static Activity CreateOpenDefaultAppReply(Activity activity, PointOfInterestModel destination, OpenDefaultAppType type)
        {
            var replyEvent = activity.CreateReply();
            replyEvent.Type = ActivityTypes.Event;
            replyEvent.Name = "OpenDefaultApp";

            var value = new OpenDefaultApp();
            switch (type)
            {
                case OpenDefaultAppType.Map: value.MapsUri = $"geo:{destination.Geolocation.Latitude},{destination.Geolocation.Longitude}"; break;
                case OpenDefaultAppType.Telephone: value.TelephoneUri = "tel:" + destination.Phone; break;
            }

            replyEvent.Value = value;
            return replyEvent;
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext outerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await base.ContinueDialogAsync(outerDc, cancellationToken);
            var state = await Accessor.GetAsync(outerDc.Context);
            if (state.ShouldInterrupt)
            {
                // Assume already call CancelAllDialogsAsync
                // TODO Empty indicates RouteAsync in RouterDialog
                state.ShouldInterrupt = false;
                return new DialogTurnResult(DialogTurnStatus.Empty);
            }
            else
            {
                return result;
            }
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Clear interrupt state
            var state = await Accessor.GetAsync(dc.Context);
            state.ShouldInterrupt = false;

            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
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

                var pointOfInterestList = await service.GetPointOfInterestListByAddressAsync(double.NaN, double.NaN, sc.Result.ToString());
                var cards = await GetPointOfInterestLocationCards(sc, pointOfInterestList, service);

                if (cards.Count() == 0)
                {
                    var replyMessage = ResponseManager.GetResponse(POISharedResponses.NoLocationsFound);
                    await sc.Context.SendActivityAsync(replyMessage);

                    return await sc.EndDialogAsync();
                }
                else
                {
                    var containerCard = await GetContainerCard(sc.Context, "PointOfInterestOverviewContainer", state.CurrentCoordinates, pointOfInterestList, service);

                    var options = GetPointOfInterestPrompt(cards.Count == 1 ? POISharedResponses.CurrentLocationSingleSelection : POISharedResponses.CurrentLocationMultipleSelection, containerCard, "Container", cards);

                    if (cards.Count == 1)
                    {
                        // Workaround. In teams, HeroCard will be used for prompt and adaptive card could not be shown. So send them separately
                        if (Channel.GetChannelId(sc.Context) == Channels.Msteams)
                        {
                            await sc.Context.SendActivityAsync(options.Prompt);
                            options.Prompt = null;
                        }
                    }

                    return await sc.PromptAsync(cards.Count == 1 ? Actions.ConfirmPrompt : Actions.SelectPointOfInterestPrompt, options);
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

                if (state.ShouldInterrupt)
                {
                    return await sc.CancelAllDialogsAsync();
                }

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
                            return await sc.ReplaceDialogAsync(Actions.CheckForCurrentLocation);
                        }
                    }
                    else if (sc.Result is FoundChoice)
                    {
                        // Update the current coordinates state with user choice.
                        userSelectIndex = (sc.Result as FoundChoice).Index;

                        if (userSelectIndex < 0 || userSelectIndex >= state.LastFoundPointOfInterests.Count)
                        {
                            return await sc.ReplaceDialogAsync(Actions.CheckForCurrentLocation);
                        }

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
                var cards = new List<Card>();

                if (!string.IsNullOrEmpty(state.Category))
                {
                    if (!string.IsNullOrEmpty(state.Keyword))
                    {
                        throw new Exception("Should search only category or keyword!");
                    }

                    if (string.IsNullOrEmpty(state.Address))
                    {
                        // Fuzzy query search with keyword
                        pointOfInterestList = await service.GetPointOfInterestListByCategoryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Category, state.PoiType, true);
                        cards = await GetPointOfInterestLocationCards(sc, pointOfInterestList, service);
                    }
                    else
                    {
                        // Get first POI matched with address, if there are multiple this could be expanded to confirm which address to use
                        var pointOfInterestAddressList = await addressMapsService.GetPointOfInterestListByAddressAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Address, state.PoiType);

                        if (pointOfInterestAddressList.Any())
                        {
                            var pointOfInterest = pointOfInterestAddressList[0];

                            // TODO nearest here is not for current
                            pointOfInterestList = await service.GetPointOfInterestListByCategoryAsync(pointOfInterest.Geolocation.Latitude, pointOfInterest.Geolocation.Longitude, state.Category, state.PoiType, true);
                            cards = await GetPointOfInterestLocationCards(sc, pointOfInterestList, service);
                        }
                        else
                        {
                            // No POIs found from address - search near current coordinates
                            pointOfInterestList = await service.GetPointOfInterestListByCategoryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Category, state.PoiType, true);
                            cards = await GetPointOfInterestLocationCards(sc, pointOfInterestList, service);
                        }
                    }
                }
                else if (string.IsNullOrEmpty(state.Keyword) && string.IsNullOrEmpty(state.Address))
                {
                    // No entities identified, find nearby locations
                    pointOfInterestList = await service.GetNearbyPointOfInterestListAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.PoiType);
                    cards = await GetPointOfInterestLocationCards(sc, pointOfInterestList, service);
                }
                else if (!string.IsNullOrEmpty(state.Keyword) && !string.IsNullOrEmpty(state.Address))
                {
                    // Get first POI matched with address, if there are multiple this could be expanded to confirm which address to use
                    var pointOfInterestAddressList = await addressMapsService.GetPointOfInterestListByAddressAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Address, state.PoiType);

                    if (pointOfInterestAddressList.Any())
                    {
                        var pointOfInterest = pointOfInterestAddressList[0];

                        // TODO nearest here is not for current
                        pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(pointOfInterest.Geolocation.Latitude, pointOfInterest.Geolocation.Longitude, state.Keyword, state.PoiType);
                        cards = await GetPointOfInterestLocationCards(sc, pointOfInterestList, service);
                    }
                    else
                    {
                        // No POIs found from address - search near current coordinates
                        pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Keyword, state.PoiType);
                        cards = await GetPointOfInterestLocationCards(sc, pointOfInterestList, service);
                    }
                }
                else if (!string.IsNullOrEmpty(state.Keyword))
                {
                    // Fuzzy query search with keyword
                    pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Keyword, state.PoiType);
                    cards = await GetPointOfInterestLocationCards(sc, pointOfInterestList, service);
                }
                else if (!string.IsNullOrEmpty(state.Address))
                {
                    // Fuzzy query search with address
                    pointOfInterestList = await addressMapsService.GetPointOfInterestListByAddressAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Address, state.PoiType);
                    cards = await GetPointOfInterestLocationCards(sc, pointOfInterestList, addressMapsService);
                }

                if (cards.Count() == 0)
                {
                    var replyMessage = ResponseManager.GetResponse(POISharedResponses.NoLocationsFound);
                    await sc.Context.SendActivityAsync(replyMessage);

                    return await sc.EndDialogAsync();
                }
                else if (cards.Count == 1)
                {
                    // only to indicate it is only one result
                    return await sc.NextAsync(true);
                }
                else
                {
                    var containerCard = await GetContainerCard(sc.Context, "PointOfInterestOverviewContainer", state.CurrentCoordinates, pointOfInterestList, addressMapsService);

                    var options = GetPointOfInterestPrompt(POISharedResponses.MultipleLocationsFound, containerCard, "Container", cards);

                    return await sc.PromptAsync(Actions.SelectPointOfInterestPrompt, options);
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

                if (state.ShouldInterrupt)
                {
                    return await sc.CancelAllDialogsAsync();
                }

                var defaultReplyMessage = ResponseManager.GetResponse(POISharedResponses.GetRouteToActiveLocationLater);

                if (sc.Result != null)
                {
                    var userSelectIndex = 0;

                    string promptResponse = null;

                    if (sc.Result is bool)
                    {
                        // promptResponse = POISharedResponses.SingleLocationFound;
                        state.Destination = state.LastFoundPointOfInterests[userSelectIndex];
                        state.LastFoundPointOfInterests = null;
                    }
                    else if (sc.Result is FoundChoice)
                    {
                        // promptResponse = FindPointOfInterestResponses.PointOfInterestDetails;

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

                    var options = new PromptOptions()
                    {
                        Choices = new List<Choice>()
                    };

                    bool hasCall = !string.IsNullOrEmpty(state.Destination.Phone);
                    if (hasCall)
                    {
                        options.Choices.Add(new Choice { Value = PointOfInterestSharedStrings.CALL });
                    }

                    options.Choices.Add(new Choice { Value = PointOfInterestSharedStrings.SHOW_DIRECTIONS });
                    options.Choices.Add(new Choice { Value = PointOfInterestSharedStrings.START_NAVIGATION });

                    var mapsService = ServiceManager.InitMapsService(Settings, sc.Context.Activity.Locale);
                    state.Destination = await mapsService.GetPointOfInterestDetailsAsync(state.Destination, ImageSize.DetailsWidth, ImageSize.DetailsHeight);

                    state.Destination.ProviderDisplayText = state.Destination.GenerateProviderDisplayText();

                    state.Destination.CardTitle = PointOfInterestSharedStrings.CARD_TITLE;
                    state.Destination.ActionCall = PointOfInterestSharedStrings.CALL;
                    state.Destination.ActionShowDirections = PointOfInterestSharedStrings.SHOW_DIRECTIONS;
                    state.Destination.ActionStartNavigation = PointOfInterestSharedStrings.START_NAVIGATION;

                    var card = new Card
                    {
                        Name = GetDivergedCardName(sc.Context, string.IsNullOrEmpty(state.Destination.Phone) ? "PointOfInterestDetailsNoCall" : "PointOfInterestDetails"),
                        Data = state.Destination,
                    };

                    if (promptResponse == null)
                    {
                        options.Prompt = ResponseManager.GetCardResponse(card);
                    }
                    else
                    {
                        options.Prompt = ResponseManager.GetCardResponse(promptResponse, card, null);
                    }

                    if (state.DestinationActionType != DestinationActionType.None)
                    {
                        int choiceIndex = -1;
                        if (state.DestinationActionType == DestinationActionType.Call)
                        {
                            choiceIndex = hasCall ? 0 : -1;
                        }
                        else if (state.DestinationActionType == DestinationActionType.ShowDirectionsThenStartNavigation)
                        {
                            choiceIndex = hasCall ? 1 : 0;
                        }
                        else if (state.DestinationActionType == DestinationActionType.StartNavigation)
                        {
                            choiceIndex = hasCall ? 2 : 1;
                        }

                        if (choiceIndex >= 0)
                        {
                            await sc.Context.SendActivityAsync(options.Prompt);
                            return await sc.NextAsync(new FoundChoice() { Index = choiceIndex });
                        }
                    }

                    return await sc.PromptAsync(Actions.SelectActionPrompt, options);
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

        protected async Task<DialogTurnResult> ProcessPointOfInterestAction(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);

            if (state.ShouldInterrupt)
            {
                return await sc.CancelAllDialogsAsync();
            }

            var choice = sc.Result as FoundChoice;
            int choiceIndex = choice.Index;

            SingleDestinationResponse response = null;

            // TODO skip call button
            if (string.IsNullOrEmpty(state.Destination.Phone))
            {
                choiceIndex += 1;
            }

            if (choiceIndex == 0)
            {
                if (SupportOpenDefaultAppReply(sc.Context))
                {
                    await sc.Context.SendActivityAsync(CreateOpenDefaultAppReply(sc.Context.Activity, state.Destination, OpenDefaultAppType.Telephone));
                }

                response = ConvertToResponse(state.Destination);
            }
            else if (choiceIndex == 1)
            {
                return await sc.ReplaceDialogAsync(nameof(RouteDialog));
            }
            else if (choiceIndex == 2)
            {
                if (SupportOpenDefaultAppReply(sc.Context))
                {
                    await sc.Context.SendActivityAsync(CreateOpenDefaultAppReply(sc.Context.Activity, state.Destination, OpenDefaultAppType.Map));
                }

                response = ConvertToResponse(state.Destination);
            }

            return await sc.NextAsync(response);
        }

        protected async Task<Card> GetContainerCard(ITurnContext context, string name, LatLng currentCoordinates, List<PointOfInterestModel> pointOfInterestList, IGeoSpatialService service)
        {
            var model = new PointOfInterestModel
            {
                CardTitle = PointOfInterestSharedStrings.CARD_TITLE,
                PointOfInterestImageUrl = await service.GetAllPointOfInterestsImageAsync(currentCoordinates, pointOfInterestList, ImageSize.OverviewWidth, ImageSize.OverviewHeight),
                Provider = new SortedSet<string> { service.Provider }
            };

            foreach (var poi in pointOfInterestList)
            {
                model.Provider.UnionWith(poi.Provider);
            }

            model.ProviderDisplayText = model.GenerateProviderDisplayText();

            return new Card
            {
                Name = GetDivergedCardName(context, name),
                Data = model
            };
        }

        /// <summary>
        /// Gets ChoicePrompt options with a formatted display name if there are identical locations.
        /// Handle the special yes no case when cards has only one.
        /// </summary>
        /// <param name="prompt">Prompt string.</param>
        /// <param name="containerCard">Container card.</param>
        /// <param name="container">Container.</param>
        /// <param name="cards">List of Cards. Data must be PointOfInterestModel.</param>
        /// <returns>PromptOptions.</returns>
        protected PromptOptions GetPointOfInterestPrompt(string prompt, Card containerCard, string container, List<Card> cards)
        {
            var pointOfInterestList = cards.Select(card => card.Data as PointOfInterestModel).ToList();

            var options = new PromptOptions()
            {
                Choices = new List<Choice>(),
            };

            for (var i = 0; i < pointOfInterestList.Count; ++i)
            {
                var address = pointOfInterestList[i].Address;

                var synonyms = new List<string>()
                {
                    address,
                };

                var choice = new Choice()
                {
                    // Use speak first for SpeechUtility.ListToSpeechReadyString
                    Value = pointOfInterestList[i].Speak,
                    Synonyms = synonyms,
                };
                options.Choices.Add(choice);

                pointOfInterestList[i].SubmitText = pointOfInterestList[i].RawSpeak;
            }

            if (cards.Count == 1)
            {
                pointOfInterestList[0].SubmitText = GetConfirmPromptTrue();
            }

            options.Prompt = ResponseManager.GetCardResponse(prompt, containerCard, null, container, cards);

            // Restore Value to SubmitText
            for (var i = 0; i < pointOfInterestList.Count; ++i)
            {
                options.Choices[i].Value = pointOfInterestList[i].RawSpeak;
            }

            return options;
        }

        // service: for details. the one generates pointOfInterestList
        protected async Task<List<Card>> GetPointOfInterestLocationCards(DialogContext sc, List<PointOfInterestModel> pointOfInterestList, IGeoSpatialService service)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var cards = new List<Card>();

            if (pointOfInterestList != null && pointOfInterestList.Count > 0)
            {
                for (var i = 0; i < pointOfInterestList.Count; i++)
                {
                    pointOfInterestList[i] = await service.GetPointOfInterestDetailsAsync(pointOfInterestList[i], ImageSize.OverviewItemWidth, ImageSize.OverviewItemHeight);

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
                        pointOfInterestList[i].Address = pointOfInterestList[i].AddressAlternative;
                    }
                }

                // Loop again as name may have changed
                for (var i = 0; i < pointOfInterestList.Count; i++)
                {
                    // If multiple points of interest share the same name, use their combined name & address as the speak property.
                    // Otherwise, just use the name.
                    if (pointOfInterestList.Where(x => x.Name == pointOfInterestList[i].Name).Skip(1).Any())
                    {
                        var promptTemplate = POISharedResponses.PointOfInterestSuggestedActionName;
                        var promptReplacements = new StringDictionary
                        {
                            { "Name", WebUtility.HtmlEncode(pointOfInterestList[i].Name) },
                            { "Address", $"<say-as interpret-as='address'>{WebUtility.HtmlEncode(pointOfInterestList[i].AddressForSpeak)}</say-as>" },
                        };
                        pointOfInterestList[i].Speak = ResponseManager.GetResponse(promptTemplate, promptReplacements).Speak;

                        promptReplacements = new StringDictionary
                        {
                            { "Name", pointOfInterestList[i].Name },
                            { "Address", pointOfInterestList[i].AddressForSpeak },
                        };
                        pointOfInterestList[i].RawSpeak = ResponseManager.GetResponse(promptTemplate, promptReplacements).Speak;
                    }
                    else
                    {
                        pointOfInterestList[i].Speak = WebUtility.HtmlEncode(pointOfInterestList[i].Name);
                        pointOfInterestList[i].RawSpeak = pointOfInterestList[i].Name;
                    }
                }

                state.LastFoundPointOfInterests = pointOfInterestList;

                foreach (var pointOfInterest in pointOfInterestList)
                {
                    cards.Add(new Card(GetDivergedCardName(sc.Context, "PointOfInterestOverview"), pointOfInterest));
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

        protected async Task<List<Card>> GetRouteDirectionsViewCards(DialogContext sc, RouteDirections routeDirections, IGeoSpatialService service)
        {
            var routes = routeDirections.Routes;
            var state = await Accessor.GetAsync(sc.Context);
            var cardData = new List<RouteDirectionsModel>();
            var cards = new List<Card>();
            var routeId = 0;

            if (routes != null)
            {
                state.FoundRoutes = routes.Select(route => route.Summary).ToList();

                var destination = state.Destination;
                destination.Provider.Add(routeDirections.Provider);

                foreach (var route in routes)
                {
                    var travelTimeSpan = TimeSpan.FromSeconds(route.Summary.TravelTimeInSeconds);
                    var trafficTimeSpan = TimeSpan.FromSeconds(route.Summary.TrafficDelayInSeconds);

                    // Set card data with formatted time strings and distance converted to miles
                    var routeDirectionsModel = new RouteDirectionsModel()
                    {
                        Name = destination.Name,
                        Address = destination.Address,
                        AvailableDetails = destination.AvailableDetails,
                        Hours = destination.Hours,
                        PointOfInterestImageUrl = await service.GetRouteImageAsync(destination, route, ImageSize.RouteWidth, ImageSize.RouteHeight),
                        TravelTime = GetShortTravelTimespanString(travelTimeSpan),
                        DelayStatus = GetFormattedTrafficDelayString(trafficTimeSpan),
                        Distance = $"{(route.Summary.LengthInMeters / 1609.344).ToString("N1")} {PointOfInterestSharedStrings.MILES_ABBREVIATION}",
                        ETA = route.Summary.ArrivalTime.ToShortTimeString(),
                        TravelTimeSpeak = GetFormattedTravelTimeSpanString(travelTimeSpan),
                        TravelDelaySpeak = GetFormattedTrafficDelayString(trafficTimeSpan),
                        ProviderDisplayText = destination.GenerateProviderDisplayText(),
                        Speak = GetFormattedTravelTimeSpanString(travelTimeSpan),
                        ActionStartNavigation = PointOfInterestSharedStrings.START,
                        CardTitle = PointOfInterestSharedStrings.CARD_TITLE
                    };

                    cardData.Add(routeDirectionsModel);
                    routeId++;
                }

                foreach (var data in cardData)
                {
                    cards.Add(new Card(GetDivergedCardName(sc.Context, "PointOfInterestDetailsWithRoute"), data));
                }
            }

            return cards;
        }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        protected async Task HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace);

            // log exception
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

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

        // Workaround until adaptive card renderer in teams is upgraded to v1.2
        protected string GetDivergedCardName(ITurnContext turnContext, string card)
        {
            if (Channel.GetChannelId(turnContext) == Channels.Msteams)
            {
                return card + ".1.0";
            }
            else
            {
                return card;
            }
        }

        /// <summary>
        /// Decorate speak for speech-synthesis-markup-language.
        /// </summary>
        /// <param name="speak">Speak text that has been converted for html.</param>
        /// <returns>Speak text surrounded by speak voice elements.</returns>
        protected string DecorateSpeak(string speak)
        {
            var culture = CultureInfo.CurrentUICulture.Name;
            if (!SpeakDefaults.ContainsKey(culture))
            {
                culture = "en-US";
            }

            return $"<speak version='1.0' xmlns='https://www.w3.org/2001/10/synthesis' xml:lang='{culture}'><voice name='{SpeakDefaults[culture]}'>{speak}</voice></speak>";
        }

        protected string GetConfirmPromptTrue()
        {
            var culture = CultureInfo.CurrentUICulture.Name.ToLower();
            if (!ChoiceDefaults.ContainsKey(culture))
            {
                culture = English;
            }

            return ChoiceDefaults[culture];
        }

        // workaround. if connect skill directly to teams, the following response does not work.
        protected bool SupportOpenDefaultAppReply(ITurnContext turnContext)
        {
            return turnContext.IsSkill() || Channel.GetChannelId(turnContext) != Channels.Msteams;
        }

        protected SingleDestinationResponse ConvertToResponse(PointOfInterestModel model)
        {
            var response = new SingleDestinationResponse();
            response.Name = model.Name;
            response.Latitude = model.Geolocation.Latitude;
            response.Longitude = model.Geolocation.Longitude;
            response.Telephone = model.Phone;
            response.Address = model.Address;
            return response;
        }

        private string GetCardImageUri(string imagePath)
        {
            var serverUrl = _httpContext.HttpContext.Request.Scheme + "://" + _httpContext.HttpContext.Request.Host.Value;
            return $"{serverUrl}/images/{imagePath}";
        }

        private async Task<bool> InterruptablePromptValidator<T>(PromptValidatorContext<T> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {
                return true;
            }
            else
            {
                var poiResult = promptContext.Context.TurnState.Get<PointOfInterestLuis>(StateProperties.POILuisResultKey);
                var topIntent = poiResult.TopIntent();

                if (topIntent.score > 0.5 && topIntent.intent != PointOfInterestLuis.Intent.None)
                {
                    var state = await Accessor.GetAsync(promptContext.Context);
                    state.ShouldInterrupt = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private async Task<bool> CanNoInterruptablePromptValidator(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {
                return true;
            }
            else
            {
                var state = await Accessor.GetAsync(promptContext.Context);
                var generalLuisResult = promptContext.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                var intent = generalLuisResult.TopIntent().intent;
                if (intent == General.Intent.Reject || intent == General.Intent.SelectNone)
                {
                    promptContext.Recognized.Value = new FoundChoice { Index = -1 };
                    return true;
                }
                else
                {
                    return await InterruptablePromptValidator(promptContext, cancellationToken);
                }
            }
        }

        private class ImageSize
        {
            public const int RouteWidth = 440;
            public const int RouteHeight = 240;
            public const int OverviewWidth = 440;
            public const int OverviewHeight = 150;
            public const int OverviewItemWidth = 240;
            public const int OverviewItemHeight = 240;
            public const int DetailsWidth = 440;
            public const int DetailsHeight = 240;
        }
    }
}