// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using CalendarSkill.Models;
using Newtonsoft.Json;

namespace CalendarSkill.Services.AzureMapsAPI
{
    public class AzureMapsClient
    {
        private static HttpClient _httpClient;
        private string _apiKey;
        private string _byCoordinatesUrl = "https://atlas.microsoft.com/timezone/byCoordinates/json?subscription-key={0}&api-version=1.0&options=all&query={1}";

        public AzureMapsClient(BotSettings settings)
        {
            GetApiKey(settings);
            _httpClient = new HttpClient();
        }

        public async Task<TimeZoneInfo> GetTimeZoneInfoByCoordinates(string query)
        {
            try
            {
                var url = string.Format(_byCoordinatesUrl, _apiKey, query);
                var response = await _httpClient.GetStringAsync(url);
                var apiResponse = JsonConvert.DeserializeObject<TimeZoneResponse>(response);
                return TimeZoneInfo.FindSystemTimeZoneById(apiResponse.TimeZones[0].Names.Standard);
            }
            catch
            {
                return null;
            }
        }

        private void GetApiKey(BotSettings settings)
        {
            _apiKey = settings.AzureMapsKey ?? throw new Exception("Could not get the required AzureMapsKey API key. Please make sure your settings are correctly configured.");
        }
    }
}
