// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace UnitTests.APITests
{
    using System;
    using System.Collections.Generic;
    using CalendarSkill;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class GoogleCalendarTests
    {
        private static CalendarService googleService;
        private List<EventModel> eventModels;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            googleService = new CalendarService("test token", EventSource.Google, TimeZoneInfo.Local);
        }

        [TestInitialize]
        public void TestInit()
        {
            this.eventModels = new List<EventModel>();
            EventModel model = new EventModel(EventSource.Google);
            model.Title = "API Test";
            model.StartTime = DateTime.Parse("19:00");
            model.EndTime = DateTime.Parse("19:30");
            model.TimeZone = TimeZoneInfo.Local;
            EventModel stored_model = googleService.CreateEvent(model).Result;
            this.eventModels.Add(stored_model);
        }

        [TestCleanup]
        public void TestClean()
        {
            foreach (EventModel model in this.eventModels)
            {
                try
                {
                    googleService.DeleteEventById(model.Id).Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        [TestMethod]
        public void CreateGoogleCalendarTest()
        {
            EventModel model = new EventModel(EventSource.Google);
            model.Title = "Create Test";
            model.StartTime = DateTime.Parse("19:00");
            model.EndTime = DateTime.Parse("19:30");
            model.TimeZone = TimeZoneInfo.Local;
            EventModel stored_model = googleService.CreateEvent(model).Result;
            this.eventModels.Add(stored_model);
        }

        [TestMethod]
        public void UpdateGoogleCalendarTest()
        {
            EventModel origin = this.eventModels[0];
            EventModel model = new EventModel(origin.Source);
            model.StartTime = DateTime.Parse("20:00");
            model.EndTime = DateTime.Parse("20:30");
            model.TimeZone = origin.TimeZone;
            model.Id = origin.Id;
            EventModel stored_model = googleService.UpdateEventById(model).Result;
            this.eventModels[0] = stored_model;
            Assert.IsTrue(stored_model.StartTime.Equals(DateTime.Parse("20:00")));
            Assert.IsTrue(stored_model.EndTime.Equals(DateTime.Parse("20:30")));
        }

        [TestMethod]
        public void GetGoogleCalendarByStartTimeTest()
        {
            DateTime startTime = DateTime.Parse("19:00");

            List<EventModel> result_models = googleService.GetEventsByStartTime(startTime).Result;

            Assert.IsTrue(result_models.Count == 1);
            Assert.IsTrue(result_models[0].StartTime.Equals(DateTime.Parse("19:00")));
            Assert.IsTrue(result_models[0].EndTime.Equals(DateTime.Parse("19:30")));
            Assert.IsTrue(result_models[0].Title.Equals("API Test"));
        }

        [TestMethod]
        public void GetGoogleCalendarByTimeTest()
        {
            DateTime startTime = DateTime.Parse("19:00");
            DateTime endTime = DateTime.Parse("20:00");

            List<EventModel> result_models = googleService.GetEventsByTime(startTime, endTime).Result;

            Assert.IsTrue(result_models.Count >= 1);
        }

        [TestMethod]
        public void GetGoogleCalendarByTitle()
        {
            string title = "API Test";

            List<EventModel> result_models = googleService.GetEventsByTitle(title).Result;

            Assert.IsTrue(result_models.Count >= 1);
        }

        [TestMethod]
        public void DeleteGoogleCalendarTest()
        {
            googleService.DeleteEventById(this.eventModels[0].Id).Wait();

            this.eventModels.RemoveAt(0);
        }
    }
}
