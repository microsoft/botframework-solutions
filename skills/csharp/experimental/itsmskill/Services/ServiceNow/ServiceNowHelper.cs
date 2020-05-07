// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ITSMSkill.Models;
using ITSMSkill.Models.ServiceNow;
using Newtonsoft.Json;
using RestSharp;

namespace ITSMSkill.Services.ServiceNow
{
    public static class ServiceNowHelper
    {
        public static readonly Dictionary<UrgencyLevel, string> UrgencyToString = new Dictionary<UrgencyLevel, string>()
        {
            { UrgencyLevel.None, string.Empty},
            { UrgencyLevel.Low, "3" },
            { UrgencyLevel.Medium, "2" },
            { UrgencyLevel.High, "1" }
        };

        public static readonly Dictionary<string, UrgencyLevel> StringToUrgency = new Dictionary<string, UrgencyLevel>(UrgencyToString.Select(pair => KeyValuePair.Create(pair.Value, pair.Key)));
        public static readonly Dictionary<TicketState, string> TicketStateToString = new Dictionary<TicketState, string>()
        {
            { TicketState.None, string.Empty},
            { TicketState.New, "1" },
            { TicketState.InProgress, "2" },
            { TicketState.OnHold, "3" },
            { TicketState.Resolved, "6" },
            { TicketState.Closed, "7" },
            { TicketState.Canceled, "8" }
        };

        public static readonly Dictionary<string, TicketState> StringToTicketState = new Dictionary<string, TicketState>(TicketStateToString.Select(pair => KeyValuePair.Create(pair.Value, pair.Key)));
        public static readonly string Provider = "ServiceNow";

        public static TicketsResult ProcessIRestResponse(this IRestResponse response)
        {
            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
            {
                var result = JsonConvert.DeserializeObject<SingleTicketResponse>(response.Content);
                return new TicketsResult()
                {
                    Success = true,
                    Tickets = new Ticket[] { ConvertTicket(result.result) }
                };
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return new TicketsResult
                {
                    Success = false,
                    Reason = "Unauthorized"
                };
            }
            else
            {
                return new TicketsResult()
                {
                    Success = false,
                    Reason = "Unknown"
                };
            }
        }

        public static Ticket ConvertTicket(TicketResponse ticketResponse)
        {
            var ticket = new Ticket()
            {
                Id = ticketResponse.sys_id,
                Title = ticketResponse.short_description,
                Description = ticketResponse.description,
                Urgency = StringToUrgency[ticketResponse.urgency],
                State = StringToTicketState[ticketResponse.state],
                OpenedTime = DateTime.Parse(ticketResponse.opened_at),
                Number = ticketResponse.number,
                Provider = Provider,
            };

            if (!string.IsNullOrEmpty(ticketResponse.close_code))
            {
                if (!string.IsNullOrEmpty(ticketResponse.close_notes))
                {
                    ticket.ResolvedReason = $"{ticketResponse.close_code}:\r\n{ticketResponse.close_notes}";
                }
                else
                {
                    ticket.ResolvedReason = ticketResponse.close_code;
                }
            }
            else
            {
                ticket.ResolvedReason = ticketResponse.close_notes;
            }

            return ticket;
        }
    }
}
