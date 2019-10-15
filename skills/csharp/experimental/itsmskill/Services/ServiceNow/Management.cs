// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Models.ServiceNow;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers;

namespace ITSMSkill.Services.ServiceNow
{
    public class Management : IITServiceManagement
    {
        private static readonly string Provider = "ServiceNow";
        private static readonly string TicketResource = "now/v1/table/incident";
        private static readonly string TicketCount = "now/v1/stats/incident";
        private static readonly string KnowledgeResource = "now/v1/table/kb_knowledge";
        private static readonly string KnowledgeCount = "now/v1/stats/kb_knowledge";
        private static readonly Dictionary<UrgencyLevel, string> UrgencyToString;
        private static readonly Dictionary<string, UrgencyLevel> StringToUrgency;
        private static readonly Dictionary<TicketState, string> TicketStateToString;
        private static readonly Dictionary<string, TicketState> StringToTicketState;
        private readonly RestClient client;
        private readonly string getUserIdResource;
        private readonly string token;
        private readonly int limitSize;
        private readonly string knowledgeUrl;

        static Management()
        {
            UrgencyToString = new Dictionary<UrgencyLevel, string>()
            {
                { UrgencyLevel.None, string.Empty },
                { UrgencyLevel.Low, "3" },
                { UrgencyLevel.Medium, "2" },
                { UrgencyLevel.High, "1" }
            };
            StringToUrgency = new Dictionary<string, UrgencyLevel>(UrgencyToString.Select(pair => KeyValuePair.Create(pair.Value, pair.Key)));
            TicketStateToString = new Dictionary<TicketState, string>()
            {
                { TicketState.None, string.Empty },
                { TicketState.New, "1" },
                { TicketState.InProgress, "2" },
                { TicketState.OnHold, "3" },
                { TicketState.Resolved, "6" },
                { TicketState.Closed, "7" },
                { TicketState.Canceled, "8" }
            };
            StringToTicketState = new Dictionary<string, TicketState>(TicketStateToString.Select(pair => KeyValuePair.Create(pair.Value, pair.Key)));
        }

        public Management(string url, string token, int limitSize, string getUserIdResource)
        {
            this.client = new RestClient($"{url}/api/");
            this.getUserIdResource = getUserIdResource;
            this.token = token;
            this.limitSize = limitSize;
            this.knowledgeUrl = $"{url}/kb_view.do?sysparm_article={{0}}";
        }

