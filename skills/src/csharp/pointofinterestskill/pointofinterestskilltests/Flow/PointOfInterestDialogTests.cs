﻿using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PointOfInterestSkill.Responses.CancelRoute;
using PointOfInterestSkill.Responses.Route;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkillTests.Flow.Utterances;

namespace PointOfInterestSkillTests.Flow
{
    [TestClass]
    public class PointOfInterestDialogTests : PointOfInterestTestBase
    {
        /// <summary>
        /// Find points of interest nearby.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task WhatsNearbyTest()
        {
            await GetTestFlow()
                .Send(PointOfInterestDialogUtterances.LocationEvent)
                .Send(PointOfInterestDialogUtterances.WhatsNearby)
                .AssertReply(MultipleLocationsFound())
                .Send(GeneralUtterances.OptionOne)
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
                .Send(PointOfInterestDialogUtterances.LocationEvent)
                .Send(PointOfInterestDialogUtterances.WhatsNearby)
                .AssertReply(MultipleLocationsFound())
                .Send(GeneralUtterances.OptionOne)
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
                .Send(PointOfInterestDialogUtterances.LocationEvent)
                .Send(PointOfInterestDialogUtterances.WhatsNearby)
                .AssertReply(MultipleLocationsFound())
                .Send(GeneralUtterances.OptionOne)
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
                .Send(PointOfInterestDialogUtterances.LocationEvent)
                .Send(PointOfInterestDialogUtterances.WhatsNearby)
                .AssertReply(MultipleLocationsFound())
                .Send(GeneralUtterances.OptionOne)
                .AssertReply(SingleRouteFound())
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
                .Send(PointOfInterestDialogUtterances.LocationEvent)
                .Send(PointOfInterestDialogUtterances.WhatsNearby)
                .AssertReply(MultipleLocationsFound())
                .Send(GeneralUtterances.OptionOne)
                .AssertReply(SingleRouteFound())
                .AssertReply(SendingRouteDetails())
                .AssertReply(CheckForEvent())
                .Send(PointOfInterestDialogUtterances.CancelRoute)
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
                .Send(PointOfInterestDialogUtterances.LocationEvent)
                .Send(PointOfInterestDialogUtterances.FindParkingNearby)
                .AssertReply(MultipleLocationsFound())
                .Send(GeneralUtterances.OptionOne)
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
                .Send(PointOfInterestDialogUtterances.LocationEvent)
                .Send(PointOfInterestDialogUtterances.FindParkingNearAddress)
                .AssertReply(MultipleLocationsFound())
                .Send(GeneralUtterances.OptionOne)
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
        /// Asserts bot response of PointOfInterestSelection.
        /// </summary>
        /// <returns>Returns an Action with IActivity object.</returns>
        private Action<IActivity> PointOfInterestSelection()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                int index = messageActivity.Text.IndexOf("\n");
                if (index > 0)
                {
                    messageActivity.Text = messageActivity.Text.Substring(0, index);
                }

                CollectionAssert.Contains(ParseReplies(POISharedResponses.PointOfInterestSelection, new StringDictionary()), messageActivity.Text);
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

                CollectionAssert.Contains(ParseReplies(POISharedResponses.SingleLocationFound, new StringDictionary()), messageActivity.Text);
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

                CollectionAssert.Contains(ParseReplies(POISharedResponses.MultipleRoutesFound, new StringDictionary()), messageActivity.Text);
            };
        }

        /// <summary>
        /// Asserts bot response of PromptToStartRoute.
        /// </summary>
        /// <returns>Returns an Action with IActivity object.</returns>
        private Action<IActivity> PromptToStartRoute()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(ParseReplies(RouteResponses.PromptToStartRoute, new StringDictionary()), messageActivity.Speak);
            };
        }

        /// <summary>
        /// Asserts bot response of AskAboutRouteLater.
        /// </summary>
        /// <returns>Returns an Action with IActivity object.</returns>
        private Action<IActivity> AskAboutRouteLater()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(ParseReplies(RouteResponses.AskAboutRouteLater, new StringDictionary()), messageActivity.Speak);
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
        /// Asserts bot response of CannotCancelActiveRoute.
        /// </summary>
        /// <returns>Returns an Action with IActivity object.</returns>
        private Action<IActivity> CannotCancelActiveRoute()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(ParseReplies(CancelRouteResponses.CannotCancelActiveRoute, new StringDictionary()), messageActivity.Text);
            };
        }

        /// <summary>
        /// Asserts bot response of CancelActiveRoute.
        /// </summary>
        /// <returns>Returns an Action with IActivity object.</returns>
        private Action<IActivity> CancelActiveRoute()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(ParseReplies(CancelRouteResponses.CancelActiveRoute, new StringDictionary()), messageActivity.Text);
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
                var endOfConversationActivity = activity.AsEndOfConversationActivity();
                Assert.AreEqual(endOfConversationActivity.Type, ActivityTypes.EndOfConversation);
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