using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using EventSkill.Models.Eventbrite;
using Newtonsoft.Json;

namespace EventSkill.Services
{
    public sealed class EventbriteService
    {
        private const string LocationAndDateApiUrl = "https://www.eventbriteapi.com/v3/events/search/?location.address={0}&location.within={1}&start_date.keyword={2}&token={3}";
        private static string _apiKey;
        private static HttpClient _httpClient;

        public EventbriteService(BotSettings settings)
        {
            _apiKey = settings.EventbriteApiKey ?? throw new Exception("The EventbriteKey must be provided to use this dialog.");
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<List<Event>> GetEventsAsync(string location)
        {
            var url = string.Format(LocationAndDateApiUrl, location, "10mi", "today", _apiKey);
            var response = await _httpClient.GetStringAsync(url);
            var apiResponse = JsonConvert.DeserializeObject<EventSearchResult>(response);

            return apiResponse.Events;
        }
    }
}
