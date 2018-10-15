// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace UnitTests.APITests
{
    using System;
    using System.Collections.Generic;
    using CalendarSkill;
    using Microsoft.Graph;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MSCalendarTests
    {
        private static CalendarService msService;
        private List<EventModel> eventModels;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            string token = "test token";
            msService = new CalendarService(token, EventSource.Microsoft, TimeZoneInfo.Local);
        }

        [TestInitialize]
        public void TestInit()
        {
            this.eventModels = new List<EventModel>();
            EventModel model = new EventModel(EventSource.Microsoft);
            model.Title = "API Test";
            model.StartTime = DateTime.Parse("19:00");
            model.EndTime = DateTime.Parse("19:30");
            model.TimeZone = TimeZoneInfo.Local;
            EventModel stored_model = msService.CreateEvent(model).Result;
            this.eventModels.Add(stored_model);
        }

        [TestCleanup]
        public void TestClean()
        {
            foreach (EventModel model in this.eventModels)
            {
                try
                {
                    msService.DeleteEventById(model.Id).Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        [TestMethod]
        public void CreateMSCalendarTest()
        {
            EventModel model = new EventModel(EventSource.Microsoft);
            model.Title = "Create Test";
            model.StartTime = DateTime.Parse("19:00");
            model.EndTime = DateTime.Parse("19:30");
            model.TimeZone = TimeZoneInfo.Local;
            EventModel stored_model = msService.CreateEvent(model).Result;
            this.eventModels.Add(stored_model);
        }

        [TestMethod]
        public void UpdateMSCalendarTest()
        {
            EventModel origin = this.eventModels[0];
            EventModel model = new EventModel(origin.Source);
            model.StartTime = DateTime.Parse("20:00");
            model.EndTime = DateTime.Parse("20:30");
            model.TimeZone = origin.TimeZone;
            model.Id = origin.Id;
            EventModel stored_model = msService.UpdateEventById(model).Result;
            this.eventModels[0] = stored_model;
            Assert.IsTrue(stored_model.StartTime.Equals(DateTime.Parse("20:00")));
            Assert.IsTrue(stored_model.EndTime.Equals(DateTime.Parse("20:30")));
        }

        [TestMethod]
        public void GetMSCalendarByStartTimeTest()
        {
            DateTime startTime = DateTime.Parse("19:00");

            List<EventModel> result_models = msService.GetEventsByStartTime(startTime).Result;

            Assert.IsTrue(result_models.Count == 1);
            Assert.IsTrue(result_models[0].StartTime.Equals(DateTime.Parse("19:00")));
            Assert.IsTrue(result_models[0].EndTime.Equals(DateTime.Parse("19:30")));
            Assert.IsTrue(result_models[0].Title.Equals("API Test"));
        }

        [TestMethod]
        public void GetMSCalendarByTimeTest()
        {
            DateTime startTime = DateTime.Parse("19:00");
            DateTime endTime = DateTime.Parse("21:00");

            List<EventModel> result_models = msService.GetEventsByTime(startTime, endTime).Result;

            Assert.IsTrue(result_models.Count >= 1);
        }

        [TestMethod]
        public void GetMSCalendarByTitle()
        {
            string title = "API Test";

            List<EventModel> result_models = msService.GetEventsByTitle(title).Result;

            Assert.IsTrue(result_models.Count >= 1);
        }

        [TestMethod]
        public void DeleteMSCalendarTest()
        {
            msService.DeleteEventById(this.eventModels[0].Id).Wait();

            this.eventModels.RemoveAt(0);
        }
    }
}
