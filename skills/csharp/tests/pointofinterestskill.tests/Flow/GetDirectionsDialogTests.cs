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
    public class GetDirectionsDialogTests : PointOfInterestSkillTestBase
    {
        /// <summary>
        /// Get directions.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task GetDirectionsTest()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(this.ParseReplies(POIMainResponses.PointOfInterestWelcomeMessage))
                .Send(BaseTestUtterances.LocationEvent)
                .Send(RouteFromXToYUtterances.GetToMicrosoft)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(CheckForEvent())
                .StartTestAsync();
        }

        /// <summary>
        /// Get directions to some nearest place.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task GetDirectionsToNearestTest()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(this.ParseReplies(POIMainResponses.PointOfInterestWelcomeMessage))
                .Send(BaseTestUtterances.LocationEvent)
                .Send(RouteFromXToYUtterances.GetToNearestPharmacy)
                .AssertReply(CheckForEvent())
                .StartTestAsync();
        }

        /// <summary>
        /// Reprompt for current location and get directions to some nearest place.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task RepromptForCurrentAndGetDirectionsToNearestTest()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(this.ParseReplies(POIMainResponses.PointOfInterestWelcomeMessage))
                .Send(RouteFromXToYUtterances.GetToNearestPharmacy)
                .AssertReply(AssertContains(POISharedResponses.PromptForCurrentLocation, null))
                .Send(ContextStrings.Ave)
                .AssertReply(AssertContains(POISharedResponses.CurrentLocationMultipleSelection, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.No)
                .AssertReply(AssertContains(POISharedResponses.PromptForCurrentLocation, null))
                .Send(ContextStrings.Ave)
                .AssertReply(AssertContains(POISharedResponses.CurrentLocationMultipleSelection, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(CheckForEvent())
                .StartTestAsync();
        }

        /// <summary>
        /// Get directions near address (no prompt for current since no routing).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task GetDirectionsNearAddressTest()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(this.ParseReplies(POIMainResponses.PointOfInterestWelcomeMessage))
                .Send(RouteFromXToYUtterances.GetToMicrosoftNearAddress)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(CheckForEvent())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task GetDirectionsActionTest()
        {
            await GetSkillTestFlow()
                .Send(RouteFromXToYUtterances.FindRouteAction)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(CheckForEvent())
                .AssertReply(CheckForEoC(true))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task GetDirectionsToNearestNoCurrentActionTest()
        {
            await GetSkillTestFlow()
                .Send(RouteFromXToYUtterances.GetToNearestPharmacyNoCurrentAction)
                .AssertReply(AssertContains(POISharedResponses.PromptForCurrentLocation, null))
                .Send(ContextStrings.Ave)
                .AssertReply(AssertContains(POISharedResponses.CurrentLocationMultipleSelection, new string[] { CardStrings.Overview }))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(CheckForEvent())
                .AssertReply(CheckForEoC(true))
                .StartTestAsync();
        }
    }
}
