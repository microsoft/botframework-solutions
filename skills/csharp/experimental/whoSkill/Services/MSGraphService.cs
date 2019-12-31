using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WhoSkill.Models;

namespace WhoSkill.Services
{
    public class MSGraphService
    {
        private const string MSGraphBaseUrl = "https://graph.microsoft.com/v1.0/";

        private HttpClient httpClient;

        public void InitAsync(string token)
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<List<Person>> GetUsers(string keyword)
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
            return JsonConvert.DeserializeObject<List<Person>>(result);
        }

        private async Task<string> ExecuteGraphFetchAsync(string url)
        {
            var result = await httpClient.GetAsync(url);
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
    }
}
