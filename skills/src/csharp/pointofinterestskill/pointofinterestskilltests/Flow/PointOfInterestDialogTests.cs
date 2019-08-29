using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
                .AssertReply(SingleLocationFound())
                .Send(BaseTestUtterances.Yes)
                .AssertReply(SingleRouteFound())
                .AssertReply(SendingRouteDetails())
                .AssertReply(CheckForEvent())
                .AssertReply(CompleteDialog())
                .StartTestAsync();
        }

        /// <summary>
        /// Find points of interest nearby and get directions to one by event.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task RouteToPointOfInterestByEventTest()
        {
            await GetTestFlow()
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindPointOfInterestUtterances.WhatsNearby)
                .AssertReply(MultipleLocationsFound())
                .Send(BaseTestUtterances.ActiveLocationEvent)
                .AssertReply(SingleRouteFound())
                .AssertReply(SendingRouteDetails())
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
                .AssertReply(MultipleLocationsFound())
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(SingleRouteFound())
                .AssertReply(SendingRouteDetails())
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
                .AssertReply(MultipleLocationsFound())
                .Send(ContextStrings.MicrosoftWay)
                .AssertReply(SingleRouteFound())
                .AssertReply(SendingRouteDetails())
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
                .AssertReply(MultipleLocationsFound())
                .Send(BaseTestUtterances.OptionTwo)
                .AssertReply(MultipleRoutesFound())
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(SendingRouteDetails())
                .AssertReply(CheckForEvent())
                .AssertReply(CompleteDialog())
                .StartTestAsync();
        }

        /// <summary>
        /// Find points of interest nearby, attempt to cancel and succeed.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CancelRouteSuccessTest()
        {
            await GetTestFlow()
                .Send(BaseTestUtterances.LocationEvent)
                .Send(FindPointOfInterestUtterances.WhatsNearby)
                .AssertReply(MultipleLocationsFound())
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(SingleRouteFound())
                .AssertReply(SendingRouteDetails())
                .AssertReply(CheckForEvent())
                .Send(CancelRouteUtterances.CancelRoute)
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
                .AssertReply(MultipleLocationsFound())
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(SingleRouteFound())
                .AssertReply(SendingRouteDetails())
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
                .AssertReply(SingleLocationFound())
                .Send(BaseTestUtterances.Yes)
                .AssertReply(SingleRouteFound())
                .AssertReply(SendingRouteDetails())
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
                .AssertReply(MultipleLocationsFound())
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(SingleRouteFound())
                .AssertReply(SendingRouteDetails())
                .AssertReply(CheckForEvent())
                .AssertReply(CompleteDialog())
                .StartTestAsync();
        }

        /// <summary>
        /// Asserts bot response of MultipleLocationsFound.
        /// </summary>
        /// <returns>Returns an Action with IActivity object.</returns>
        private Action<IActivity> MultipleLocationsFound()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                Assert.IsTrue(ParseReplies(POISharedResponses.MultipleLocationsFound, new StringDictionary()).Any((reply) =>
                {
                    return messageActivity.Text.StartsWith(reply);
                }));
            };
        }

        /// <summary>
        /// Asserts bot response of SingleLocationFound.
        /// </summary>
        /// <returns>Returns an Action with IActivity object.</returns>
        private Action<IActivity> SingleLocationFound()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                Assert.IsTrue(ParseReplies(POISharedResponses.PromptToGetRoute, new StringDictionary()).Any((reply) =>
                {
                    return messageActivity.Text.StartsWith(reply);
                }));
            };
        }

        /// <summary>
        /// Asserts bot response of SingleRouteFound.
        /// </summary>
        /// <returns>Returns an Action with IActivity object.</returns>
        private Action<IActivity> SingleRouteFound()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(ParseReplies(POISharedResponses.SingleRouteFound, new StringDictionary()), messageActivity.Text);
            };
        }

        /// <summary>
        /// Asserts bot response of MultipleRoutesFound.
        /// </summary>
        /// <returns>Returns an Action with IActivity object.</returns>
        private Action<IActivity> MultipleRoutesFound()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                Assert.IsTrue(ParseReplies(POISharedResponses.MultipleRoutesFound, new StringDictionary()).Any((reply) =>
                {
                    return messageActivity.Text.StartsWith(reply);
                }));
            };
        }

        /// <summary>
        /// Asserts bot response of SendingRouteDetails.
        /// </summary>
        /// <returns>Returns an Action with IActivity object.</returns>
        private Action<IActivity> SendingRouteDetails()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(ParseReplies(RouteResponses.SendingRouteDetails, new StringDictionary()), messageActivity.Text);
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