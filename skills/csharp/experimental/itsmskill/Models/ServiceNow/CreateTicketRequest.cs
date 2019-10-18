// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ITSMSkill.Models.ServiceNow
{
    public class CreateTicketRequest
    {
        public string caller_id { get; set; }

        public string short_description { get; set; }

        public string description { get; set; }

        public string urgency { get; set; }
    }
}