        public async Task<TicketsResult> CreateTicket(string description, UrgencyLevel urgency)
        {
            try
            {
                var request = CreateRequest(getUserIdResource);
                var userId = await client.GetAsync<GetUserIdResponse>(request);

                request = CreateRequest(TicketResource);
                var body = new CreateTicketRequest()
                {
                    caller_id = userId.result,
                    short_description = description,
                    urgency = UrgencyToString[urgency]
                };
                request.AddJsonBody(body);
                var result = await client.PostAsync<SingleTicketResponse>(request);

                return new TicketsResult()
                {
                    Success = true,
                    Tickets = new Ticket[] { ConvertTicket(result.result) }
                };
            }
            catch (Exception ex)
            {
                return new TicketsResult()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<TicketsResult> SearchTicket(int pageIndex, string description = null, List<UrgencyLevel> urgencies = null, string id = null, List<TicketState> states = null, string number = null)
        {
            try
            {
                var request = CreateRequest(TicketResource);

                var sysparmQuery = await CreateTicketSearchQuery(description: description, urgencies: urgencies, id: id, states: states, number: number);

                request.AddParameter("sysparm_query", string.Join('^', sysparmQuery));

                request.AddParameter("sysparm_limit", limitSize);

                request.AddParameter("sysparm_offset", limitSize * pageIndex);

                var result = await client.GetAsync<MultiTicketsResponse>(request);
                return new TicketsResult()
                {
                    Success = true,
                    Tickets = result.result?.Select(r => ConvertTicket(r)).ToArray()
                };
            }
            catch (Exception ex)
            {
                return new TicketsResult()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<TicketsResult> CountTicket(string description = null, List<UrgencyLevel> urgencies = null, string id = null, List<TicketState> states = null, string number = null)
        {
            try
            {
                var request = CreateRequest(TicketCount);

                var sysparmQuery = await CreateTicketSearchQuery(description: description, urgencies: urgencies, id: id, states: states, number: number);

                request.AddParameter("sysparm_query", string.Join('^', sysparmQuery));

                request.AddParameter("sysparm_count", true);

                var result = await client.GetAsync<SingleAggregateResponse>(request);
                return new TicketsResult()
                {
                    Success = true,
                    Tickets = new Ticket[result.result.stats.count]
                };
            }
            catch (Exception ex)
            {
                return new TicketsResult()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<TicketsResult> UpdateTicket(string id, string description = null, UrgencyLevel urgency = UrgencyLevel.None)
        {
            var request = CreateRequest($"{TicketResource}/{id}?sysparm_exclude_ref_link=true");
            var body = new CreateTicketRequest()
            {
                short_description = description,
                urgency = urgency == UrgencyLevel.None ? null : UrgencyToString[urgency]
            };
            request.JsonSerializer = new JsonNoNull();
            request.AddJsonBody(body);
            try
            {
                var result = await client.PatchAsync<SingleTicketResponse>(request);

                return new TicketsResult()
                {
                    Success = true,
                    Tickets = new Ticket[] { ConvertTicket(result.result) }
                };
            }
            catch (Exception ex)
            {
                return new TicketsResult()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<TicketsResult> CloseTicket(string id, string reason)
        {
            try
            {
                // minimum field required: https://community.servicenow.com/community?id=community_question&sys_id=84ceb6a5db58dbc01dcaf3231f9619e9
                var request = CreateRequest(getUserIdResource);
                var userId = await client.GetAsync<GetUserIdResponse>(request);

                request = CreateRequest($"{TicketResource}/{id}?sysparm_exclude_ref_link=true");
                var body = new
                {
                    close_code = "Closed/Resolved by Caller",
                    state = "7",
                    caller_id = userId.result,
                    close_notes = reason
                };
                request.JsonSerializer = new JsonNoNull();
                request.AddJsonBody(body);

                var result = await client.PatchAsync<SingleTicketResponse>(request);

                return new TicketsResult()
                {
                    Success = true,
                    Tickets = new Ticket[] { ConvertTicket(result.result) }
                };
            }
            catch (Exception ex)
            {
                return new TicketsResult()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<KnowledgesResult> SearchKnowledge(string query, int pageIndex)
        {
            try
            {
                var request = CreateRequest(KnowledgeResource);

                var sysparmQuery = await CreateKnowledgeSearchQuery(query: query);

                request.AddParameter("sysparm_query", string.Join('^', sysparmQuery));

                request.AddParameter("sysparm_limit", limitSize);

                request.AddParameter("sysparm_offset", limitSize * pageIndex);

                var result = await client.GetAsync<MultiKnowledgesResponse>(request);
                return new KnowledgesResult()
                {
                    Success = true,
                    Knowledges = result.result?.Select(r => ConvertKnowledge(r)).ToArray()
                };
            }
            catch (Exception ex)
            {
                return new KnowledgesResult()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<KnowledgesResult> CountKnowledge(string query)
        {
            try
            {
                var request = CreateRequest(KnowledgeCount);

                var sysparmQuery = await CreateKnowledgeSearchQuery(query: query);

                request.AddParameter("sysparm_query", string.Join('^', sysparmQuery));

                request.AddParameter("sysparm_count", true);

                var result = await client.GetAsync<SingleAggregateResponse>(request);
                return new KnowledgesResult()
                {
                    Success = true,
                    Knowledges = new Knowledge[result.result.stats.count]
                };
            }
            catch (Exception ex)
            {
                return new KnowledgesResult()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<List<string>> CreateTicketSearchQuery(string description, List<UrgencyLevel> urgencies, string id, List<TicketState> states, string number)
        {
            var request = CreateRequest(getUserIdResource);
            var userId = await client.GetAsync<GetUserIdResponse>(request);

            var sysparmQuery = new List<string>
            {
                $"caller_id={userId.result}"
            };

            if (!string.IsNullOrEmpty(description))
            {
                sysparmQuery.Add($"short_descriptionLIKE{description}");
            }

            if (urgencies != null && urgencies.Count > 0)
            {
                sysparmQuery.Add($"urgencyIN{string.Join(',', urgencies.Select(urgency => UrgencyToString[urgency]))}");
            }

            if (!string.IsNullOrEmpty(id))
            {
                sysparmQuery.Add($"sys_id={id}");
            }

            if (states != null && states.Count > 0)
            {
                sysparmQuery.Add($"stateIN{string.Join(',', states.Select(state => TicketStateToString[state]))}");
            }

            if (!string.IsNullOrEmpty(number))
            {
                sysparmQuery.Add($"number={number}");
            }

            return sysparmQuery;
        }

        private async Task<List<string>> CreateKnowledgeSearchQuery(string query)
        {
            var sysparmQuery = new List<string>
            {
                // https://codecreative.io/blog/gliderecord-full-text-search-explained/
                $"IR_AND_OR_QUERY={query}"
            };

            return sysparmQuery;
        }

        private Ticket ConvertTicket(TicketResponse ticketResponse)
        {
            var ticket = new Ticket()
            {
                Id = ticketResponse.sys_id,
                Description = ticketResponse.short_description,
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
                    ticket.ResolvedReason = $"{ticketResponse.close_code}:\n{ticketResponse.close_notes}";
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

        private Knowledge ConvertKnowledge(KnowledgeResponse knowledgeResponse)
        {
            var knowledge = new Knowledge()
            {
                Id = knowledgeResponse.sys_id,
                Title = knowledgeResponse.short_description,
                UpdatedTime = DateTime.Parse(knowledgeResponse.sys_updated_on),
                Number = knowledgeResponse.number,
                Url = string.Format(knowledgeUrl, knowledgeResponse.number),
                Provider = Provider,
            };
            if (!string.IsNullOrEmpty(knowledgeResponse.text))
            {
                // TODO temporary solution
                Regex reg = new Regex("<[^>]+>", RegexOptions.IgnoreCase);
                knowledge.Content = reg.Replace(knowledgeResponse.text, string.Empty);
            }
            else
            {
                knowledge.Content = knowledgeResponse.wiki;
            }

            return knowledge;
        }

        private RestRequest CreateRequest(string resource)
        {
            var request = new RestRequest(resource);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", $"Bearer {token}");
            return request;
        }

        private class JsonNoNull : ISerializer
        {
            public JsonNoNull()
            {
                ContentType = "application/json";
            }

            public string ContentType { get; set; }

            public string Serialize(object obj)
            {
                return JsonConvert.SerializeObject(obj, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            }
        }
    }
}
