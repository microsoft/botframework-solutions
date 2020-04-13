using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Models.ServiceNow
{
    public class ServiceNowIncidentDetails
    {
        public ServiceNowIncidentDetails(string title, string description, UrgencyLevel urgency)
        {
            Title = title;
            Description = description;
            Urgency = urgency;
        }

        public string Title { get; set; }

        public string Description { get; set; }

        public UrgencyLevel Urgency { get; set; }
    }
}
