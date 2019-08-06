using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Models.ServiceNow;
using RestSharp;

namespace ITSMSkill.Services.ServiceNow
{
    public class Management : IITServiceManagement
    {
        private static readonly string TicketResource = "table/incident";
        private static readonly string KnowledgeResource = "table/kb_knowledge";
        private static readonly Dictionary<UrgencyLevel, string> UrgencyToString;
        private static readonly Dictionary<string, UrgencyLevel> StringToUrgency;
        private static readonly Dictionary<TicketState, string> TicketStateToString;
        private static readonly Dictionary<string, TicketState> StringToTicketState;
        private readonly RestClient client;
        private readonly string token;
        private readonly int limitSize;

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

        public Management(string url, string token, int limitSize)
        {
            this.client = new RestClient($"{url}/api/now/v1/");
            this.token = token;
            this.limitSize = limitSize;
        }

        public async Task<CreateTicketResult> CreateTicket(string description, UrgencyLevel urgency)
        {
            var request = CreateRequest(TicketResource);
            var body = new CreateTicketRequest()
            {
                short_description = description,
                urgency = UrgencyToString[urgency]
            };
            request.AddJsonBody(body);
            try
            {
                var result = await client.PostAsync<CreateTicketResponse>(request);

                // didn't find way to get current user's id directly, so update again. or have to create a custom api like https://community.servicenow.com/community?id=community_question&sys_id=52efcb88db1ddb084816f3231f9619c7
                request = CreateRequest($"{TicketResource}/{result.result.sys_id}?sysparm_exclude_ref_link=true");
                var updateBody = new
                {
                    caller_id = result.result.opened_by.value
                };
                request.AddJsonBody(updateBody);
                result = await client.PatchAsync<CreateTicketResponse>(request);

                return new CreateTicketResult()
                {
                    Success = true,
                    Ticket = ConvertTicket(result.result)
                };
            }
            catch (Exception ex)
            {
                return new CreateTicketResult()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<SearchTicketResult> SearchTicket(string description = null, List<UrgencyLevel> urgencies = null, string id = null, List<TicketState> states = null)
        {
            var request = CreateRequest(TicketResource);
            var sysparmQuery = new List<string>();
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

            if (sysparmQuery.Count > 0)
            {
                request.AddParameter("sysparm_query", string.Join('^', sysparmQuery));
            }

            request.AddParameter("sysparm_limit", limitSize);

            try
            {
                var result = await client.GetAsync<SearchTicketResponse>(request);
                return new SearchTicketResult()
                {
                    Success = true,
                    Tickets = result.result?.Select(r => ConvertTicket(r)).ToArray()
                };
            }
            catch (Exception ex)
            {
                return new SearchTicketResult()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<SearchKnowledgeResult> SearchKnowledge(string query)
        {
            var request = CreateRequest(KnowledgeResource);

            // https://codecreative.io/blog/gliderecord-full-text-search-explained/
            request.AddParameter("sysparm_query", $"IR_AND_OR_QUERY={query}");

            request.AddParameter("sysparm_limit", limitSize);

            try
            {
                var result = await client.GetAsync<SearchKnowledgeResponse>(request);
                return new SearchKnowledgeResult()
                {
                    Success = true,
                    Knowledges = result.result?.Select(r => ConvertKnowledge(r)).ToArray()
                };
            }
            catch (Exception ex)
            {
                return new SearchKnowledgeResult()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private Ticket ConvertTicket(TicketResponse ticketResponse)
        {
            var ticket = new Ticket()
            {
                Id = ticketResponse.sys_id,
                Description = ticketResponse.short_description,
                Urgency = StringToUrgency[ticketResponse.urgency],
                State = StringToTicketState[ticketResponse.state],
                OpenedTime = DateTime.Parse(ticketResponse.opened_at)
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
                UpdatedTime = DateTime.Parse(knowledgeResponse.sys_updated_on)
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
    }
}
