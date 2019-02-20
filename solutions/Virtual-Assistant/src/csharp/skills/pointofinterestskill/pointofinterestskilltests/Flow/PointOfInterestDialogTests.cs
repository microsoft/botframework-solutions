using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PointOfInterestSkill.Dialogs.CancelRoute.Resources;
using PointOfInterestSkill.Dialogs.Route.Resources;
using PointOfInterestSkill.Dialogs.Shared.Resources;
using PointOfInterestSkillTests.Flow.Utterances;
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using VirtualAssistant.Dialogs.Main.Resources;

namespace PointOfInterestSkillTests.Flow
{
    [TestClass]
    public class PointOfInterestDialogTests : PointOfInterestTestBase
    {
        /// <summary>
        /// Find points of interest nearby.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task WhatsNearbyTest()
        {
            await GetTestFlow()
                .Send(PointOfInterestDialogUtterances.LocationEvent)
                .Send(PointOfInterestDialogUtterances.WhatsNearby)
                .AssertReply(MultipleLocationsFound())
                .AssertReply(CompleteDialog())
                .StartTestAsync();
        }

        /// <summary>
        /// Find points of interest nearby and get directions to one by event.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task RouteToPointOfInterestByEventTest()
        {
            await GetTestFlow()
                .Send(PointOfInterestDialogUtterances.LocationEvent)
                .Send(PointOfInterestDialogUtterances.WhatsNearby)
                .AssertReply(MultipleLocationsFound())
                .AssertReply(CompleteDialog())
                .Send(PointOfInterestDialogUtterances.ActiveLocationEvent)
                .AssertReply(SingleRouteFound())
                .AssertReply(PromptToStartRoute())
                .Send(GeneralUtterances.Yes)
                .AssertReply(SendingRouteDetails())
                .AssertReply(CheckForEvent())
                .AssertReply(CompleteDialog())
                .StartTestAsync();
        }

        /// <summary>
        /// Find points of interest nearby and get directions to one by index number.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task RouteToPointOfInterestByIndexTest()
        {
            await GetTestFlow()
                .Send(PointOfInterestDialogUtterances.LocationEvent)
                .Send(PointOfInterestDialogUtterances.WhatsNearby)
                .AssertReply(MultipleLocationsFound())
                .AssertReply(CompleteDialog())
                .Send(GeneralUtterances.OptionOne)
                .AssertReply(SingleRouteFound())
                .AssertReply(PromptToStartRoute())
                .Send(GeneralUtterances.Yes)
                .AssertReply(SendingRouteDetails())
                .AssertReply(CheckForEvent())
                .AssertReply(CompleteDialog())
                .StartTestAsync();
        }

        /// <summary>
        /// Find points of interest nearby and get directions to one by POI name.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task RouteToPointOfInterestByNameTest()
        {
            await GetTestFlow()
                .Send(PointOfInterestDialogUtterances.LocationEvent)
                .Send(PointOfInterestDialogUtterances.WhatsNearby)
                .AssertReply(MultipleLocationsFound())
                .AssertReply(CompleteDialog())
                .Send(PointOfInterestDialogUtterances.GetDirectionsByName)
                .AssertReply(SingleRouteFound())
                .AssertReply(PromptToStartRoute())
                .Send(GeneralUtterances.Yes)
                .AssertReply(SendingRouteDetails())
                .AssertReply(CheckForEvent())
                .AssertReply(CompleteDialog())
                .StartTestAsync();
        }

        /// <summary>
        /// Find points of interest nearby and get ask for directions 
        /// to a name that was not found in the recent search. Skill searches
        /// for new points of interest based on matched keyword.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task RouteToPointOfInterestByNewNameTest()
        {
            await GetTestFlow()
                .Send(PointOfInterestDialogUtterances.LocationEvent)
                .Send(PointOfInterestDialogUtterances.WhatsNearby)
                .AssertReply(MultipleLocationsFound())
                .AssertReply(CompleteDialog())
                .Send(PointOfInterestDialogUtterances.GetDirectionsByNewName)
                .AssertReply(MultipleLocationsFound())
                .AssertReply(CompleteDialog())
                .Send(GeneralUtterances.OptionOne)
                .AssertReply(SingleRouteFound())
                .AssertReply(PromptToStartRoute())
                .Send(GeneralUtterances.Yes)
                .AssertReply(SendingRouteDetails())
                .AssertReply(CheckForEvent())
                .AssertReply(CompleteDialog())
                .StartTestAsync();
        }



        /// <summary>
        /// Find points of interest nearby, attempt to cancel a route but fail because there is no active route.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task CancelRouteFailureTest()
        {
            await GetTestFlow()
                .Send(PointOfInterestDialogUtterances.LocationEvent)
                .Send(PointOfInterestDialogUtterances.WhatsNearby)
                .AssertReply(MultipleLocationsFound())
                .AssertReply(CompleteDialog())
                .Send(PointOfInterestDialogUtterances.CancelRoute)
                .AssertReply(CannotCancelActiveRoute())
                .AssertReply(CompleteDialog())
                .StartTestAsync();
        }

