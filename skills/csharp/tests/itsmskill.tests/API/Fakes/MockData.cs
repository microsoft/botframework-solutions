// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ITSMSkill.Models;

namespace ITSMSkill.Tests.API.Fakes
{
    public static class MockData
    {
        public const string ServiceNowProvider = "ServiceNow";

        public const string ServiceNowUrl = "MockServiceNowUrl";

        public const string Token = "MockToken";

        public const int LimitSize = 1;

        public const string ServiceNowGetUserId = "MockServiceNowGetUserId";

        public const int TicketCount = 1;

        public const string CreateTicketTitle = "MockCreateTicketTitle";

        public const string CreateTicketDescription = "MockCreateTicketDescription";

        public const string CreateTicketUrgency = "3";

        public const UrgencyLevel CreateTicketUrgencyLevel = UrgencyLevel.Low;

        public const string CreateTicketState = "1";

        public const TicketState CreateTicketTicketState = TicketState.New;

        public const string CreateTicketOpenedTime = "2016-12-12 12:12:12";

        public const string CreateTicketNumber = "INC0000001";

        public const string CreateTicketId = "MockCreateTicketId";

        public const string CloseTicketNumber = "INC0000002";

        public const string CloseTicketId = "MockCloseTicketId";

        public const string CloseTicketReason = "MockCloseTicketReason";

        public const string CloseTicketState = "7";

        public const string CloseTicketCloseCode = "Closed/Resolved by Caller";

        public const string KnowledgeTitle = "MockKnowledgeTitle";

        public const string KnowledgeUpdatedTime = "2016-12-12 12:12:12";

        public const string KnowledgeNumber = "MockKnowledgeNumber";

        public const string KnowledgeContent = "MockKnowledgeContent";

        public const int KnowledgeCount = 1;

        public static string KnowledgeUrl { get => $"{ServiceNowUrl}/kb_view.do?sysparm_article={KnowledgeNumber}"; }
    }
}
