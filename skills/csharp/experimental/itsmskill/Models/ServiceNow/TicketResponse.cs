// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ITSMSkill.Models.ServiceNow
{
    public class TicketResponse
    {
        public UserInfo opened_by { get; set; }

        public string state { get; set; }

        public string opened_at { get; set; }

        public string short_description { get; set; }

        public string description { get; set; }

        public string close_code { get; set; }

        public string close_notes { get; set; }

        public string sys_id { get; set; }

        public string urgency { get; set; }

        public string number { get; set; }

        public class UserInfo
        {
            public string value { get; set; }
        }
    }
}