        /// <summary>
        /// Find points of interest nearby, attempt to cancel and succeed.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task CancelRouteSuccessTest()
        {
            await GetTestFlow()
                .Send(PointOfInterestDialogUtterances.LocationEvent)
                .Send(PointOfInterestDialogUtterances.WhatsNearby)
                .AssertReply(MultipleLocationsFound())
                .AssertReply(CompleteDialog())
                .Send(PointOfInterestDialogUtterances.ActiveLocationEvent)
                .AssertReply(SingleRouteFound())
                .AssertReply(PromptToStartRoute())
                .Send(GeneralUtterances.Yes)
                .AssertReply(SendingRouteDetails())
                .AssertReply(CheckForEvent())
                .Send(PointOfInterestDialogUtterances.CancelRoute)
                .AssertReply(CompleteDialog())
                .StartTestAsync();
        }

        /// <summary>
        /// Find parking nearby.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task ParkingNearbyTest()
        {
            await GetTestFlow()
                .Send(PointOfInterestDialogUtterances.LocationEvent)
                .Send(PointOfInterestDialogUtterances.FindParkingNearby)
                .AssertReply(MultipleLocationsFound())
                .AssertReply(CompleteDialog())
                .StartTestAsync();
        }

        /// <summary>
        /// Find parking nearby.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task ParkingNearAddressTest()
        {
            await GetTestFlow()
                .Send(PointOfInterestDialogUtterances.LocationEvent)
                .Send(PointOfInterestDialogUtterances.FindParkingNearAddress)
                .AssertReply(MultipleLocationsFound())
                .AssertReply(CompleteDialog())
                .StartTestAsync();
        }

        /// <summary>
        /// Asserts bot response of MultipleLocationsFound
        /// </summary>
        /// <returns></returns>
        private Action<IActivity> MultipleLocationsFound()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(ParseReplies(POISharedResponses.MultipleLocationsFound, new StringDictionary()), messageActivity.Text);
            };
        }

        /// <summary>
        /// Asserts bot response of SingleLocationFound
        /// </summary>
        /// <returns></returns>
        private Action<IActivity> SingleLocationFound()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(ParseReplies(POISharedResponses.SingleLocationFound, new StringDictionary()), messageActivity.Text);
            };
        }

        /// <summary>
        /// Asserts bot response of SingleRouteFound
        /// </summary>
        /// <returns></returns>
        private Action<IActivity> SingleRouteFound()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(ParseReplies(POISharedResponses.SingleRouteFound, new StringDictionary()), messageActivity.Text);
            };
        }


        /// <summary>
        /// Asserts bot response of MultipleRoutesFound
        /// </summary>
        /// <returns></returns>
        private Action<IActivity> MultipleRoutesFound()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(ParseReplies(POISharedResponses.MultipleRoutesFound, new StringDictionary()), messageActivity.Text);
            };
        }


        /// <summary>
        /// Asserts bot response of PromptToStartRoute
        /// </summary>
        /// <returns></returns>
        private Action<IActivity> PromptToStartRoute()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(ParseReplies(RouteResponses.PromptToStartRoute, new StringDictionary()), messageActivity.Speak);
            };
        }


        /// <summary>
        /// Asserts bot response of SendingRouteDetails
        /// </summary>
        /// <returns></returns>
        private Action<IActivity> SendingRouteDetails()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(ParseReplies(RouteResponses.SendingRouteDetails, new StringDictionary()), messageActivity.Text);
            };
        }

        /// <summary>
        /// Asserts bot response of CannotCancelActiveRoute
        /// </summary>
        /// <returns></returns>
        private Action<IActivity> CannotCancelActiveRoute()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(ParseReplies(CancelRouteResponses.CannotCancelActiveRoute, new StringDictionary()), messageActivity.Text);
            };
        }

        /// <summary>
        /// Asserts bot response of CancelActiveRoute
        /// </summary>
        /// <returns></returns>
        private Action<IActivity> CancelActiveRoute()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(ParseReplies(CancelRouteResponses.CancelActiveRoute, new StringDictionary()), messageActivity.Text);
            };
        }

        /// <summary>
        /// Asserts bot response of CompleteDialog
        /// </summary>
        /// <returns></returns>
        private Action<IActivity> CompleteDialog()
        {
            return activity =>
            {
                var endOfConversationActivity = activity.AsEndOfConversationActivity();
                Assert.AreEqual(endOfConversationActivity.Type, ActivityTypes.EndOfConversation);
            };
        }

        /// <summary>
        /// Asserts bot response of Event Activity
        /// </summary>
        /// <returns></returns>
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