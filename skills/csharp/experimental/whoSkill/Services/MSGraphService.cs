using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WhoSkill.Models;

namespace WhoSkill.Services
{
    public class MSGraphService
    {
        private const string MSGraphBaseUrl = "https://graph.microsoft.com/v1.0/";

        private HttpClient _httpClient;

        private IGraphServiceClient _graphClient;

        public void InitServices(string token)
        {
            InitHttpClient(token);
            InitGraphServiceClient(token);
        }

        public async Task<List<Candidate>> GetMeetingContacts(string keyword)
        {
            var selectItem = "$select=attendees,organizer";
            var filter = string.Format("$filter=contains(subject,'{0}')", keyword);
            var orderOption = "$orderBy=createdDateTime desc";
            var resultMaxNumber = "$top=15";
            var url = MSGraphBaseUrl + "me/events"
                + "?"
                + selectItem
                + "&" + filter
                + "&" + orderOption
                + "&" + resultMaxNumber;

            var result = await ExecuteGraphFetchAsync(url);
            var searchResults = JsonConvert.DeserializeObject<List<EventSearchResult>>(result);

            var candidates = new List<Candidate>();
            foreach (var searchResult in searchResults)
            {
                await AddSearchResult(candidates, searchResult.Organizer.EmailAddress);
                foreach (var attendee in searchResult.Attendees)
                {
                    await AddSearchResult(candidates, attendee.EmailAddress);
                }
            }

            return candidates;
        }

        public async Task<List<Candidate>> GetEmailContacts(string keyword)
        {
            var selectItem = "$select=sender,toRecipients,ccRecipients";
            var searchOption = "search="
                + "\""
                + string.Format("(body: '{0}' OR subject: '{0}')", keyword)
                + string.Format(" AND (received >= {0} AND received <= {1})", DateTime.Now.AddYears(-1).ToShortDateString(), DateTime.Now.ToShortDateString())
                + "\"";
            var resultMaxNumber = "$top=15";
            var url = MSGraphBaseUrl + "me/messages"
                + "?"
                + searchOption
                + "&" + selectItem
                + "&" + resultMaxNumber;

            var result = await ExecuteGraphFetchAsync(url);
            var searchResults = JsonConvert.DeserializeObject<List<EmailSearchResult>>(result);

            var candidates = new List<Candidate>();
            foreach (var searchResult in searchResults)
            {
                await AddSearchResult(candidates, searchResult.Sender.EmailAddress);
                foreach (var toRecepient in searchResult.ToRecipients)
                {
                    await AddSearchResult(candidates, toRecepient.EmailAddress);
                }

                foreach (var ccRecepient in searchResult.CcRecipients)
                {
                    await AddSearchResult(candidates, ccRecepient.EmailAddress);
                }
            }

            return candidates;
        }

        public async Task<List<Candidate>> GetUsers(string keyword)
        {
            var selectItem = "$select=userType,displayName,mail,jobTitle,userPrincipalName,id,officeLocation,mobilePhone";
            var filter = "$filter="
                + string.Format("(startswith(displayName,'{0}') or startswith(givenName,'{0}') or startswith(surname,'{0}') or startswith(mail,'{0}') or startswith(userPrincipalName,'{0}'))", keyword);
            var resultMaxNumber = "$top=15";
            var url = MSGraphBaseUrl + "users"
                + "?"
                + selectItem
                + "&" + filter
                + "&" + resultMaxNumber;

            var result = await ExecuteGraphFetchAsync(url);
            return JsonConvert.DeserializeObject<List<Candidate>>(result, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        public async Task<Candidate> GetManager(string id)
        {
            var request = _graphClient.Users[id].Manager.Request();
            try
            {
                var result = await request.GetAsync();
                var managerUser = result as User;
                return new Candidate(managerUser);
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<Candidate>> GetDirectReports(string id)
        {
            var request = _graphClient.Users[id].DirectReports.Request();
            try
            {
                var result = await request.GetAsync();
                return GetDirectReportsFromResponse(result);
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> GetUserPhotoUrlAsyc(string id)
        {
            var photoRequest = _graphClient.Users[id].Photos["64x64"].Content.Request();
            Stream originalPhoto = null;
            string photoUrl = string.Empty;
            try
            {
                originalPhoto = await photoRequest.GetAsync();
                photoUrl = Convert.ToBase64String(ReadFully(originalPhoto));
                return string.Format("data:image/jpeg;base64,{0}", photoUrl);
            }
            catch
            {
                return null;
            }
        }

        private void InitHttpClient(string token)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private async Task AddSearchResult(List<Candidate> candidates, Models.EmailAddress emailAddress)
        {
            if (candidates.Where(x => x.Mail == emailAddress.Address).Count() == 0)
            {
                var users = await GetUsers(emailAddress.Address);
                if (users != null && users.Any())
                {
                    candidates.Add(users[0]);
                }
            }
        }

        private void InitGraphServiceClient(string token)
        {
            _graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);

                        // Get event times in the current time zone.
                        requestMessage.Headers.Add("Prefer", "outlook.timezone=\"" + TimeZoneInfo.Utc.Id + "\"");
                        await Task.CompletedTask;
                    }));
        }

        private async Task<string> ExecuteGraphFetchAsync(string url)
        {
            var result = await _httpClient.GetAsync(url);
            dynamic responseContent = JObject.Parse(await result.Content.ReadAsStringAsync());
            if (result.IsSuccessStatusCode)
            {
                return JsonConvert.SerializeObject((object)responseContent.value);
            }
            else
            {
                throw new Exception(responseContent.error.message.ToString());
            }
        }

        private byte[] ReadFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Gets the person types from direct reports collection type.
        /// </summary>
        /// <param name="directReportsCollectionPage">The direct reports collection page.</param>
        /// <returns>A list of person types.</returns>
        private List<Candidate> GetDirectReportsFromResponse(IUserDirectReportsCollectionWithReferencesPage directReportsCollectionPage)
        {
            var candidates = new List<Candidate>();
            if (directReportsCollectionPage != null && directReportsCollectionPage.Any())
            {
                foreach (User user in directReportsCollectionPage)
                {
                    Candidate personEntity = new Candidate(user);
                    candidates.Add(personEntity);
                }
            }

            return candidates;
        }
    }
}
