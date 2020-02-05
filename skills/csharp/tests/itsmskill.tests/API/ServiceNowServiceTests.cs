// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Services.ServiceNow;
using ITSMSkill.Tests.API.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;

namespace ITSMSkill.Tests.API
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class ServiceNowServiceTests
    {
        private MockServiceNowRestClient mockServiceNowRestClient;
        private IRestClient mockClient;

        [TestInitialize]
        public void Initialize()
        {
            mockServiceNowRestClient = new MockServiceNowRestClient();
            mockClient = mockServiceNowRestClient.MockRestClient;
        }

        [TestMethod]
        public async Task CreateTicketTest()
        {
            var service = new Management(MockData.ServiceNowUrl, MockData.Token, MockData.LimitSize, MockData.ServiceNowGetUserId, null, mockClient);

            var result = await service.CreateTicket(MockData.CreateTicketTitle, MockData.CreateTicketDescription, MockData.CreateTicketUrgencyLevel);

            Assert.AreEqual(result.Success, true);
            Assert.AreEqual(result.Tickets.Length, 1);
            Assert.AreEqual(result.Tickets[0].Title, MockData.CreateTicketTitle);
            Assert.AreEqual(result.Tickets[0].Description, MockData.CreateTicketDescription);
            Assert.AreEqual(result.Tickets[0].Urgency, MockData.CreateTicketUrgencyLevel);
            Assert.AreEqual(result.Tickets[0].State, MockData.CreateTicketTicketState);
            Assert.AreEqual(result.Tickets[0].OpenedTime, DateTime.Parse(MockData.CreateTicketOpenedTime));
            Assert.AreEqual(result.Tickets[0].Number, MockData.CreateTicketNumber);
            Assert.AreEqual(result.Tickets[0].Provider, MockData.ServiceNowProvider);
        }

        [TestMethod]
        public async Task SearchTicketTest()
        {
            var service = new Management(MockData.ServiceNowUrl, MockData.Token, MockData.LimitSize, MockData.ServiceNowGetUserId, null, mockClient);

            var result = await service.SearchTicket(0);

            Assert.AreEqual(result.Success, true);
            Assert.AreEqual(result.Tickets.Length, MockData.TicketCount);
            Assert.AreEqual(result.Tickets[0].Title, MockData.CreateTicketTitle);
            Assert.AreEqual(result.Tickets[0].Description, MockData.CreateTicketDescription);
            Assert.AreEqual(result.Tickets[0].Urgency, MockData.CreateTicketUrgencyLevel);
            Assert.AreEqual(result.Tickets[0].State, MockData.CreateTicketTicketState);
            Assert.AreEqual(result.Tickets[0].OpenedTime, DateTime.Parse(MockData.CreateTicketOpenedTime));
            Assert.AreEqual(result.Tickets[0].Number, MockData.CreateTicketNumber);
            Assert.AreEqual(result.Tickets[0].Provider, MockData.ServiceNowProvider);
        }

        [TestMethod]
        public async Task CountTicketTest()
        {
            var service = new Management(MockData.ServiceNowUrl, MockData.Token, MockData.LimitSize, MockData.ServiceNowGetUserId, null, mockClient);

            var result = await service.CountTicket();

            Assert.AreEqual(result.Success, true);
            Assert.AreEqual(result.Tickets.Length, MockData.TicketCount);
        }

        [TestMethod]
        public async Task UpdateTicketTest()
        {
            var service = new Management(MockData.ServiceNowUrl, MockData.Token, MockData.LimitSize, MockData.ServiceNowGetUserId, null, mockClient);

            var result = await service.UpdateTicket(MockData.CreateTicketId);

            Assert.AreEqual(result.Success, true);
            Assert.AreEqual(result.Tickets.Length, 1);
            Assert.AreEqual(result.Tickets[0].Title, MockData.CreateTicketTitle);
            Assert.AreEqual(result.Tickets[0].Description, MockData.CreateTicketDescription);
            Assert.AreEqual(result.Tickets[0].Urgency, MockData.CreateTicketUrgencyLevel);
            Assert.AreEqual(result.Tickets[0].State, MockData.CreateTicketTicketState);
            Assert.AreEqual(result.Tickets[0].OpenedTime, DateTime.Parse(MockData.CreateTicketOpenedTime));
            Assert.AreEqual(result.Tickets[0].Number, MockData.CreateTicketNumber);
            Assert.AreEqual(result.Tickets[0].Provider, MockData.ServiceNowProvider);
        }

        [TestMethod]
        public async Task CloseTicketTest()
        {
            var service = new Management(MockData.ServiceNowUrl, MockData.Token, MockData.LimitSize, MockData.ServiceNowGetUserId, null, mockClient);

            var result = await service.CloseTicket(MockData.CloseTicketId, MockData.CloseTicketReason);

            Assert.AreEqual(result.Success, true);
            Assert.AreEqual(result.Tickets.Length, 1);
            Assert.AreEqual(result.Tickets[0].Title, MockData.CreateTicketTitle);
            Assert.AreEqual(result.Tickets[0].Description, MockData.CreateTicketDescription);
            Assert.AreEqual(result.Tickets[0].Urgency, MockData.CreateTicketUrgencyLevel);
            Assert.AreEqual(result.Tickets[0].State, TicketState.Closed);
            Assert.AreEqual(result.Tickets[0].OpenedTime, DateTime.Parse(MockData.CreateTicketOpenedTime));
            Assert.AreEqual(result.Tickets[0].Number, MockData.CloseTicketNumber);
            Assert.AreEqual(result.Tickets[0].ResolvedReason, $"{MockData.CloseTicketCloseCode}:\r\n{MockData.CloseTicketReason}");
            Assert.AreEqual(result.Tickets[0].Provider, MockData.ServiceNowProvider);
        }

        [TestMethod]
        public async Task SearchKnowledgeTest()
        {
            var service = new Management(MockData.ServiceNowUrl, MockData.Token, MockData.LimitSize, MockData.ServiceNowGetUserId, null, mockClient);

            var result = await service.SearchKnowledge(MockData.KnowledgeTitle, 0);

            Assert.AreEqual(result.Success, true);
            Assert.AreEqual(result.Knowledges.Length, 1);
            Assert.AreEqual(result.Knowledges[0].Title, MockData.KnowledgeTitle);
            Assert.AreEqual(result.Knowledges[0].UpdatedTime, DateTime.Parse(MockData.KnowledgeUpdatedTime));
            Assert.AreEqual(result.Knowledges[0].Content, MockData.KnowledgeContent);
            Assert.AreEqual(result.Knowledges[0].Number, MockData.KnowledgeNumber);
            Assert.AreEqual(result.Knowledges[0].Url, MockData.KnowledgeUrl);
            Assert.AreEqual(result.Knowledges[0].Provider, MockData.ServiceNowProvider);
        }

        [TestMethod]
        public async Task CountKnowledgeTest()
        {
            var service = new Management(MockData.ServiceNowUrl, MockData.Token, MockData.LimitSize, MockData.ServiceNowGetUserId, null, mockClient);

            var result = await service.CountKnowledge(MockData.KnowledgeTitle);

            Assert.AreEqual(result.Success, true);
            Assert.AreEqual(result.Knowledges.Length, MockData.KnowledgeCount);
        }

        [TestMethod]
        public async Task ServiceHasCacheTest()
        {
            var cache = new ServiceCache();

            var service = new Management(MockData.ServiceNowUrl, MockData.Token, MockData.LimitSize, MockData.ServiceNowGetUserId, cache, mockClient);
            var result = await service.CountKnowledge(MockData.KnowledgeTitle);
            Assert.AreEqual(result.Success, true);
            Assert.AreEqual(result.Knowledges.Length, MockData.KnowledgeCount);

            Assert.AreEqual(mockServiceNowRestClient.GetUserIdResponseCount, 1);

            service = new Management(MockData.ServiceNowUrl, MockData.Token, MockData.LimitSize, MockData.ServiceNowGetUserId, cache, mockClient);
            result = await service.CountKnowledge(MockData.KnowledgeTitle);
            Assert.AreEqual(result.Success, true);
            Assert.AreEqual(result.Knowledges.Length, MockData.KnowledgeCount);

            Assert.AreEqual(mockServiceNowRestClient.GetUserIdResponseCount, 1);
        }
    }
}
