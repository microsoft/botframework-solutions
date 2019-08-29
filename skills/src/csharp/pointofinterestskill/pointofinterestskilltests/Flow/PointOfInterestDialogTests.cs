using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PointOfInterestSkill.Responses.FindPointOfInterest;
using PointOfInterestSkill.Responses.Route;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkillTests.Flow.Strings;
using PointOfInterestSkillTests.Flow.Utterances;

namespace PointOfInterestSkillTests.Flow
{
    [TestClass]
    public class PointOfInterestDialogTests : PointOfInterestTestBase
    {
        /// <summary>
        /// Find nearest points of interest nearby.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task RouteToNearestPointOfInterestTest()
        {
            await GetTestFlow()
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindPointOfInterestUtterances.FindNearestPoi)
                .AssertReply(AssertContains(POISharedResponses.SingleLocationFound))
                .Send(BaseTestUtterances.ShowDirections)
                .AssertReply(AssertContains(POISharedResponses.SingleRouteFound))
                .AssertReply(AssertContains(RouteResponses.SendingRouteDetails))
                .AssertReply(CheckForEvent())
                .AssertReply(CompleteDialog())
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
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindPointOfInterestUtterances.WhatsNearby)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(AssertContains(FindPointOfInterestResponses.PointOfInterestDetails))
                .Send(BaseTestUtterances.ShowDirections)
                .AssertReply(AssertContains(POISharedResponses.SingleRouteFound))
                .AssertReply(AssertContains(RouteResponses.SendingRouteDetails))
                .AssertReply(CheckForEvent())
                .AssertReply(CompleteDialog())
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
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindPointOfInterestUtterances.WhatsNearby)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound))
                .Send(ContextStrings.MicrosoftWay)
                .AssertReply(AssertContains(FindPointOfInterestResponses.PointOfInterestDetails))
                .Send(BaseTestUtterances.ShowDirections)
                .AssertReply(AssertContains(POISharedResponses.SingleRouteFound))
                .AssertReply(AssertContains(RouteResponses.SendingRouteDetails))
                .AssertReply(CheckForEvent())
                .AssertReply(CompleteDialog())
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
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindPointOfInterestUtterances.WhatsNearby)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(AssertContains(FindPointOfInterestResponses.PointOfInterestDetails))
                .Send(BaseTestUtterances.Call)
                .AssertReply(CheckForEvent())
                .AssertReply(CompleteDialog())
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
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindPointOfInterestUtterances.WhatsNearby)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(AssertContains(FindPointOfInterestResponses.PointOfInterestDetails))
                .Send(BaseTestUtterances.StartNavigation)
                .AssertReply(CheckForEvent())
                .AssertReply(CompleteDialog())
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
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindPointOfInterestUtterances.WhatsNearby)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound))
                .Send(BaseTestUtterances.OptionTwo)
                .AssertReply(AssertContains(FindPointOfInterestResponses.PointOfInterestDetails))
                .Send(BaseTestUtterances.ShowDirections)
                .AssertReply(AssertStartsWith(POISharedResponses.MultipleRoutesFound))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(AssertContains(RouteResponses.SendingRouteDetails))
                .AssertReply(CheckForEvent())
                .AssertReply(CompleteDialog())
                .StartTestAsync();
        }

        /// <summary>
        /// Find parking nearby.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task ParkingNearbyTest()
        {
            await GetTestFlow()
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindParkingUtterances.FindParkingNearby)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(AssertContains(FindPointOfInterestResponses.PointOfInterestDetails))
                .Send(BaseTestUtterances.ShowDirections)
                .AssertReply(AssertContains(POISharedResponses.SingleRouteFound))
                .AssertReply(AssertContains(RouteResponses.SendingRouteDetails))
                .AssertReply(CheckForEvent())
                .AssertReply(CompleteDialog())
                .StartTestAsync();
        }

        /// <summary>
        /// Find nearest parking.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task ParkingNearestTest()
        {
            await GetTestFlow()
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindParkingUtterances.FindParkingNearest)
                .AssertReply(AssertContains(POISharedResponses.SingleLocationFound))
                .Send(BaseTestUtterances.ShowDirections)
                .AssertReply(AssertContains(POISharedResponses.SingleRouteFound))
                .AssertReply(AssertContains(RouteResponses.SendingRouteDetails))
                .AssertReply(CheckForEvent())
                .AssertReply(CompleteDialog())
                .StartTestAsync();
        }

        /// <summary>
        /// Find parking nearby.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task ParkingNearAddressTest()
        {
            await GetTestFlow()
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindParkingUtterances.FindParkingNearAddress)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(AssertContains(FindPointOfInterestResponses.PointOfInterestDetails))
                .Send(BaseTestUtterances.ShowDirections)
                .AssertReply(AssertContains(POISharedResponses.SingleRouteFound))
                .AssertReply(AssertContains(RouteResponses.SendingRouteDetails))
                .AssertReply(CheckForEvent())
                .AssertReply(CompleteDialog())
                .StartTestAsync();
        }

        /// <summary>
        /// Get directions.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task GetDirectionsTest()
        {
            await GetTestFlow()
                .Send(BaseTestUtterances.LocationEvent)
                .Send(RouteFromXToYUtterances.GetToMicrosoft)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(CheckForEvent())
                .AssertReply(CompleteDialog())
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
                .Send(BaseTestUtterances.LocationEvent)
                .Send(RouteFromXToYUtterances.GetToNearestPharmacy)
                .AssertReply(CheckForEvent())
                .AssertReply(CompleteDialog())
                .StartTestAsync();
        }

        private Action<IActivity> AssertStartsWith(string response)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                Assert.IsTrue(ParseReplies(response, new StringDictionary()).Any((reply) =>
                {
                    return messageActivity.Text.StartsWith(reply);
                }));
            };
        }

        private Action<IActivity> AssertContains(string response)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(ParseReplies(response, new StringDictionary()), messageActivity.Text);
            };
        }

        /// <summary>
        /// Asserts bot response of CompleteDialog.
        /// </summary>
        /// <returns>Returns an Action with IActivity object.</returns>
        private Action<IActivity> CompleteDialog()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.Handoff);
            };
        }

        /// <summary>
        /// Asserts bot response of Event Activity.
        /// </summary>
        /// <returns>Returns an Action with IActivity object.</returns>
        private Action<IActivity> CheckForEvent()
        {
            return activity =>
            {
                var eventReceived = activity.AsEventActivity();
                Assert.IsNotNull(eventReceived, "Activity received is not an Event as expected");
            };
        }
    }
}