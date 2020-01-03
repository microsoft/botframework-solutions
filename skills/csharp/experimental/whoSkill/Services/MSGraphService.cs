using System;
using System.Collections.Generic;
using System.IO;
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

        public async Task<List<Candidate>> GetUsers(string keyword)
        {
            var selectItem = "$select=userType,displayName,mail,jobTitle,userPrincipalName,id,officeLocation,mobilePhone";
            var filter = "$filter="
                + string.Format("(startswith(displayName,'{0}') or startswith(givenName,'{0}') or startswith(surname,'{0}') or startswith(mail,'{0}') or startswith(userPrincipalName,'{0}'))", keyword);
            var resultMaxNumber = "$top=25";
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

        public async Task<string> GetMSUserPhotoUrlAsyc(string id)
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
    }
}
