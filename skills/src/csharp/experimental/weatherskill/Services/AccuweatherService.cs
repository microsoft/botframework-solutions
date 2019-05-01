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
        public string OneDayForecastUrl = $"http://dataservice.accuweather.com/forecasts/v1/daily/1day/{{0}}?apikey={{1}}&details=true";
        public string FiveDayForecastUrl = $"http://dataservice.accuweather.com/forecasts/v1/daily/5day/{{0}}?apikey={{1}}&details=true";
        public string TenDayForecastUrl = $"http://dataservice.accuweather.com/forecasts/v1/daily/10day/{{0}}?apikey={{1}}&details=true";
        public string TwelveHourForecastUrl = $"http://dataservice.accuweather.com/forecasts/v1/hourly/12hour/{{0}}?apikey={{1}}&details=true";

        private string ApiKey;
        private static HttpClient httpClient;

        public AccuweatherService(BotSettings settings)
        {
            GetApiKey(settings);
            httpClient = new HttpClient();
        }

        private void GetApiKey(BotSettings settings)
        {
            settings.Properties.TryGetValue("apiKey", out var key);

            ApiKey = key;
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                throw new Exception("Could not get the required AccuWeather API key. Please make sure your settings are correctly configured.");
            }
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
            return await GetDailyForecastResponseAsync(string.Format(CultureInfo.InvariantCulture, OneDayForecastUrl, query, ApiKey));
        }

        public async Task<ForecastResponse> GetFiveDayForecastAsync(string query)
        {
            return await GetDailyForecastResponseAsync(string.Format(CultureInfo.InvariantCulture, FiveDayForecastUrl, query, ApiKey));
        }

        public async Task<ForecastResponse> GetTenDayForecastAsync(string query)
        {
            return await GetDailyForecastResponseAsync(string.Format(CultureInfo.InvariantCulture, TenDayForecastUrl, query, ApiKey));
        }

        public async Task<HourlyForecast[]> GetTwelveHourForecastAsync(string query)
        {
            return await GetHourlyForecastResponseAsync(string.Format(CultureInfo.InvariantCulture, TwelveHourForecastUrl, query, ApiKey));
        }

        private async Task<ForecastResponse> GetDailyForecastResponseAsync(string url)
        {
            var response = await httpClient.GetStringAsync(url);

            var apiResponse = JsonConvert.DeserializeObject<ForecastResponse>(response);

            return apiResponse;
        }

        private async Task<HourlyForecast[]> GetHourlyForecastResponseAsync(string url)
        {
            var response = await httpClient.GetStringAsync(url);

            var apiResponse = JsonConvert.DeserializeObject<HourlyForecast[]>(response);

            return apiResponse;
        }
    }
}
