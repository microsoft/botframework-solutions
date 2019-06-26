using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NewsSkill.Models;
using Newtonsoft.Json;

namespace NewsSkill.Services
{
    public sealed class AzureMapsService
    {
        private const string FuzzyQueryApiUrl = "https://atlas.microsoft.com/search/fuzzy/json?api-version=1.0&query={0}";
        private static string apiKey;
        private static HttpClient httpClient = new HttpClient();

        public void InitKeyAsync(string key)
        {
            // set user's api key
            apiKey = key;
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> GetCountryCodeAsync(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException(nameof(query));
            }

            // add query and key to api url
            var url = string.Format(FuzzyQueryApiUrl, query);
            url = string.Concat(url, $"&subscription-key={apiKey}");

            var response = await httpClient.GetStringAsync(url);
            var apiResponse = JsonConvert.DeserializeObject<SearchResultSet>(response);

            // check there is a valid response
            if (apiResponse != null && apiResponse.Results.Count > 0)
            {
                return apiResponse.Results[0]?.Address?.CountryCode;
            }

            return null;
        }
    }
}
