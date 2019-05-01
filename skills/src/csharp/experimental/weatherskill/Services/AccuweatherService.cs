using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WeatherSkill.Models;

namespace WeatherSkill.Services
{
    public sealed class AccuweatherService
    {
        public string SearchLocationUrl = $"http://dataservice.accuweather.com/locations/v1/search?q={{0}}&apikey={{1}}";
        public string OneDayForecastUrl = $"http://dataservice.accuweather.com/forecasts/v1/daily/1day/{{0}}?apikey={{1}}";
        private string ApiKey = "11Lcu7hycS3B6SlyAgkrC9TkFgfaw1TI";
        private static HttpClient httpClient;

        public AccuweatherService()
        {
            httpClient = new HttpClient();
        }

        public async Task<Location> GetLocationByQueryAsync(string query)
        {
            return await GetLocationResponseAsync(string.Format(CultureInfo.InvariantCulture, SearchLocationUrl, query, ApiKey));
        }

        private async Task<Location> GetLocationResponseAsync(string url)
        {
            var response = await httpClient.GetStringAsync(url);

            var apiResponse = JsonConvert.DeserializeObject<Location[]>(response);

            return apiResponse[0];
        }

        public async Task<ForecastResponse> GetOneDayForecastAsync(string query)
        {
            return await GetForecastResponseAsync(string.Format(CultureInfo.InvariantCulture, OneDayForecastUrl, query, ApiKey));
        }

        private async Task<ForecastResponse> GetForecastResponseAsync(string url)
        {
            var response = await httpClient.GetStringAsync(url);

            var apiResponse = JsonConvert.DeserializeObject<ForecastResponse>(response);

            return apiResponse;
        }
    }
}
