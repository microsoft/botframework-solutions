using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WeatherSkill.Models;

namespace WeatherSkill.Services
{
    public sealed class AccuweatherService
    {
        private static HttpClient _httpClient;
        private string _searchLocationUrl = $"http://dataservice.accuweather.com/locations/v1/search?q={{0}}&apikey={{1}}";
        private string _oneDayForecastUrl = $"http://dataservice.accuweather.com/forecasts/v1/daily/1day/{{0}}?apikey={{1}}&details=true";
        private string _fiveDayForecastUrl = $"http://dataservice.accuweather.com/forecasts/v1/daily/5day/{{0}}?apikey={{1}}&details=true";
        private string _tenDayForecastUrl = $"http://dataservice.accuweather.com/forecasts/v1/daily/10day/{{0}}?apikey={{1}}&details=true";
        private string _twelveHourForecastUrl = $"http://dataservice.accuweather.com/forecasts/v1/hourly/12hour/{{0}}?apikey={{1}}&details=true";
        private string _apiKey;

        public AccuweatherService(BotSettings settings)
        {
            GetApiKey(settings);
            _httpClient = new HttpClient();
        }

        public async Task<Location> GetLocationByQueryAsync(string query)
        {
            return await GetLocationResponseAsync(string.Format(CultureInfo.InvariantCulture, _searchLocationUrl, query, _apiKey));
        }

        public async Task<ForecastResponse> GetOneDayForecastAsync(string query)
        {
            return await GetDailyForecastResponseAsync(string.Format(CultureInfo.InvariantCulture, _oneDayForecastUrl, query, _apiKey));
        }

        public async Task<ForecastResponse> GetFiveDayForecastAsync(string query)
        {
            return await GetDailyForecastResponseAsync(string.Format(CultureInfo.InvariantCulture, _fiveDayForecastUrl, query, _apiKey));
        }

        public async Task<ForecastResponse> GetTenDayForecastAsync(string query)
        {
            return await GetDailyForecastResponseAsync(string.Format(CultureInfo.InvariantCulture, _tenDayForecastUrl, query, _apiKey));
        }

        public async Task<HourlyForecast[]> GetTwelveHourForecastAsync(string query)
        {
            return await GetHourlyForecastResponseAsync(string.Format(CultureInfo.InvariantCulture, _twelveHourForecastUrl, query, _apiKey));
        }

        private void GetApiKey(BotSettings settings)
        {
            settings.Properties.TryGetValue("apiKey", out var key);

            _apiKey = key;
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new Exception("Could not get the required AccuWeather API key. Please make sure your settings are correctly configured.");
            }
        }

        private async Task<Location> GetLocationResponseAsync(string url)
        {
            var response = await _httpClient.GetStringAsync(url);

            var apiResponse = JsonConvert.DeserializeObject<Location[]>(response);

            return apiResponse[0];
        }

        private async Task<ForecastResponse> GetDailyForecastResponseAsync(string url)
        {
            var response = await _httpClient.GetStringAsync(url);

            var apiResponse = JsonConvert.DeserializeObject<ForecastResponse>(response);

            return apiResponse;
        }

        private async Task<HourlyForecast[]> GetHourlyForecastResponseAsync(string url)
        {
            var response = await _httpClient.GetStringAsync(url);

            var apiResponse = JsonConvert.DeserializeObject<HourlyForecast[]>(response);

            return apiResponse;
        }
    }
}
