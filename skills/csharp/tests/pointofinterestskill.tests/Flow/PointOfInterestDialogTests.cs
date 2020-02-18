// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PointOfInterestSkill.Dialogs;
using PointOfInterestSkill.Responses.Main;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Tests.Flow.Strings;
using PointOfInterestSkill.Tests.Flow.Utterances;

namespace PointOfInterestSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class PointOfInterestDialogTests : PointOfInterestSkillTestBase
    {
        /// <summary>
        /// Find nearest points of interest nearby.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task RouteToNearestPointOfInterestTest()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(this.ParseReplies(POIMainResponses.PointOfInterestWelcomeMessage))
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindPointOfInterestUtterances.FindNearestPoi)
                .AssertReply(AssertContains(null, new string[] { CardStrings.DetailsNoCall }))
                .Send(BaseTestUtterances.ShowDirections)
                .AssertReply(AssertContains(null, new string[] { CardStrings.Route }))
                .Send(BaseTestUtterances.StartNavigation)
                .AssertReply(CheckForEvent())
                .StartTestAsync();
        }

        /// <summary>
        /// Reprompt current location and find nearest points of interest nearby.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task RepromptForCurrentAndRouteToNearestPointOfInterestTest()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(this.ParseReplies(POIMainResponses.PointOfInterestWelcomeMessage))
                .Send(FindPointOfInterestUtterances.FindNearestPoi)
                .AssertReply(AssertContains(POISharedResponses.PromptForCurrentLocation, null))
                .Send(ContextStrings.Ave)
                .AssertReply(AssertContains(POISharedResponses.CurrentLocationMultipleSelection, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.No)
                .AssertReply(AssertContains(POISharedResponses.PromptForCurrentLocation, null))
                .Send(ContextStrings.Ave)
                .AssertReply(AssertContains(POISharedResponses.CurrentLocationMultipleSelection, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(AssertContains(null, new string[] { CardStrings.DetailsNoCall }))
                .Send(BaseTestUtterances.ShowDirections)
                .AssertReply(AssertContains(null, new string[] { CardStrings.Route }))
                .Send(BaseTestUtterances.StartNavigation)
                .AssertReply(CheckForEvent())
                .StartTestAsync();
        }

        /// <summary>
        /// Find points of interest near address and prompt for current location.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task RouteToPointOfInterestAndPromptForCurrentTest()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(this.ParseReplies(POIMainResponses.PointOfInterestWelcomeMessage))
                .Send(FindPointOfInterestUtterances.FindPoiNearAddress)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(AssertContains(null, new string[] { CardStrings.DetailsNoCall }))
                .Send(BaseTestUtterances.ShowDirections)
                .AssertReply(AssertContains(POISharedResponses.PromptForCurrentLocation, null))
                .Send(ContextStrings.Ave)
                .AssertReply(AssertContains(POISharedResponses.CurrentLocationMultipleSelection, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(AssertContains(null, new string[] { CardStrings.Route }))
                .Send(BaseTestUtterances.StartNavigation)
                .AssertReply(CheckForEvent())
                .StartTestAsync();
        }

        /// <summary>
        /// Find points of interest nearby and get directions to one by index number.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task RouteToPointOfInterestByIndexTest()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(this.ParseReplies(POIMainResponses.PointOfInterestWelcomeMessage))
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindPointOfInterestUtterances.WhatsNearby)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(AssertContains(null, new string[] { CardStrings.DetailsNoCall }))
                .Send(BaseTestUtterances.ShowDirections)
                .AssertReply(AssertContains(null, new string[] { CardStrings.Route }))
                .Send(BaseTestUtterances.StartNavigation)
                .AssertReply(CheckForEvent())
                .StartTestAsync();
        }

        /// <summary>
        /// Find points of interest nearby and get directions to one by POI name.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task RouteToPointOfInterestByNameTest()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(this.ParseReplies(POIMainResponses.PointOfInterestWelcomeMessage))
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindPointOfInterestUtterances.WhatsNearby)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound, new string[] { CardStrings.Overview }))
                .Send(ContextStrings.MicrosoftWay)
                .AssertReply(AssertContains(null, new string[] { CardStrings.DetailsNoCall }))
                .Send(BaseTestUtterances.ShowDirections)
                .AssertReply(AssertContains(null, new string[] { CardStrings.Route }))
                .Send(BaseTestUtterances.StartNavigation)
                .AssertReply(CheckForEvent())
                .StartTestAsync();
        }

        /// <summary>
        /// Find points of interest nearby and select none to cancel.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task RouteToPointOfInterestAndSelectNoneTest()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(this.ParseReplies(POIMainResponses.PointOfInterestWelcomeMessage))
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindPointOfInterestUtterances.WhatsNearby)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound, new string[] { CardStrings.Overview }))
                .Send(GeneralTestUtterances.SelectNone)
                .AssertReply(AssertContains(POISharedResponses.CancellingMessage, null))
                .StartTestAsync();
        }

        /// <summary>
        /// Find points of interest nearby by category.
        /// TODO by option 3 since they are merged.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task RouteToPointOfInterestByCategoryTest()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(this.ParseReplies(POIMainResponses.PointOfInterestWelcomeMessage))
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindPointOfInterestUtterances.FindPharmacy)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.OptionThree)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(AssertContains(null, new string[] { CardStrings.DetailsNoCall }))
                .Send(BaseTestUtterances.ShowDirections)
                .AssertReply(AssertContains(null, new string[] { CardStrings.Route }))
                .Send(BaseTestUtterances.StartNavigation)
                .AssertReply(CheckForEvent())
                .StartTestAsync();
        }

        /// <summary>
        /// Find points of interest nearby then call.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task RouteToPointOfInterestThenCallTest()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(this.ParseReplies(POIMainResponses.PointOfInterestWelcomeMessage))
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindPointOfInterestUtterances.WhatsNearby)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.OptionTwo)
                .AssertReply(AssertContains(null, new string[] { CardStrings.Details }))
                .Send(BaseTestUtterances.Call)
                .AssertReply(CheckForEvent(PointOfInterestDialogBase.OpenDefaultAppType.Telephone))
                .StartTestAsync();
        }

        /// <summary>
        /// Find points of interest nearby then start navigation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task RouteToPointOfInterestThenStartNavigationTest()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(this.ParseReplies(POIMainResponses.PointOfInterestWelcomeMessage))
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindPointOfInterestUtterances.WhatsNearby)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(AssertContains(null, new string[] { CardStrings.DetailsNoCall }))
                .Send(BaseTestUtterances.StartNavigation)
                .AssertReply(CheckForEvent())
                .StartTestAsync();
        }

        /// <summary>
        /// Find points of interest nearby with interruptions.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task RouteToPointOfInterestAndInterruptTest()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(this.ParseReplies(POIMainResponses.PointOfInterestWelcomeMessage))
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindPointOfInterestUtterances.WhatsNearby)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound, new string[] { CardStrings.Overview }))
                .Send(FindPointOfInterestUtterances.WhatsNearby)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(AssertContains(null, new string[] { CardStrings.DetailsNoCall }))
                .Send(FindPointOfInterestUtterances.WhatsNearby)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(AssertContains(null, new string[] { CardStrings.DetailsNoCall }))
                .Send(BaseTestUtterances.StartNavigation)
                .AssertReply(CheckForEvent())
                .StartTestAsync();
        }

        /// <summary>
        /// Find points of interest nearby with multiple routes.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task MultipleRouteTest()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(this.ParseReplies(POIMainResponses.PointOfInterestWelcomeMessage))
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindPointOfInterestUtterances.WhatsNearby)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.OptionTwo)
                .AssertReply(AssertContains(null, new string[] { CardStrings.Details }))
                .Send(BaseTestUtterances.ShowDirections)
                .AssertReply(AssertStartsWith(POISharedResponses.MultipleRoutesFound, new string[] { CardStrings.Route, CardStrings.Route }))
                .Send(BaseTestUtterances.StartNavigation)
                .AssertReply(CheckForEvent())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task RouteToPointOfInterestActionTest()
        {
            await GetSkillTestFlow()
                .Send(FindPointOfInterestUtterances.WhatsNearbyAction)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(AssertContains(null, new string[] { CardStrings.DetailsNoCall }))
                .Send(BaseTestUtterances.ShowDirections)
                .AssertReply(AssertContains(null, new string[] { CardStrings.Route }))
                .Send(BaseTestUtterances.StartNavigation)
                .AssertReply(CheckForEvent())
                .AssertReply(CheckForEoC(true))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task RouteToNearestPointOfInterestWithRouteActionTest()
        {
            await GetSkillTestFlow()
                .Send(FindPointOfInterestUtterances.FindNearestPoiWithRouteAction)
                .AssertReply(AssertContains(null, new string[] { CardStrings.DetailsNoCall }))
                .AssertReply(AssertContains(null, new string[] { CardStrings.Route }))
                .AssertReply(CheckForEvent())
                .AssertReply(CheckForEoC(true))
                .StartTestAsync();
        }
    }
}