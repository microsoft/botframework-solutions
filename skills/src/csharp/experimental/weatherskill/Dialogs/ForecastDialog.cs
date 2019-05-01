using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using WeatherSkill.Models;
using WeatherSkill.Responses.Sample;
using WeatherSkill.Services;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using WeatherSkill.Responses.Shared;
using System.Globalization;
using System;
using Luis;
using System.Collections.Generic;

namespace WeatherSkill.Dialogs
{
    public class ForecastDialog : SkillDialogBase
    {
        private BotServices _services;
        private IStatePropertyAccessor<SkillState> _stateAccessor;

        public ForecastDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ForecastDialog), settings, services, responseManager, conversationState, telemetryClient)
        {
            _stateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));
            _services = services;
            Settings = settings;

            var fullForecastDialogWithPrompt = new WaterfallStep[]
            {
                RouteToGeographyPromptOrForecastResponse,
                GeographyPrompt,
                GetWeatherResponse,
                End,
            };

            var getForecastResponseDialog = new WaterfallStep[]
            {
                GetWeatherResponse,
                End,
            };
            
            AddDialog(new WaterfallDialog(nameof(ForecastDialog), fullForecastDialogWithPrompt));
            AddDialog(new WaterfallDialog(DialogIds.GetForecastResponseDialog, getForecastResponseDialog));
            AddDialog(new TextPrompt(DialogIds.GeographyPrompt, GeographyLuisValidatorAsync));

            InitialDialogId = nameof(ForecastDialog);
        }

        /// <summary>
        /// Check if geography is stored in state and route to prompt or go to API call
        /// </summary>
        private async Task<DialogTurnResult> RouteToGeographyPromptOrForecastResponse(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context);
            var geography = state.Geography;

            if (string.IsNullOrEmpty(geography))
            {
                return await stepContext.NextAsync();

            }
            else
            {
                return await stepContext.ReplaceDialogAsync(DialogIds.GetForecastResponseDialog);
            }
        }

        /// <summary>
        /// Ask user for current location
        /// </summary>
        private async Task<DialogTurnResult> GeographyPrompt(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var prompt = ResponseManager.GetResponse(SharedResponses.LocationPrompt);
            return await stepContext.PromptAsync(DialogIds.GeographyPrompt, new PromptOptions { Prompt = prompt });
        }

        /// <summary>
        /// Validate that a geography entity is pulled from the LUIS result.
        /// </summary>
        private async Task<bool> GeographyLuisValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            if (!promptContext.Recognized.Succeeded)
            {
                var prompt = ResponseManager.GetResponse(SharedResponses.LocationPrompt);
                await promptContext.Context.SendActivityAsync(prompt, cancellationToken: cancellationToken);
                return false;
            }

            var state = await _stateAccessor.GetAsync(promptContext.Context);

            // check if geography is in state from processed LUIS result
            if (string.IsNullOrEmpty(state.Geography))
            {
                var prompt = ResponseManager.GetResponse(SharedResponses.LocationPrompt);
                await promptContext.Context.SendActivityAsync(prompt, cancellationToken: cancellationToken);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Look up the six hour forecast using Accuweather.
        /// </summary>
        private async Task<DialogTurnResult> GetWeatherResponse(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context);

            var service = new AccuweatherService(Settings);
            state.GeographyLocation = await service.GetLocationByQueryAsync(state.Geography);

            var oneDayForecast = await service.GetOneDayForecastAsync(state.GeographyLocation.Key);

            var twelveHourForecast = await service.GetTwelveHourForecastAsync(state.GeographyLocation.Key);

            var hourlyForecasts = new List<HourDetails>();
            for (int i = 0; i < 6; i++)
            {
                hourlyForecasts.Add(new HourDetails()
                {
                    Hour = twelveHourForecast[i].DateTime.ToString("hh tt", CultureInfo.InvariantCulture),
                    Icon = GetWeatherIcon(twelveHourForecast[i].WeatherIcon),
                    Temperature = Convert.ToInt32(twelveHourForecast[i].Temperature.Value)
                });
            }

            var forecastModel = new SixHourForecastCard()
            {
                Speak = oneDayForecast.DailyForecasts[0].Day.ShortPhrase,
                Location = state.GeographyLocation.LocalizedName,
                DayIcon = GetWeatherIcon(oneDayForecast.DailyForecasts[0].Day.Icon),
                Date = $"{oneDayForecast.DailyForecasts[0].Date.DayOfWeek} {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(oneDayForecast.DailyForecasts[0].Date.Month)} {oneDayForecast.DailyForecasts[0].Date.Day}",
                MinimumTemperature = Convert.ToInt32(oneDayForecast.DailyForecasts[0].Temperature.Minimum.Value),
                MaximumTemperature = Convert.ToInt32(oneDayForecast.DailyForecasts[0].Temperature.Maximum.Value),
                ShortPhrase = oneDayForecast.DailyForecasts[0].Day.ShortPhrase,
                WindDescription = $"Winds {oneDayForecast.DailyForecasts[0].Day.Wind.Speed.Value} {oneDayForecast.DailyForecasts[0].Day.Wind.Speed.Unit} {oneDayForecast.DailyForecasts[0].Day.Wind.Direction.Localized}",
                Hour1 = twelveHourForecast[0].DateTime.ToString("h tt", CultureInfo.InvariantCulture),
                Icon1 = GetWeatherIcon(twelveHourForecast[0].WeatherIcon),
                Temperature1 = Convert.ToInt32(twelveHourForecast[0].Temperature.Value),
                Hour2 = twelveHourForecast[1].DateTime.ToString("h tt", CultureInfo.InvariantCulture),
                Icon2 = GetWeatherIcon(twelveHourForecast[1].WeatherIcon),
                Temperature2 = Convert.ToInt32(twelveHourForecast[1].Temperature.Value),
                Hour3 = twelveHourForecast[2].DateTime.ToString("h tt", CultureInfo.InvariantCulture),
                Icon3 = GetWeatherIcon(twelveHourForecast[2].WeatherIcon),
                Temperature3 = Convert.ToInt32(twelveHourForecast[2].Temperature.Value),
                Hour4 = twelveHourForecast[3].DateTime.ToString("h tt", CultureInfo.InvariantCulture),
                Icon4 = GetWeatherIcon(twelveHourForecast[3].WeatherIcon),
                Temperature4 = Convert.ToInt32(twelveHourForecast[3].Temperature.Value),
                Hour5 = twelveHourForecast[4].DateTime.ToString("h tt", CultureInfo.InvariantCulture),
                Icon5 = GetWeatherIcon(twelveHourForecast[4].WeatherIcon),
                Temperature5 = Convert.ToInt32(twelveHourForecast[4].Temperature.Value),
                Hour6 = twelveHourForecast[5].DateTime.ToString("h tt", CultureInfo.InvariantCulture),
                Icon6 = GetWeatherIcon(twelveHourForecast[5].WeatherIcon),
                Temperature6 = Convert.ToInt32(twelveHourForecast[5].Temperature.Value)
            };

            var templateId = SharedResponses.SixHourForecast;
            var card = new Card("SixHourForecast", forecastModel);
            var response = ResponseManager.GetCardResponse(templateId, card, tokens: null);

            await stepContext.Context.SendActivityAsync(response);

            return await stepContext.NextAsync();
        }

        /// <summary>
        /// AccuWeather returns an icon id, correlate those to custom assets.
        /// https://apidev.accuweather.com/developers/weatherIcons
        /// </summary>
        public string GetWeatherIcon(int iconValue)
        {
            switch (iconValue)
            {
                // sunny
                case 1:
                // mostly sunny
                case 2:
                // partly sunny
                case 3:
                    // return sun icon
                    return "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA0OCA0%0D%0AOCI+PHRpdGxlPndlYXRoZXJpY29uczwvdGl0bGU+PHBhdGggZD0iTTE4LjU3LDIxLjVIMTUuMlYy%0D%0AMC4zOGgzLjM3Wm0xLjA4LTMuNzQtMi4zOC0yLjM5Ljc5LS44TDIwLjQ1LDE3Wm0wLDYuMzYuOC44%0D%0ATDE4LjA2LDI3LjNsLS43OS0uNzlabTQtNy4xMmEzLjc4LDMuNzgsMCwwLDEsMS41NC4zMSwzLjkx%0D%0ALDMuOTEsMCwwLDEsMi4wOSwyLjA5LDQsNCwwLDAsMSwwLDMuMDcsMy43MiwzLjcyLDAsMCwxLS44%0D%0ANCwxLjI1LDQuMTMsNC4xMywwLDAsMS0xLjI1Ljg1LDQsNCwwLDAsMS0zLjA3LDAsNCw0LDAsMCwx%0D%0ALTEuMjUtLjg1LDMuODMsMy44MywwLDAsMS0xLjE1LTIuNzhBNCw0LDAsMCwxLDIwLDE5LjRhNC4x%0D%0AMyw0LjEzLDAsMCwxLC44NS0xLjI1LDMuODIsMy44MiwwLDAsMSwxLjI1LS44NEEzLjc3LDMuNzcs%0D%0AMCwwLDEsMjMuNjMsMTdabTAsNi43NWEyLjY2LDIuNjYsMCwwLDAsMS4wOS0uMjIsMy4wOCwzLjA4%0D%0ALDAsMCwwLC45LS42LDMsMywwLDAsMCwuNi0uOSwyLjcsMi43LDAsMCwwLC4yMy0xLjA5LDIuNjYs%0D%0AMi42NiwwLDAsMC0uMjMtMS4wOSwyLjg1LDIuODUsMCwwLDAtMS41LTEuNSwyLjY2LDIuNjYsMCww%0D%0ALDAtMS4wOS0uMjMsMi43LDIuNywwLDAsMC0xLjA5LjIzLDIuODUsMi44NSwwLDAsMC0xLjUsMS41%0D%0ALDIuNjYsMi42NiwwLDAsMC0uMjIsMS4wOUEyLjY5LDIuNjksMCwwLDAsMjEsMjJhMywzLDAsMCww%0D%0ALC42LjksMy4wOCwzLjA4LDAsMCwwLC45LjZBMi42OSwyLjY5LDAsMCwwLDIzLjYzLDIzLjc1Wm0u%0D%0ANTctNy44N0gyMy4wN1YxMi41SDI0LjJaTTIzLjA3LDI2SDI0LjJ2My4zOEgyMy4wN1ptNC41NS04%0D%0ALjI0LS44LS44LDIuMzktMi4zOS43OS44Wm0wLDYuMzZMMzAsMjYuNTFsLS43OS43OS0yLjM5LTIu%0D%0AMzhabTQuNDUtMy43NFYyMS41SDI4LjdWMjAuMzhaIi8+PC9zdmc+";
                // intermittent clouds
                case 4:
                // hazy sunshine
                case 5:
                    // cloud icon
                    return "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA0OCA0%0D%0AOCI+PHRpdGxlPndlYXRoZXJpY29uczwvdGl0bGU+PHBhdGggZD0iTTI5Ljc5LDIyLjM4YTMuMzIs%0D%0AMy4zMiwwLDAsMSwxLjMzLjI2LDMuMzMsMy4zMywwLDAsMSwxLjgxLDEuNzgsMy4yOSwzLjI5LDAs%0D%0AMCwxLC4yNywxLjMzLDMuMTcsMy4xNywwLDAsMS0uMjcsMS4zMSwzLjMsMy4zLDAsMCwxLS43Miwx%0D%0ALjA4LDMuNDYsMy40NiwwLDAsMS0xLjA4LjcyLDMuMjMsMy4yMywwLDAsMS0xLjMxLjI2SDE5Ljdh%0D%0ANC4zNiw0LjM2LDAsMCwxLTEuNzYtLjM1LDQuNSw0LjUsMCwwLDEtMS40My0xLDQuMzksNC4zOSww%0D%0ALDAsMS0xLTEuNDMsNC41OCw0LjU4LDAsMCwxLDAtMy41MSw0LjM5LDQuMzksMCwwLDEsMS0xLjQz%0D%0ALDQuNSw0LjUsMCwwLDEsMS40My0xLDQuMzYsNC4zNiwwLDAsMSwxLjc2LS4zNiw0LjQ1LDQuNDUs%0D%0AMCwwLDEsLjgxLjA4LDUuNTQsNS41NCwwLDAsMSwuODItMSw0Ljc1LDQuNzUsMCwwLDEsMS0uNzNB%0D%0ANS4yOSw1LjI5LDAsMCwxLDIzLjQ5LDE4YTUuMzgsNS4zOCwwLDAsMSwxLjI3LS4xNSw1LDUsMCww%0D%0ALDEsMy4zNywxLjI4LDUsNSwwLDAsMSwxLjExLDEuNDNBNS4xOSw1LjE5LDAsMCwxLDI5Ljc5LDIy%0D%0ALjM4Wm0wLDUuNjJhMi4yLDIuMiwwLDAsMCwuODgtLjE4LDIsMiwwLDAsMCwuNzEtLjQ4LDIuMTQs%0D%0AMi4xNCwwLDAsMCwuNDktLjcyLDIuMzEsMi4zMSwwLDAsMCwwLTEuNzQsMi4xNCwyLjE0LDAsMCww%0D%0ALS40OS0uNzIsMiwyLDAsMCwwLS43MS0uNDgsMi4yLDIuMiwwLDAsMC0uODgtLjE4SDI4Ljd2LS41%0D%0ANmE0LDQsMCwwLDAtLjMxLTEuNTQsNC4xMyw0LjEzLDAsMCwwLS44NS0xLjI1LDMuNzIsMy43Miww%0D%0ALDAsMC0xLjI1LS44NEEzLjc3LDMuNzcsMCwwLDAsMjQuNzYsMTlhMy45NCwzLjk0LDAsMCwwLTEu%0D%0AMTkuMTgsNC4yOSw0LjI5LDAsMCwwLTEuMDUuNTIsNC4wNyw0LjA3LDAsMCwwLS44NS44MSw0LjEz%0D%0ALDQuMTMsMCwwLDAtLjU5LDEsMy4xMywzLjEzLDAsMCwwLTEuMzgtLjMxLDMuNDEsMy40MSwwLDAs%0D%0AMC0xLjMyLjI2LDMuNTcsMy41NywwLDAsMC0xLjA3LjczLDMuNDksMy40OSwwLDAsMC0uNzMsMS4w%0D%0AOCwzLjM1LDMuMzUsMCwwLDAtLjI2LDEuMywzLjI5LDMuMjksMCwwLDAsMSwyLjM5LDMuNTcsMy41%0D%0ANywwLDAsMCwxLjA3LjczQTMuNDEsMy40MSwwLDAsMCwxOS43LDI4WiIvPjwvc3ZnPg==";
                // mostly cloudy
                case 6:
                // cloudy
                case 7:    
                // dreary (overcast)
                case 8:
                    // cloud2x icon
                    return "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA0OCA0%0D%0AOCI+PHRpdGxlPndlYXRoZXJpY29uczwvdGl0bGU+PHBhdGggZD0iTTMwLjY3LDIzLjYxYTMuMzMs%0D%0AMy4zMywwLDAsMSwxLC40NywzLjI0LDMuMjQsMCwwLDEsLjc5LjczLDMuMTksMy4xOSwwLDAsMSwu%0D%0ANTIsMSwzLjQsMy40LDAsMCwxLC4xOSwxLjExLDMuNCwzLjQsMCwwLDEtMSwyLjM4LDMuNDksMy40%0D%0AOSwwLDAsMS0xLjA4LjczLDMuNCwzLjQsMCwwLDEtMS4zMS4yNkgyMC4yNmEzLjcsMy43LDAsMCwx%0D%0ALTEuNTMtLjMxLDMuODcsMy44NywwLDAsMS0xLjI2LS44NCw0LjEsNC4xLDAsMCwxLS44NC0xLjI1%0D%0ALDMuOTQsMy45NCwwLDAsMS0uMS0yLjgsMy41LDMuNSwwLDAsMS0xLTEuMTgsMy4yMywzLjIzLDAs%0D%0AMCwxLS4zNS0xLjQ5LDMuMTksMy4xOSwwLDAsMSwuMjYtMS4zMUEzLjQ5LDMuNDksMCwwLDEsMTYu%0D%0AMTksMjBhMy4yOSwzLjI5LDAsMCwxLDIuMzgtMWguMTlMMTksMTlhMy43NiwzLjc2LDAsMCwxLC42%0D%0AMS0uOTUsNC4wNiw0LjA2LDAsMCwxLC44NC0uNzEsMy40OCwzLjQ4LDAsMCwxLDEtLjQ1LDMuNzcs%0D%0AMy43NywwLDAsMSwxLjExLS4xNiwzLjg3LDMuODcsMCwwLDEsMS40LjI2LDQuMDksNC4wOSwwLDAs%0D%0AMSwxLjE5LjcxQTMuODMsMy44MywwLDAsMSwyNiwxOC44YTMuNywzLjcsMCwwLDEsLjQ0LDEuMzUs%0D%0ANC44OCw0Ljg4LDAsMCwxLDEuNDEuMzYsNS4wOSw1LjA5LDAsMCwxLDEuMjMuNzQsNS40Myw1LjQz%0D%0ALDAsMCwxLDEsMS4wNUE0LjgzLDQuODMsMCwwLDEsMzAuNjcsMjMuNjFabS0xMi4xLTMuNDlhMi4x%0D%0AMiwyLjEyLDAsMCwwLS44Ny4xOCwyLjM5LDIuMzksMCwwLDAtLjcyLjQ4LDIuMzMsMi4zMywwLDAs%0D%0AMC0uNDguNzIsMi4xMywyLjEzLDAsMCwwLS4xOC44OCwyLjE3LDIuMTcsMCwwLDAsLjE5LjksMi40%0D%0ANywyLjQ3LDAsMCwwLC41NC43NSwzLjg4LDMuODgsMCwwLDEsMy4yMS0xLjY1LDMuNjcsMy42Nyww%0D%0ALDAsMSwuNjYuMDUsMy40MiwzLjQyLDAsMCwxLC42My4xNiw0LjY0LDQuNjQsMCwwLDEsLjY5LS45%0D%0AMSw1LjU5LDUuNTksMCwwLDEsLjg4LS43Myw1Ljg1LDUuODUsMCwwLDEsMS0uNTIsNS41NCw1LjU0%0D%0ALDAsMCwxLDEuMTItLjI3LDIuODcsMi44NywwLDAsMC0uMzUtLjkxLDIuNTksMi41OSwwLDAsMC0u%0D%0ANjItLjczLDIuNzEsMi43MSwwLDAsMC0uODMtLjQ3LDIuNjMsMi42MywwLDAsMC0xLS4xNywyLjcz%0D%0ALDIuNzMsMCwwLDAtMSwuMTksMi44LDIuOCwwLDAsMC0uODcuNTUsMywzLDAsMCwwLS42MS44Miwy%0D%0ALjg2LDIuODYsMCwwLDAtLjI4LDEsMi43LDIuNywwLDAsMC0uNTUtLjI0QTIsMiwwLDAsMCwxOC41%0D%0ANywyMC4xMlptMTEuMjUsOUEyLjE5LDIuMTksMCwwLDAsMzAuNywyOWEyLjI5LDIuMjksMCwwLDAs%0D%0AMS4yLTEuMiwyLjI5LDIuMjksMCwwLDAsLjE3LS44NywyLjE5LDIuMTksMCwwLDAtLjE4LS45LDIs%0D%0AMiwwLDAsMC0uNS0uNzIsMi4yMywyLjIzLDAsMCwwLS43My0uNDcsMi40NCwyLjQ0LDAsMCwwLS45%0D%0ALS4xNyw0LDQsMCwwLDAtMS4zMS0yLjQxLDMuODEsMy44MSwwLDAsMC0yLjU3LTEsMy42MywzLjYz%0D%0ALDAsMCwwLTEuMjguMjIsNC4xOSw0LjE5LDAsMCwwLTEuMTIuNjEsMy42NSwzLjY1LDAsMCwwLS44%0D%0ANi45MywzLjkyLDMuOTIsMCwwLDAtLjUzLDEuMTgsMywzLDAsMCwwLS44Ni0uNTEsMi42NSwyLjY1%0D%0ALDAsMCwwLTEtLjE4LDIuNzQsMi43NCwwLDAsMC0xLjEuMjIsMi45MSwyLjkxLDAsMCwwLS44OS42%0D%0ALDIuNzMsMi43MywwLDAsMC0uNi45LDIuNjksMi42OSwwLDAsMC0uMjIsMS4wOSwyLjc0LDIuNzQs%0D%0AMCwwLDAsLjIyLDEuMSwyLjU5LDIuNTksMCwwLDAsLjYuODksMi43NywyLjc3LDAsMCwwLC44OS42%0D%0AMSwyLjkxLDIuOTEsMCwwLDAsMS4xLjIxWiIvPjwvc3ZnPg==";
                // fog
                case 11:
                    // cloud fog icon
                    return "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA0OCA0%0D%0AOCI+PHRpdGxlPndlYXRoZXJpY29uczwvdGl0bGU+PHBhdGggZD0iTTMwLjY3LDIxLjYxYTMuMzYs%0D%0AMy4zNiwwLDAsMSwxLC40NiwzLjUyLDMuNTIsMCwwLDEsLjguNzQsMy40NywzLjQ3LDAsMCwxLC43%0D%0AMSwyLjA3LDIuNzYsMi43NiwwLDAsMS0uMDYuNTdBNS42Miw1LjYyLDAsMCwxLDMzLDI2SDMxLjc2%0D%0AYTIuMTQsMi4xNCwwLDAsMCwuMjMtLjU0LDIuMzIsMi4zMiwwLDAsMCwuMDgtLjU4LDIuMjMsMi4y%0D%0AMywwLDAsMC0uMTgtLjksMi4wOCwyLjA4LDAsMCwwLS41LS43MSwyLjQsMi40LDAsMCwwLTEuNjIt%0D%0ALjY1LDMuODEsMy44MSwwLDAsMC0uNDUtMS4zMyw0LDQsMCwwLDAtLjg2LTEuMDgsNCw0LDAsMCww%0D%0ALTEuMTgtLjcxLDMuODIsMy44MiwwLDAsMC0xLjQtLjI1LDMuNjMsMy42MywwLDAsMC0xLjI4LjIy%0D%0ALDQuMTksNC4xOSwwLDAsMC0xLjEyLjYxLDMuNjUsMy42NSwwLDAsMC0uODYuOTMsMy45MiwzLjky%0D%0ALDAsMCwwLS41MywxLjE4LDMsMywwLDAsMC0uODYtLjUxLDIuNjUsMi42NSwwLDAsMC0xLS4xOCwy%0D%0ALjc0LDIuNzQsMCwwLDAtMS4xLjIyLDIuOTEsMi45MSwwLDAsMC0uODkuNiwyLjczLDIuNzMsMCww%0D%0ALDAtLjYuOSwyLjc3LDIuNzcsMCwwLDAtLjA3LDJBMi44MiwyLjgyLDAsMCwwLDE4LDI2SDE2Ljcy%0D%0AYTQsNCwwLDAsMS0uMy0uODIsMy42MywzLjYzLDAsMCwxLS4xLS44Nyw0LDQsMCwwLDEsLjMxLTEu%0D%0ANTMsNCw0LDAsMCwxLDIuMS0yLjA5LDMuNywzLjcsMCwwLDEsMS41My0uMzEsNC4xNSw0LjE1LDAs%0D%0AMCwxLDEuMjkuMjEsNS4xNyw1LjE3LDAsMCwxLDEuODQtMS44LDQuNTksNC41OSwwLDAsMSwxLjE5%0D%0ALS40OSw0Ljc0LDQuNzQsMCwwLDEsMS4zLS4xOCw0LjgzLDQuODMsMCwwLDEsMS41Ny4yNiw1LjEs%0D%0ANS4xLDAsMCwxLDEuMzkuNzIsNC43OCw0Ljc4LDAsMCwxLDEuMSwxLjFBNC43Miw0LjcyLDAsMCwx%0D%0ALDMwLjY3LDIxLjYxWm0uODQsNS41MWEuNTQuNTQsMCwwLDEsLjM5LjE3LjUyLjUyLDAsMCwxLC4x%0D%0ANy40LjUuNSwwLDAsMS0uMTcuMzkuNTQuNTQsMCwwLDEtLjM5LjE3SDE4YS41Ni41NiwwLDAsMS0u%0D%0ANC0uMTcuNTMuNTMsMCwwLDEtLjE2LS4zOS41NS41NSwwLDAsMSwuMTYtLjQuNTYuNTYsMCwwLDEs%0D%0ALjQtLjE3Wm0tMy4zOCwyLjI2YS41NC41NCwwLDAsMSwuNC4xNi41Ni41NiwwLDAsMSwuMTcuNC41%0D%0ANC41NCwwLDAsMS0uMTcuMzkuNTQuNTQsMCwwLDEtLjQuMTdIMjAuMjZhLjU2LjU2LDAsMCwxLS40%0D%0ALS4xNy41My41MywwLDAsMS0uMTYtLjM5LjU1LjU1LDAsMCwxLC41Ni0uNTZaTTIxLjM4LDI2YS41%0D%0AOC41OCwwLDAsMS0uNTYtLjU2QS41Ni41NiwwLDAsMSwyMSwyNWEuNTMuNTMsMCwwLDEsLjM5LS4x%0D%0ANmg3Ljg4YS41My41MywwLDAsMSwuMzkuMTYuNTIuNTIsMCwwLDEsLjE3LjQuNS41LDAsMCwxLS4x%0D%0ANy4zOS41NC41NCwwLDAsMS0uMzkuMTdaIi8+PC9zdmc+";
                // showers
                case 12:
                // rain
                case 18:
                    // rain icon
                    return "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA0OCA0%0D%0AOCI+PHRpdGxlPndlYXRoZXJpY29uczwvdGl0bGU+PHBhdGggZD0iTTMwLjY3LDIzLjYxYTMuMzYs%0D%0AMy4zNiwwLDAsMSwxLC40NiwzLjMxLDMuMzEsMCwwLDEsLjguNzQsMy40MSwzLjQxLDAsMCwxLC43%0D%0AMSwyLjA3QTMuMjUsMy4yNSwwLDAsMSwzMywyOGEzLjM0LDMuMzQsMCwwLDEtMi4zOSwyLjE1bC0u%0D%0ANTMtMS4wNWEyLjUxLDIuNTEsMCwwLDAsLjc5LS4yNCwyLjMyLDIuMzIsMCwwLDAsLjYzLS40OSwy%0D%0ALjM1LDIuMzUsMCwwLDAsLjQyLS42OCwyLjE1LDIuMTUsMCwwLDAsLjE1LS44LDIuMTksMi4xOSww%0D%0ALDAsMC0uMTgtLjksMiwyLDAsMCwwLS41LS43MSwyLjIzLDIuMjMsMCwwLDAtLjczLS40NywyLjI2%0D%0ALDIuMjYsMCwwLDAtLjktLjE4LDQsNCwwLDAsMC0xLjMxLTIuNDEsMy44MSwzLjgxLDAsMCwwLTIu%0D%0ANTctMSwzLjYzLDMuNjMsMCwwLDAtMS4yOC4yMiw0LjE5LDQuMTksMCwwLDAtMS4xMi42MSwzLjY1%0D%0ALDMuNjUsMCwwLDAtLjg2LjkzLDMuOTIsMy45MiwwLDAsMC0uNTMsMS4xOCwzLDMsMCwwLDAtLjg2%0D%0ALS41MSwyLjY1LDIuNjUsMCwwLDAtMS0uMTgsMi43NCwyLjc0LDAsMCwwLTEuMS4yMiwyLjkxLDIu%0D%0AOTEsMCwwLDAtLjg5LjYsMi43MywyLjczLDAsMCwwLS42LjksMi42OSwyLjY5LDAsMCwwLS4yMiwx%0D%0ALjA5LDIuNzQsMi43NCwwLDAsMCwuMTUuOUEyLjgzLDIuODMsMCwwLDAsMTgsMjhhMy4wOSwzLjA5%0D%0ALDAsMCwwLC42NC42MSwzLDMsMCwwLDAsLjgzLjM5TDE5LDMwYTMuNDksMy40OSwwLDAsMS0xLjA4%0D%0ALS41NywzLjc2LDMuNzYsMCwwLDEtLjg0LS44NSw0LjExLDQuMTEsMCwwLDEtLjU1LTEuMDYsNCw0%0D%0ALDAsMCwxLS4xOS0xLjIxLDQsNCwwLDAsMSwuMzEtMS41Myw0LDQsMCwwLDEsMi4xLTIuMDksMy43%0D%0ALDMuNywwLDAsMSwxLjUzLS4zMSw0LjE1LDQuMTUsMCwwLDEsMS4yOS4yMSw1LjE3LDUuMTcsMCww%0D%0ALDEsMS44NC0xLjgsNC41OSw0LjU5LDAsMCwxLDEuMTktLjQ5LDQuNzQsNC43NCwwLDAsMSwxLjMt%0D%0ALjE4LDQuODMsNC44MywwLDAsMSwxLjU3LjI2LDUuMSw1LjEsMCwwLDEsMS4zOS43Miw1LDUsMCww%0D%0ALDEsMS44MywyLjUxWm0tOCw3YTEuMjQsMS4yNCwwLDAsMSwuMTIuNTMsMS40MiwxLjQyLDAsMCwx%0D%0ALS4xMS41NSwxLjM4LDEuMzgsMCwwLDEtLjc1Ljc1LDEuNDEsMS40MSwwLDAsMS0xLjA5LDAsMS4z%0D%0AOCwxLjM4LDAsMCwxLS43NS0uNzUsMS4yNiwxLjI2LDAsMCwxLS4xMS0uNTUsMS4yNCwxLjI0LDAs%0D%0AMCwxLC4xMi0uNTNMMjEuMzgsMjhaTTI2LDI4LjMxYTEuMjUsMS4yNSwwLDAsMSwuMTMuNTMsMS40%0D%0AMiwxLjQyLDAsMCwxLS4xMS41NSwxLjU3LDEuNTcsMCwwLDEtLjMxLjQ1LDEuMzcsMS4zNywwLDAs%0D%0AMS0uNDUuMywxLjQxLDEuNDEsMCwwLDEtLjU0LjExLDEuNDYsMS40NiwwLDAsMS0uNTUtLjExLDEu%0D%0AMjMsMS4yMywwLDAsMS0uNDQtLjMsMS41NywxLjU3LDAsMCwxLS4zMS0uNDUsMS40MiwxLjQyLDAs%0D%0AMCwxLS4xMS0uNTUsMS4xMiwxLjEyLDAsMCwxLC4xMy0uNTNsMS4yOC0yLjU2Wm0zLjM4LDIuMjVh%0D%0AMS4yNCwxLjI0LDAsMCwxLC4xMi41MywxLjQyLDEuNDIsMCwwLDEtLjExLjU1LDEuMzgsMS4zOCww%0D%0ALDAsMS0uNzUuNzUsMS40MSwxLjQxLDAsMCwxLTEuMDksMCwxLjM4LDEuMzgsMCwwLDEtLjc1LS43%0D%0ANSwxLjI2LDEuMjYsMCwwLDEtLjExLS41NSwxLjI0LDEuMjQsMCwwLDEsLjEyLS41M0wyOC4xMywy%0D%0AOFoiLz48L3N2Zz4=";
                // mostly cloudy w showers
                case 13:
                // partly sunny w showers
                case 14:
                    // cloudsunrain icon
                    return "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA0OCA0%0D%0AOCI+PHRpdGxlPndlYXRoZXJpY29uczwvdGl0bGU+PHBhdGggZD0iTTE3LjQ1LDIyLjM4SDE1LjJW%0D%0AMjEuMjVoMi4yNVptMTMuMjIsMS4yM2EzLjM2LDMuMzYsMCwwLDEsMSwuNDYsMy4zMSwzLjMxLDAs%0D%0AMCwxLC44Ljc0LDMuNDEsMy40MSwwLDAsMSwuNzEsMi4wNywzLjQsMy40LDAsMCwxLTEsMi4zOCwz%0D%0ALjQ5LDMuNDksMCwwLDEtMS4wOC43MywzLjQsMy40LDAsMCwxLTEuMzEuMjZoLS45NWEyLjYxLDIu%0D%0ANjEsMCwwLDAsLjI0LS41NCwyLjk0LDIuOTQsMCwwLDAsLjEyLS41OWguNTlBMi4xOSwyLjE5LDAs%0D%0AMCwwLDMwLjcsMjlhMi4yOSwyLjI5LDAsMCwwLDEuMi0xLjIsMi4yOSwyLjI5LDAsMCwwLC4xNy0u%0D%0AODcsMi4xOSwyLjE5LDAsMCwwLS4xOC0uOSwyLDIsMCwwLDAtLjUtLjcxLDIuMjMsMi4yMywwLDAs%0D%0AMC0uNzMtLjQ3LDIuMjYsMi4yNiwwLDAsMC0uOS0uMTgsNCw0LDAsMCwwLTEuMzEtMi40MSwzLjgx%0D%0ALDMuODEsMCwwLDAtMi41Ny0xLDMuNjMsMy42MywwLDAsMC0xLjI4LjIyLDQuMTksNC4xOSwwLDAs%0D%0AMC0xLjEyLjYxLDMuNjUsMy42NSwwLDAsMC0uODYuOTMsMy45MiwzLjkyLDAsMCwwLS41MywxLjE4%0D%0ALDMsMywwLDAsMC0uODYtLjUxLDIuNjUsMi42NSwwLDAsMC0xLS4xOCwyLjc0LDIuNzQsMCwwLDAt%0D%0AMS4xLjIyLDIuOTEsMi45MSwwLDAsMC0uODkuNiwyLjczLDIuNzMsMCwwLDAtLjYuOSwyLjY5LDIu%0D%0ANjksMCwwLDAtLjIyLDEuMDksMi43NCwyLjc0LDAsMCwwLC4yMiwxLjEsMi41OSwyLjU5LDAsMCww%0D%0ALC42Ljg5LDIuNzcsMi43NywwLDAsMCwuODkuNjEsMi45MSwyLjkxLDAsMCwwLDEuMS4yMWguODRs%0D%0ALS41NiwxLjEzaC0uMjhhMy43LDMuNywwLDAsMS0xLjUzLS4zMSwzLjksMy45LDAsMCwxLTEuMjUt%0D%0ALjg1LDMuOTEsMy45MSwwLDAsMS0xLTMuOTIsNC4yOSw0LjI5LDAsMCwxLC40Ny0xLDMuOTQsMy45%0D%0ANCwwLDAsMSwuNzUtLjgzLDMuODgsMy44OCwwLDAsMSwxLS42MSwzLjgyLDMuODIsMCwwLDEtLjEx%0D%0ALS45LDQsNCwwLDAsMSwuMzEtMS41M0E0LDQsMCwwLDEsMjEsMTguMTlhMy43LDMuNywwLDAsMSwx%0D%0ALjUzLS4zMSw0LDQsMCwwLDEsMS4xLjE1LDQuMTIsNC4xMiwwLDAsMSwxLjgzLDEuMTcsNC4zMiw0%0D%0ALjMyLDAsMCwxLC42MS45Miw0Ljg5LDQuODksMCwwLDEsMS41Mi4zLDUuMzQsNS4zNCwwLDAsMSwx%0D%0ALjMzLjcyQTUsNSwwLDAsMSwzMCwyMi4yMyw0LjczLDQuNzMsMCwwLDEsMzAuNjcsMjMuNjFabS0x%0D%0AMi4xNC01TDE2Ljk0LDE3bC44LS44LDEuNTksMS41OVptMyw0YTUsNSwwLDAsMSwzLjI4LTIuMzUs%0D%0AMi43NiwyLjc2LDAsMCwwLTEtLjkxQTIuNjgsMi42OCwwLDAsMCwyMi41MSwxOWEyLjc0LDIuNzQs%0D%0AMCwwLDAtMS4xLjIyLDIuOTMsMi45MywwLDAsMC0uODkuNjEsMi43OSwyLjc5LDAsMCwwLS44Miwy%0D%0ALDIuNDQsMi40NCwwLDAsMCwuMDcuNTksMy45MywzLjkzLDAsMCwxLC40OSwwQTQuMTUsNC4xNSww%0D%0ALDAsMSwyMS41NSwyMi41OVptMi44LDhhMS4xMiwxLjEyLDAsMCwxLC4xMy41MywxLjQyLDEuNDIs%0D%0AMCwwLDEtLjExLjU1LDEuNTcsMS41NywwLDAsMS0uMzEuNDUsMS4yMywxLjIzLDAsMCwxLS40NC4z%0D%0ALDEuNDYsMS40NiwwLDAsMS0uNTUuMTEsMS40MSwxLjQxLDAsMCwxLS41NC0uMTEsMS4zNywxLjM3%0D%0ALDAsMCwxLS40NS0uMywxLjU3LDEuNTcsMCwwLDEtLjMxLS40NSwxLjQyLDEuNDIsMCwwLDEtLjEt%0D%0ALjU1LDEuMjQsMS4yNCwwLDAsMSwuMTItLjUzTDIzLjA3LDI4Wk0yMy4wNywxNi43NUgyMlYxNC41%0D%0AaDEuMTJabTQuNjYsMTEuNTZhMS4yNCwxLjI0LDAsMCwxLC4xMi41MywxLjI2LDEuMjYsMCwwLDEt%0D%0ALjExLjU1LDEuMzgsMS4zOCwwLDAsMS0uNzUuNzUsMS40MSwxLjQxLDAsMCwxLTEuMDksMCwxLjM4%0D%0ALDEuMzgsMCwwLDEtLjc1LS43NSwxLjQyLDEuNDIsMCwwLDEtLjExLS41NSwxLjI0LDEuMjQsMCww%0D%0ALDEsLjEyLS41M2wxLjI5LTIuNTZabS0xLjI0LTkuNjgtLjgtLjgsMS41OS0xLjU5LjguOFoiLz48%0D%0AL3N2Zz4=";
                // thunderstorms
                case 15:
                // mostly cloudy w thunderstorms
                case 16:
                // partly sunny w thunderstorms
                case 17:
                    // lightning icon
                    return "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA0OCA0%0D%0AOCI+PHRpdGxlPndlYXRoZXJpY29uczwvdGl0bGU+PHBhdGggZD0iTTMwLjY3LDIzLjYxYTMuMzYs%0D%0AMy4zNiwwLDAsMSwxLC40NiwzLjMxLDMuMzEsMCwwLDEsLjguNzQsMy40MSwzLjQxLDAsMCwxLC43%0D%0AMSwyLjA3LDMuNCwzLjQsMCwwLDEtMSwyLjM4LDMuNDksMy40OSwwLDAsMS0xLjA4LjczLDMuNCwz%0D%0ALjQsMCwwLDEtMS4zMS4yNkgyNi40NWwxLjEyLTEuMTNoMi4yNUEyLjE5LDIuMTksMCwwLDAsMzAu%0D%0ANywyOWEyLjI5LDIuMjksMCwwLDAsMS4yLTEuMiwyLjI5LDIuMjksMCwwLDAsLjE3LS44NywyLjIz%0D%0ALDIuMjMsMCwwLDAtLjE4LS45LDIuMDgsMi4wOCwwLDAsMC0uNS0uNzEsMi40LDIuNCwwLDAsMC0x%0D%0ALjYyLS42NSwzLjkzLDMuOTMsMCwwLDAtLjQ1LTEuMzQsNC4wOSw0LjA5LDAsMCwwLS44Ni0xLjA3%0D%0ALDQsNCwwLDAsMC0xLjE4LS43MSwzLjgyLDMuODIsMCwwLDAtMS40LS4yNSwzLjYzLDMuNjMsMCww%0D%0ALDAtMS4yOC4yMiw0LjE5LDQuMTksMCwwLDAtMS4xMi42MSwzLjY1LDMuNjUsMCwwLDAtLjg2Ljkz%0D%0ALDMuOTIsMy45MiwwLDAsMC0uNTMsMS4xOCwzLDMsMCwwLDAtLjg2LS41MSwyLjY1LDIuNjUsMCww%0D%0ALDAtMS0uMTgsMi43NCwyLjc0LDAsMCwwLTEuMS4yMiwyLjkxLDIuOTEsMCwwLDAtLjg5LjYsMi43%0D%0AMywyLjczLDAsMCwwLS42LjksMi42OSwyLjY5LDAsMCwwLS4yMiwxLjA5LDIuNzQsMi43NCwwLDAs%0D%0AMCwuMjIsMS4xLDIuNTksMi41OSwwLDAsMCwuNi44OSwyLjc3LDIuNzcsMCwwLDAsLjg5LjYxLDIu%0D%0AOTEsMi45MSwwLDAsMCwxLjEuMjFoMi4yNUwyMiwzMC4yNUgyMC4yNmEzLjc4LDMuNzgsMCwwLDEt%0D%0AMS41NC0uMzEsMy45MywzLjkzLDAsMCwxLTIuNC0zLjYzLDMuNzksMy43OSwwLDAsMSwuMjEtMS4y%0D%0ANiwzLjUsMy41LDAsMCwxLTEtMS4xOCwzLjIzLDMuMjMsMCwwLDEtLjM1LTEuNDksMy4xOSwzLjE5%0D%0ALDAsMCwxLC4yNi0xLjMxQTMuNDksMy40OSwwLDAsMSwxNi4xOSwyMGEzLjI5LDMuMjksMCwwLDEs%0D%0AMi4zOC0xaC4xOUwxOSwxOWEzLjc2LDMuNzYsMCwwLDEsLjYxLS45NSw0LjI4LDQuMjgsMCwwLDEs%0D%0ALjgzLS43MSwzLjY3LDMuNjcsMCwwLDEsMS0uNDUsMy43NywzLjc3LDAsMCwxLDEuMTEtLjE2LDMu%0D%0AODcsMy44NywwLDAsMSwxLjQuMjYsNC4wOSw0LjA5LDAsMCwxLDEuMTkuNzFBMy44MywzLjgzLDAs%0D%0AMCwxLDI2LDE4LjhhMy43LDMuNywwLDAsMSwuNDQsMS4zNSw0Ljg5LDQuODksMCwwLDEsMS40MS4z%0D%0ANyw1LjE5LDUuMTksMCwwLDEsMS4yMi43Myw1LjQxLDUuNDEsMCwwLDEsMSwxLjA1QTQuODYsNC44%0D%0ANiwwLDAsMSwzMC42NywyMy42MVptLTkuMTItMWE0Ljc5LDQuNzksMCwwLDEsLjctLjkxLDQuNjgs%0D%0ANC42OCwwLDAsMSwuODgtLjczLDUuNDQsNS40NCwwLDAsMSwxLS41Miw1LjIzLDUuMjMsMCwwLDEs%0D%0AMS4xMi0uMjcsMi44NywyLjg3LDAsMCwwLS4zNS0uOTEsMi43NiwyLjc2LDAsMCwwLS42Mi0uNzMs%0D%0AMi44MywyLjgzLDAsMCwwLS44My0uNDcsMi42MywyLjYzLDAsMCwwLTEtLjE3LDIuNzMsMi43Myww%0D%0ALDAsMC0xLC4xOSwyLjY0LDIuNjQsMCwwLDAtLjg2LjU1LDIuODIsMi44MiwwLDAsMC0uNjIuODIs%0D%0AMi44NiwyLjg2LDAsMCwwLS4yOCwxLDIuNywyLjcsMCwwLDAtLjU1LS4yNCwyLDIsMCwwLDAtLjYt%0D%0ALjA5LDIuMTIsMi4xMiwwLDAsMC0uODcuMTgsMi4zOSwyLjM5LDAsMCwwLS43Mi40OCwyLjMzLDIu%0D%0AMzMsMCwwLDAtLjQ4LjcyLDIuMTMsMi4xMywwLDAsMC0uMTguODgsMi4xNywyLjE3LDAsMCwwLC4x%0D%0AOS45LDIuMTUsMi4xNSwwLDAsMCwuNTUuNzUsMy44OCwzLjg4LDAsMCwxLDMuMi0xLjY1QTQuMTUs%0D%0ANC4xNSwwLDAsMSwyMS41NSwyMi41OVptMSw5LjkxTDI0Ljc2LDI4aC0ybDEuNjktMy4zOGgybC0x%0D%0ALjEzLDIuMjZoMi44MVoiLz48L3N2Zz4=";
                // flurries
                case 19:
                // mostly cloudy w flurries
                case 20:
                // partly sunny w flurries
                case 21:
                // snow
                case 22:
                // mostly cloudy w snow
                case 23:
                // ice
                case 24:
                // sleet
                case 25:
                // freezing rain
                case 26:
                    //suncloudhail icon
                    return "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA0OCA0%0D%0AOCI+PHRpdGxlPndlYXRoZXJpY29uczwvdGl0bGU+PHBhdGggZD0iTTE3LjQ1LDIwLjM4SDE1LjJW%0D%0AMTkuMjVoMi4yNVptMTMuMjIsMS4yM2EzLjM2LDMuMzYsMCwwLDEsMSwuNDYsMy41MiwzLjUyLDAs%0D%0AMCwxLC44Ljc0LDMuNDEsMy40MSwwLDAsMSwuNzEsMi4wNywzLjQsMy40LDAsMCwxLTEsMi4zOCwz%0D%0ALjQ5LDMuNDksMCwwLDEtMS4wOC43MywzLjQsMy40LDAsMCwxLTEuMzEuMjZIMjguN1YyNy4xMmgx%0D%0ALjEyQTIuMTksMi4xOSwwLDAsMCwzMC43LDI3YTIuMjksMi4yOSwwLDAsMCwxLjItMS4yLDIuMjks%0D%0AMi4yOSwwLDAsMCwuMTctLjg3LDIuMTksMi4xOSwwLDAsMC0uMTgtLjksMiwyLDAsMCwwLS41LS43%0D%0AMSwyLjQsMi40LDAsMCwwLTEuNjItLjY1LDMuODEsMy44MSwwLDAsMC0uNDUtMS4zMyw0LDQsMCww%0D%0ALDAtLjg2LTEuMDgsNCw0LDAsMCwwLTEuMTgtLjcxLDMuODIsMy44MiwwLDAsMC0xLjQtLjI1LDMu%0D%0ANjMsMy42MywwLDAsMC0xLjI4LjIyLDQuMTksNC4xOSwwLDAsMC0xLjEyLjYxLDMuNjUsMy42NSww%0D%0ALDAsMC0uODYuOTMsMy45MiwzLjkyLDAsMCwwLS41MywxLjE4LDMsMywwLDAsMC0uODYtLjUxLDIu%0D%0ANjUsMi42NSwwLDAsMC0xLS4xOCwyLjc0LDIuNzQsMCwwLDAtMS4xLjIyLDIuOTEsMi45MSwwLDAs%0D%0AMC0uODkuNiwyLjczLDIuNzMsMCwwLDAtLjYuOSwyLjY5LDIuNjksMCwwLDAtLjIyLDEuMDksMi43%0D%0ANCwyLjc0LDAsMCwwLC4yMiwxLjEsMi41OSwyLjU5LDAsMCwwLC42Ljg5LDIuNzcsMi43NywwLDAs%0D%0AMCwuODkuNjEsMi45MSwyLjkxLDAsMCwwLDEuMS4yMWguNTZ2MS4xM2gtLjU2YTMuNzgsMy43OCww%0D%0ALDAsMS0xLjU0LS4zMSwzLjkzLDMuOTMsMCwwLDEtMi4yMy00Ljc2LDMuOCwzLjgsMCwwLDEsMS4y%0D%0AMi0xLjg2LDMuODgsMy44OCwwLDAsMSwxLS42MSwzLjgyLDMuODIsMCwwLDEtLjExLS45LDQsNCww%0D%0ALDAsMSwuMzEtMS41M0EzLjksMy45LDAsMCwxLDE5LjczLDE3LDMuOTQsMy45NCwwLDAsMSwyMSwx%0D%0ANi4xOWEzLjc4LDMuNzgsMCwwLDEsMS41NC0uMzEsNCw0LDAsMCwxLDEuMS4xNSw0LDQsMCwwLDEs%0D%0AMS44MywxLjE3LDQuMzIsNC4zMiwwLDAsMSwuNjEuOTIsNC44OSw0Ljg5LDAsMCwxLDEuNTIuMyw1%0D%0ALjQsNS40LDAsMCwxLDEuMzQuNzJBNS4yMiw1LjIyLDAsMCwxLDMwLDIwLjIzLDQuNzMsNC43Myww%0D%0ALDAsMSwzMC42NywyMS42MVptLTEyLjE0LTVMMTYuOTQsMTVsLjgtLjgsMS41OSwxLjU5Wm0zLDRh%0D%0ANSw1LDAsMCwxLDMuMjgtMi4zNSwyLjc2LDIuNzYsMCwwLDAtMS0uOTFBMi42OCwyLjY4LDAsMCww%0D%0ALDIyLjUxLDE3YTIuNzQsMi43NCwwLDAsMC0xLjEuMjIsMi45MywyLjkzLDAsMCwwLS44OS42MSwy%0D%0ALjc5LDIuNzksMCwwLDAtLjgyLDIsMi40NCwyLjQ0LDAsMCwwLC4wNy41OSwzLjkzLDMuOTMsMCww%0D%0ALDEsLjQ5LDBBNC4xNSw0LjE1LDAsMCwxLDIxLjU1LDIwLjU5Wm0xLjUyLTUuODRIMjJWMTIuNWgx%0D%0ALjEyWm0wLDEwLjI3YTEsMSwwLDAsMSwuMzguMDcsMSwxLDAsMCwxLC41My41My45NC45NCwwLDAs%0D%0AMSwwLC43NiwxLDEsMCwwLDEtLjUzLjUzLDEsMSwwLDAsMS0uMzguMDcsMSwxLDAsMCwxLS4zOC0u%0D%0AMDcsMSwxLDAsMCwxLS4zMS0uMjIuODUuODUsMCwwLDEtLjIxLS4zMS45NC45NCwwLDAsMSwwLS43%0D%0ANi44NS44NSwwLDAsMSwuMjEtLjMxLDEsMSwwLDAsMSwuMzEtLjIyQTEsMSwwLDAsMSwyMy4wNywy%0D%0ANVptMCwzLjM3YTEsMSwwLDAsMSwuMzguMDgsMSwxLDAsMCwxLC4zMi4yMUExLjQxLDEuNDEsMCww%0D%0ALDEsMjQsMjlhMSwxLDAsMCwxLC4wOC4zOSwxLDEsMCwwLDEtLjA4LjM4LDEuNDEsMS40MSwwLDAs%0D%0AMS0uMjEuMzEsMSwxLDAsMCwxLS4zMi4yMSwxLDEsMCwwLDEtLjM4LjA4LDEsMSwwLDAsMS0uMzgt%0D%0ALjA4Ljg1Ljg1LDAsMCwxLS4zMS0uMjEsMSwxLDAsMCwxLS4yMS0uMzEuODQuODQsMCwwLDEtLjA4%0D%0ALS4zOC44NS44NSwwLDAsMSwuMDgtLjM5LDEsMSwwLDAsMSwuMjEtLjMxLjg1Ljg1LDAsMCwxLC4z%0D%0AMS0uMjFBMSwxLDAsMCwxLDIzLjA3LDI4LjM5Wm0zLjM4LTQuNWExLDEsMCwwLDEsLjM4LjA4LDEs%0D%0AMSwwLDAsMSwuNTIuNTIsMSwxLDAsMCwxLC4wOC4zOSwxLDEsMCwwLDEtLjA4LjM4LDEsMSwwLDAs%0D%0AMS0uNTIuNTIsMSwxLDAsMCwxLS4zOC4wOCwxLDEsMCwwLDEtLjM5LS4wOCwxLDEsMCwwLDEtLjUy%0D%0ALS41MiwxLDEsMCwwLDEtLjA4LS4zOCwxLDEsMCwwLDEsLjA4LS4zOSwxLDEsMCwwLDEsLjUyLS41%0D%0AMkExLDEsMCwwLDEsMjYuNDUsMjMuODlabTAsMy4zOGExLDEsMCwwLDEsLjM4LjA3LDEuMTYsMS4x%0D%0ANiwwLDAsMSwuMzEuMjIuODUuODUsMCwwLDEsLjIxLjMxLjk0Ljk0LDAsMCwxLDAsLjc2Ljg1Ljg1%0D%0ALDAsMCwxLS4yMS4zMSwxLjE2LDEuMTYsMCwwLDEtLjMxLjIyLDEsMSwwLDAsMS0uMzguMDcsMSwx%0D%0ALDAsMCwxLS4zOS0uMDcsMS4xNiwxLjE2LDAsMCwxLS4zMS0uMjIuODUuODUsMCwwLDEtLjIxLS4z%0D%0AMS45NC45NCwwLDAsMSwwLS43Ni44NS44NSwwLDAsMSwuMjEtLjMxLDEuMTYsMS4xNiwwLDAsMSwu%0D%0AMzEtLjIyQTEsMSwwLDAsMSwyNi40NSwyNy4yN1ptMC0xMC42NC0uOC0uOCwxLjU5LTEuNTkuOC44%0D%0AWiIvPjwvc3ZnPg==";
                // rain and snow
                case 29:
                    //rainsnow icon
                    return "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA0OCA0%0D%0AOCI+PHRpdGxlPndlYXRoZXJpY29uczwvdGl0bGU+PHBhdGggZD0iTTMwLjY3LDIzLjYxYTMuMzYs%0D%0AMy4zNiwwLDAsMSwxLC40NiwzLjMxLDMuMzEsMCwwLDEsLjguNzQsMy40MSwzLjQxLDAsMCwxLC43%0D%0AMSwyLjA3LDMuMjcsMy4yNywwLDAsMS0uMTcsMSwzLjM2LDMuMzYsMCwwLDEtLjQ1LjkxLDMuNzcs%0D%0AMy43NywwLDAsMS0uNzEuNzNBMy41NCwzLjU0LDAsMCwxLDMxLDMwVjI4LjgyYTIuMzgsMi4zOCww%0D%0ALDAsMCwuODItLjgyLDIuMjgsMi4yOCwwLDAsMCwuMTItMiwyLjA4LDIuMDgsMCwwLDAtLjUtLjcx%0D%0ALDIuNCwyLjQsMCwwLDAtMS42Mi0uNjUsMy45MywzLjkzLDAsMCwwLS40NS0xLjM0LDQuMDksNC4w%0D%0AOSwwLDAsMC0uODYtMS4wNyw0LDQsMCwwLDAtMS4xOC0uNzEsMy44MiwzLjgyLDAsMCwwLTEuNC0u%0D%0AMjUsMy42MywzLjYzLDAsMCwwLTEuMjguMjIsNC4xOSw0LjE5LDAsMCwwLTEuMTIuNjEsMy42NSwz%0D%0ALjY1LDAsMCwwLS44Ni45MywzLjkyLDMuOTIsMCwwLDAtLjUzLDEuMTgsMywzLDAsMCwwLS44Ni0u%0D%0ANTEsMi42NSwyLjY1LDAsMCwwLTEtLjE4LDIuNzQsMi43NCwwLDAsMC0xLjEuMjIsMi45MSwyLjkx%0D%0ALDAsMCwwLS44OS42LDIuNzMsMi43MywwLDAsMC0uNi45LDIuNjksMi42OSwwLDAsMC0uMjIsMS4w%0D%0AOSwyLjc0LDIuNzQsMCwwLDAsLjIyLDEuMSwyLjU5LDIuNTksMCwwLDAsLjYuODksMi43NywyLjc3%0D%0ALDAsMCwwLC44OS42MSwyLjkxLDIuOTEsMCwwLDAsMS4xLjIxbC0uNTQsMS4wOGE0LDQsMCwwLDEt%0D%0AMS4zNS0uNDQsMy44MywzLjgzLDAsMCwxLTEuMDgtLjg3LDQsNCwwLDAsMS0uNzEtMS4xOCwzLjg3%0D%0ALDMuODcsMCwwLDEtLjI2LTEuNCw0LDQsMCwwLDEsLjMxLTEuNTMsNCw0LDAsMCwxLC44NS0xLjI1%0D%0ALDQuMTIsNC4xMiwwLDAsMSwxLjI0LS44NCwzLjc4LDMuNzgsMCwwLDEsMS41NC0uMzEsNC4xNSw0%0D%0ALjE1LDAsMCwxLDEuMjkuMjEsNS4xNyw1LjE3LDAsMCwxLDEuODQtMS44LDQuNTksNC41OSwwLDAs%0D%0AMSwxLjE5LS40OSw0Ljc0LDQuNzQsMCwwLDEsMS4zLS4xOCw0LjgzLDQuODMsMCwwLDEsMS41Ny4y%0D%0ANiw1LjEsNS4xLDAsMCwxLDEuMzkuNzIsNSw1LDAsMCwxLDEuODMsMi41MVptLTYuODgsN2ExLjIz%0D%0ALDEuMjMsMCwwLDEsMCwxLjA4LDEuMzgsMS4zOCwwLDAsMS0uNzUuNzUsMS40MSwxLjQxLDAsMCwx%0D%0ALS41NC4xMSwxLjQ2LDEuNDYsMCwwLDEtLjU1LS4xMSwxLjIzLDEuMjMsMCwwLDEtLjQ0LS4zLDEu%0D%0ANTcsMS41NywwLDAsMS0uMzEtLjQ1LDEuNDIsMS40MiwwLDAsMS0uMTEtLjU1LDEuMTIsMS4xMiww%0D%0ALDAsMSwuMTMtLjUzTDIyLjUxLDI4Wm00LjY5LTIuODRhLjUuNSwwLDAsMSwuMzkuMTcuNTMuNTMs%0D%0AMCwwLDEsLjE2LjM5LjUyLjUyLDAsMCwxLS4yOC40OGwtLjYyLjM2LjYyLjM3QS41Mi41MiwwLDAs%0D%0AMSwyOSwzMGEuNTQuNTQsMCwwLDEtLjE3LjQuNTMuNTMsMCwwLDEtLjM5LjE2LjUyLjUyLDAsMCwx%0D%0ALS4yNC0uMDVMMjgsMzAuMzdsLS4yMy0uMTQtLjItLjEzYzAsLjExLDAsLjI0LDAsLjM5YTEuMzks%0D%0AMS4zOSwwLDAsMSwwLC40MS43Mi43MiwwLDAsMS0uMTYuMzQuNDkuNDksMCwwLDEtLjM4LjE0LjUu%0D%0ANSwwLDAsMS0uMzktLjE0LjcyLjcyLDAsMCwxLS4xNi0uMzQsMS4zOSwxLjM5LDAsMCwxLDAtLjQx%0D%0AYzAtLjE1LDAtLjI4LDAtLjM5bC0uMjEuMTMtLjIyLjE0LS4yNC4xMWEuNDguNDgsMCwwLDEtLjIz%0D%0ALjA1LjU3LjU3LDAsMCwxLS40LS4xNkEuNTQuNTQsMCwwLDEsMjUsMzBhLjUyLjUyLDAsMCwxLC4y%0D%0AOC0uNDhsLjYxLS4zNy0uNjEtLjM2YS41Mi41MiwwLDAsMS0uMjgtLjQ4LjUzLjUzLDAsMCwxLC4x%0D%0ANi0uMzkuNS41LDAsMCwxLC4zOS0uMTcuNTIuNTIsMCwwLDEsLjI0LjA1LDEuMzQsMS4zNCwwLDAs%0D%0AMSwuMjQuMTJsLjIzLjE0LjIuMTJjMC0uMTEsMC0uMjQsMC0uMzlhMS4zOSwxLjM5LDAsMCwxLDAt%0D%0ALjQxLjcyLjcyLDAsMCwxLC4xNi0uMzQuNS41LDAsMCwxLC4zOS0uMTMuNDkuNDksMCwwLDEsLjM4%0D%0ALjEzLjcyLjcyLDAsMCwxLC4xNi4zNCwxLjM5LDEuMzksMCwwLDEsMCwuNDFjMCwuMTUsMCwuMjgs%0D%0AMCwuMzlsLjItLjEyLjIzLS4xNGExLjM0LDEuMzQsMCwwLDEsLjI0LS4xMkEuNTIuNTIsMCwwLDEs%0D%0AMjguNDgsMjcuNzJaIi8+PC9zdmc+";
                // hot
                case 30:
                // cold
                case 31:
                    //temperature icon
                    return "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA0OCA0%0D%0AOCI+PGRlZnM+PHN0eWxlPi5he2ZvbnQtc2l6ZToxNnB4O2ZvbnQtZmFtaWx5OkZ1bGxNREwyQXNz%0D%0AZXRzLCBGdWxsIE1ETDIgQXNzZXRzO308L3N0eWxlPjwvZGVmcz48dGl0bGU+d2VhdGhlcmljb25z%0D%0APC90aXRsZT48dGV4dCBjbGFzcz0iYSIgdHJhbnNmb3JtPSJ0cmFuc2xhdGUoMTUgMzMpIj7up4o8%0D%0AL3RleHQ+PC9zdmc+";
                // windy
                case 32:
                    //wind icon
                    return "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA0OCA0%0D%0AOCI+PHRpdGxlPndlYXRoZXJpY29uczwvdGl0bGU+PHBhdGggZD0iTTI4LjcsMTguMTJhMy40LDMu%0D%0ANCwwLDAsMS0xLDIuMzksMy40OSwzLjQ5LDAsMCwxLTEuMDguNzMsMy40LDMuNCwwLDAsMS0xLjMx%0D%0ALjI2SDE1LjJWMjAuMzhIMjUuMzJhMi4yLDIuMiwwLDAsMCwuODgtLjE4QTIuMjksMi4yOSwwLDAs%0D%0AMCwyNy40LDE5YTIuMzQsMi4zNCwwLDAsMCwwLTEuNzUsMi4yOSwyLjI5LDAsMCwwLTEuMi0xLjIs%0D%0AMi4xOSwyLjE5LDAsMCwwLS44OC0uMTcsMi4xMSwyLjExLDAsMCwwLS44Ny4xNywyLjM5LDIuMzks%0D%0AMCwwLDAtLjcyLjQ4LDIuMzMsMi4zMywwLDAsMC0uNDguNzIsMi4xMiwyLjEyLDAsMCwwLS4xOC44%0D%0AN0gyMmEzLjE4LDMuMTgsMCwwLDEsLjI2LTEuMywzLjQ5LDMuNDksMCwwLDEsLjczLTEuMDgsMy4y%0D%0AOSwzLjI5LDAsMCwxLDIuMzgtMSwzLjQsMy40LDAsMCwxLDEuMzEuMjYsMy40OSwzLjQ5LDAsMCwx%0D%0ALDEuMDguNzMsMy4zLDMuMywwLDAsMSwuNzIsMS4wOEEzLjE5LDMuMTksMCwwLDEsMjguNywxOC4x%0D%0AMlpNMzEsMjAuMzhhMi4xMSwyLjExLDAsMCwxLC44Ny4xNywyLjIxLDIuMjEsMCwwLDEsMS4yLDEu%0D%0AMiwyLjIyLDIuMjIsMCwwLDEsMCwxLjc1LDIuMjEsMi4yMSwwLDAsMS0xLjIsMS4yLDIuMTIsMi4x%0D%0AMiwwLDAsMS0uODcuMThIMjkuNTJhMi4xMywyLjEzLDAsMCwxLC4zLDEuMTIsMi4zOCwyLjM4LDAs%0D%0AMCwxLS4xNy44OCwyLjIsMi4yLDAsMCwxLS40OS43MSwyLDIsMCwwLDEtLjcxLjQ4LDIuMiwyLjIs%0D%0AMCwwLDEtLjg4LjE4LDIuMTIsMi4xMiwwLDAsMS0uODctLjE4LDIuMDcsMi4wNywwLDAsMS0uNzIt%0D%0ALjQ4LDIuMTcsMi4xNywwLDAsMS0uNDgtLjcxLDIuMiwyLjIsMCwwLDEtLjE4LS44OGgxLjEzYTEu%0D%0AMjcsMS4yNywwLDAsMCwuMDguNDQsMS4wOSwxLjA5LDAsMCwwLDEsLjY4QTEsMSwwLDAsMCwyOCwy%0D%0AN2ExLjExLDEuMTEsMCwwLDAsLjM2LS4yNSwxLjIxLDEuMjEsMCwwLDAsLjI0LS4zNSwxLjEyLDEu%0D%0AMTIsMCwwLDAsMC0uODgsMS4yMSwxLjIxLDAsMCwwLS4yNC0uMzVBMS4xMSwxLjExLDAsMCwwLDI4%0D%0ALDI1YTEsMSwwLDAsMC0uNDQtLjA4SDE1LjJWMjMuNzVIMzFhMS4xMiwxLjEyLDAsMCwwLC40NC0u%0D%0AMDksMS4yMSwxLjIxLDAsMCwwLC4zNS0uMjQsMS4xMywxLjEzLDAsMCwwLC4yNC0uMzYsMSwxLDAs%0D%0AMCwwLC4wOS0uNDQsMSwxLDAsMCwwLS4wOS0uNDMsMS4xMywxLjEzLDAsMCwwLS4yNC0uMzYsMS4y%0D%0AMSwxLjIxLDAsMCwwLS4zNS0uMjRBMS4xMiwxLjEyLDAsMCwwLDMxLDIxLjVhMSwxLDAsMCwwLS40%0D%0ANC4wOSwxLjEsMS4xLDAsMCwwLS42LjYsMSwxLDAsMCwwLS4wOS40M0gyOC43YTIuMTEsMi4xMSww%0D%0ALDAsMSwuMTctLjg3LDIuMjksMi4yOSwwLDAsMSwxLjItMS4yQTIuMTYsMi4xNiwwLDAsMSwzMSwy%0D%0AMC4zOFoiLz48L3N2Zz4=";
                // clear
                case 33:
                // mostly clear
                case 34:
                // partly cloudy
                case 35:
                // intermittent clouds
                case 36:
                // hazy moonlight
                case 37:
                // mostly cloudy
                case 38:
                    // cloudmoon icon
                    return "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA0OCA0%0D%0AOCI+PHRpdGxlPndlYXRoZXJpY29uczwvdGl0bGU+PHBhdGggZD0iTTMwLjY3LDIzLjYxYTMuMzks%0D%0AMy4zOSwwLDAsMSwxLC40NywzLjI5LDMuMjksMCwwLDEsLjguNzMsMy4xOSwzLjE5LDAsMCwxLC41%0D%0AMiwxLDMuNCwzLjQsMCwwLDEsLjE5LDEuMTEsMy4xOSwzLjE5LDAsMCwxLS4yNywxLjMsMy40LDMu%0D%0ANCwwLDAsMS0xLjgsMS44LDMuMjMsMy4yMywwLDAsMS0xLjMxLjI3SDIwLjI2YTMuNzgsMy43OCww%0D%0ALDAsMS0xLjU0LS4zMSwzLjkzLDMuOTMsMCwwLDEtMS4yNS0uODQsNC4xLDQuMSwwLDAsMS0uODQt%0D%0AMS4yNSwzLjc4LDMuNzgsMCwwLDEtLjMxLTEuNTRBMy44OCwzLjg4LDAsMCwxLDE2LjU0LDI1YTMu%0D%0AOCwzLjgsMCwwLDEsLjY1LTEuMTZBNC42LDQuNiwwLDAsMSwxNiwyMi42N2E0LjQxLDQuNDEsMCww%0D%0ALDEtLjY3LTEuNTQsNC4zNCw0LjM0LDAsMCwwLDEsLjEyLDQuNDUsNC40NSwwLDAsMCwxLjc1LS4z%0D%0ANSw0Ljc4LDQuNzgsMCwwLDAsMS40NC0xLDQuNiw0LjYsMCwwLDAsMS0xLjQ0LDQuNDUsNC40NSww%0D%0ALDAsMCwuMzUtMS43NSw0LjM0LDQuMzQsMCwwLDAtLjEyLTEsNC4yNSw0LjI1LDAsMCwxLDEuNC41%0D%0AOSw0LjM4LDQuMzgsMCwwLDEsMS4xMSwxLDQuNzQsNC43NCwwLDAsMSwuNzMsMS4zLDQuMzYsNC4z%0D%0ANiwwLDAsMSwuMjYsMS40OWMwLC4wNSwwLC4xLDAsLjE1czAsLjExLDAsLjE1aDBjLjI4LS4wOS41%0D%0ANS0uMTYuODItLjIyYTQuNjMsNC42MywwLDAsMSwuODUtLjA4LDQuOTQsNC45NCwwLDAsMSwxLjU4%0D%0ALjI2LDQuNzUsNC43NSwwLDAsMSwxLjM4LjcxLDUsNSwwLDAsMSwxLjEsMS4xQTQuOSw0LjksMCww%0D%0ALDEsMzAuNjcsMjMuNjFabS04Ljc5LTYuMDVhNS4yMSw1LjIxLDAsMCwxLS41NCwxLjczLDUuNTgs%0D%0ANS41OCwwLDAsMS0yLjQ4LDIuNDgsNS4zNyw1LjM3LDAsMCwxLTEuNzMuNTQsMywzLDAsMCwwLC45%0D%0AMS43NCw0LjEsNC4xLDAsMCwxLDEuMDYtLjUsNC4xNiw0LjE2LDAsMCwxLDEuODItLjEyLDMuNDIs%0D%0AMy40MiwwLDAsMSwuNjMuMTYsNC4wNiw0LjA2LDAsMCwxLC42MS0uODIsMTAsMTAsMCwwLDEsLjc2%0D%0ALS42OSwzLjExLDMuMTEsMCwwLDAsLjE1LTEsMy4yOSwzLjI5LDAsMCwwLS4zMS0xLjQxQTMuMDgs%0D%0AMy4wOCwwLDAsMCwyMS44OCwxNy41NlptNy45NCwxMS41NkEyLjE5LDIuMTksMCwwLDAsMzAuNywy%0D%0AOWEyLjI5LDIuMjksMCwwLDAsMS4yLTEuMiwyLjI5LDIuMjksMCwwLDAsLjE3LS44NywyLjE5LDIu%0D%0AMTksMCwwLDAtLjE4LS45LDIsMiwwLDAsMC0uNS0uNzIsMi4yMywyLjIzLDAsMCwwLS43My0uNDcs%0D%0AMi40NCwyLjQ0LDAsMCwwLS45LS4xNyw0LDQsMCwwLDAtMS4zMS0yLjQxLDQsNCwwLDAsMC0xLjE4%0D%0ALS43MSwzLjgxLDMuODEsMCwwLDAtMS4zOS0uMjUsMy42MywzLjYzLDAsMCwwLTEuMjguMjIsMy45%0D%0ALDMuOSwwLDAsMC0xLjEyLjYsNCw0LDAsMCwwLS44Ni45MywzLjg3LDMuODcsMCwwLDAtLjUzLDEu%0D%0AMTksMywzLDAsMCwwLS44Ni0uNTEsMi42NSwyLjY1LDAsMCwwLTEtLjE4LDIuNzQsMi43NCwwLDAs%0D%0AMC0xLjEuMjIsMi45MSwyLjkxLDAsMCwwLS44OS42LDIuNzMsMi43MywwLDAsMC0uNi45LDIuNjks%0D%0AMi42OSwwLDAsMC0uMjIsMS4wOSwyLjc0LDIuNzQsMCwwLDAsLjIyLDEuMSwyLjU5LDIuNTksMCww%0D%0ALDAsLjYuODksMi43NywyLjc3LDAsMCwwLC44OS42MSwyLjkxLDIuOTEsMCwwLDAsMS4xLjIxWiIv%0D%0APjwvc3ZnPg==";
                // partly cloudy w showers
                case 39:
                // mostly cloudy w showers
                case 40:
                // partly cloudy w thunderstorms
                case 41:
                // mostly cloudy w thunderstorms
                case 42:
                    // moonrain icon
                    return "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA0OCA0%0D%0AOCI+PHRpdGxlPndlYXRoZXJpY29uczwvdGl0bGU+PHBhdGggZD0iTTMwLjY3LDIzLjYxYTMuMzYs%0D%0AMy4zNiwwLDAsMSwxLC40NiwzLjMxLDMuMzEsMCwwLDEsLjguNzQsMy40MSwzLjQxLDAsMCwxLC43%0D%0AMSwyLjA3LDMuNCwzLjQsMCwwLDEtMSwyLjM4LDMuNDksMy40OSwwLDAsMS0xLjA4LjczLDMuNCwz%0D%0ALjQsMCwwLDEtMS4zMS4yNkgyNi40NWwxLjEyLTEuMTNoMi4yNUEyLjE5LDIuMTksMCwwLDAsMzAu%0D%0ANywyOWEyLjI5LDIuMjksMCwwLDAsMS4yLTEuMiwyLjI5LDIuMjksMCwwLDAsLjE3LS44NywyLjIz%0D%0ALDIuMjMsMCwwLDAtLjE4LS45LDIuMDgsMi4wOCwwLDAsMC0uNS0uNzEsMi40LDIuNCwwLDAsMC0x%0D%0ALjYyLS42NSwzLjkzLDMuOTMsMCwwLDAtLjQ1LTEuMzQsNC4wOSw0LjA5LDAsMCwwLS44Ni0xLjA3%0D%0ALDQsNCwwLDAsMC0xLjE4LS43MSwzLjgyLDMuODIsMCwwLDAtMS40LS4yNSwzLjYzLDMuNjMsMCww%0D%0ALDAtMS4yOC4yMiw0LjE5LDQuMTksMCwwLDAtMS4xMi42MSwzLjY1LDMuNjUsMCwwLDAtLjg2Ljkz%0D%0ALDMuOTIsMy45MiwwLDAsMC0uNTMsMS4xOCwzLDMsMCwwLDAtLjg2LS41MSwyLjY1LDIuNjUsMCww%0D%0ALDAtMS0uMTgsMi43NCwyLjc0LDAsMCwwLTEuMS4yMiwyLjkxLDIuOTEsMCwwLDAtLjg5LjYsMi43%0D%0AMywyLjczLDAsMCwwLS42LjksMi42OSwyLjY5LDAsMCwwLS4yMiwxLjA5LDIuNzQsMi43NCwwLDAs%0D%0AMCwuMjIsMS4xLDIuNTksMi41OSwwLDAsMCwuNi44OSwyLjc3LDIuNzcsMCwwLDAsLjg5LjYxLDIu%0D%0AOTEsMi45MSwwLDAsMCwxLjEuMjFoMi4yNUwyMiwzMC4yNUgyMC4yNmEzLjc4LDMuNzgsMCwwLDEt%0D%0AMS41NC0uMzEsMy45MywzLjkzLDAsMCwxLTIuNC0zLjYzLDMuNzksMy43OSwwLDAsMSwuMjEtMS4y%0D%0ANiwzLjUsMy41LDAsMCwxLTEtMS4xOCwzLjIzLDMuMjMsMCwwLDEtLjM1LTEuNDksMy4xOSwzLjE5%0D%0ALDAsMCwxLC4yNi0xLjMxQTMuNDksMy40OSwwLDAsMSwxNi4xOSwyMGEzLjI5LDMuMjksMCwwLDEs%0D%0AMi4zOC0xaC4xOUwxOSwxOWEzLjc2LDMuNzYsMCwwLDEsLjYxLS45NSw0LjI4LDQuMjgsMCwwLDEs%0D%0ALjgzLS43MSwzLjY3LDMuNjcsMCwwLDEsMS0uNDUsMy43NywzLjc3LDAsMCwxLDEuMTEtLjE2LDMu%0D%0AODcsMy44NywwLDAsMSwxLjQuMjYsNC4wOSw0LjA5LDAsMCwxLDEuMTkuNzFBMy44MywzLjgzLDAs%0D%0AMCwxLDI2LDE4LjhhMy43LDMuNywwLDAsMSwuNDQsMS4zNSw0Ljg5LDQuODksMCwwLDEsMS40MS4z%0D%0ANyw1LjE5LDUuMTksMCwwLDEsMS4yMi43Myw1LjQxLDUuNDEsMCwwLDEsMSwxLjA1QTQuODYsNC44%0D%0ANiwwLDAsMSwzMC42NywyMy42MVptLTkuMTItMWE0Ljc5LDQuNzksMCwwLDEsLjctLjkxLDQuNjgs%0D%0ANC42OCwwLDAsMSwuODgtLjczLDUuNDQsNS40NCwwLDAsMSwxLS41Miw1LjIzLDUuMjMsMCwwLDEs%0D%0AMS4xMi0uMjcsMi44NywyLjg3LDAsMCwwLS4zNS0uOTEsMi43NiwyLjc2LDAsMCwwLS42Mi0uNzMs%0D%0AMi44MywyLjgzLDAsMCwwLS44My0uNDcsMi42MywyLjYzLDAsMCwwLTEtLjE3LDIuNzMsMi43Myww%0D%0ALDAsMC0xLC4xOSwyLjY0LDIuNjQsMCwwLDAtLjg2LjU1LDIuODIsMi44MiwwLDAsMC0uNjIuODIs%0D%0AMi44NiwyLjg2LDAsMCwwLS4yOCwxLDIuNywyLjcsMCwwLDAtLjU1LS4yNCwyLDIsMCwwLDAtLjYt%0D%0ALjA5LDIuMTIsMi4xMiwwLDAsMC0uODcuMTgsMi4zOSwyLjM5LDAsMCwwLS43Mi40OCwyLjMzLDIu%0D%0AMzMsMCwwLDAtLjQ4LjcyLDIuMTMsMi4xMywwLDAsMC0uMTguODgsMi4xNywyLjE3LDAsMCwwLC4x%0D%0AOS45LDIuMTUsMi4xNSwwLDAsMCwuNTUuNzUsMy44OCwzLjg4LDAsMCwxLDMuMi0xLjY1QTQuMTUs%0D%0ANC4xNSwwLDAsMSwyMS41NSwyMi41OVptMSw5LjkxTDI0Ljc2LDI4aC0ybDEuNjktMy4zOGgybC0x%0D%0ALjEzLDIuMjZoMi44MVoiLz48L3N2Zz4=";
                // mostly cloudy w flurries
                case 43:
                // mostly cloudy w snow
                case 44:
                    // night snow
                    return "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA0OCA0%0D%0AOCI+PHRpdGxlPndlYXRoZXJpY29uczwvdGl0bGU+PHBhdGggZD0iTTMwLjY3LDIxLjYxYTMuMzYs%0D%0AMy4zNiwwLDAsMSwxLC40NiwzLjUyLDMuNTIsMCwwLDEsLjguNzQsMy40MSwzLjQxLDAsMCwxLC43%0D%0AMSwyLjA3QTMuMjgsMy4yOCwwLDAsMSwzMywyNi4wNWEzLjcyLDMuNzIsMCwwLDEtLjU5LDEsMy40%0D%0AMywzLjQzLDAsMCwxLS45Ljc1LDMuMjcsMy4yNywwLDAsMS0xLjEyLjQxVjI3LjA1YTIsMiwwLDAs%0D%0AMCwuNjgtLjMsMi4xNSwyLjE1LDAsMCwwLC41NC0uNSwyLjA5LDIuMDksMCwwLDAsLjM1LS42NCwy%0D%0ALjI0LDIuMjQsMCwwLDAsLjEyLS43MywyLjE5LDIuMTksMCwwLDAtLjE4LS45LDIsMiwwLDAsMC0u%0D%0ANS0uNzEsMi4yMywyLjIzLDAsMCwwLS43My0uNDcsMi4yNiwyLjI2LDAsMCwwLS45LS4xOCw0LDQs%0D%0AMCwwLDAtMS4zMS0yLjQxLDQsNCwwLDAsMC0xLjE4LS43MSwzLjgxLDMuODEsMCwwLDAtMS4zOS0u%0D%0AMjUsMy42MywzLjYzLDAsMCwwLTEuMjguMjIsNC4xOSw0LjE5LDAsMCwwLTEuMTIuNjEsMy42NSwz%0D%0ALjY1LDAsMCwwLS44Ni45MywzLjkyLDMuOTIsMCwwLDAtLjUzLDEuMTgsMywzLDAsMCwwLS44Ni0u%0D%0ANTEsMi42NSwyLjY1LDAsMCwwLTEtLjE4LDIuNzQsMi43NCwwLDAsMC0xLjEuMjIsMi45MSwyLjkx%0D%0ALDAsMCwwLS44OS42LDIuNzMsMi43MywwLDAsMC0uNi45LDIuNjksMi42OSwwLDAsMC0uMjIsMS4w%0D%0AOSwzLDMsMCwwLDAsLjExLjgxLDIuNTksMi41OSwwLDAsMCwuMzUuNzMsMywzLDAsMCwwLC41My42%0D%0ALDIuNjEsMi42MSwwLDAsMCwuNjkuNDR2MS4xOUEzLjc2LDMuNzYsMCwwLDEsMTgsMjcuNTNhNC4z%0D%0AMiw0LjMyLDAsMCwxLS44OS0uODcsNC4xLDQuMSwwLDAsMS0uNTctMS4xLDMuNzMsMy43MywwLDAs%0D%0AMS0uMjEtMS4yNSwzLjgzLDMuODMsMCwwLDEsLjIzLTEuMywzLjc3LDMuNzcsMCwwLDEsLjY0LTEu%0D%0AMTZBNC44MSw0LjgxLDAsMCwxLDE2LDIwLjY3YTQuMzMsNC4zMywwLDAsMS0uNjctMS41NCw0LjM0%0D%0ALDQuMzQsMCwwLDAsMSwuMTIsNC40NSw0LjQ1LDAsMCwwLDEuNzUtLjM1LDQuNzgsNC43OCwwLDAs%0D%0AMCwxLjQ0LTEsNC42LDQuNiwwLDAsMCwxLTEuNDQsNC40NSw0LjQ1LDAsMCwwLC4zNS0xLjc1LDQu%0D%0AMzQsNC4zNCwwLDAsMC0uMTItMSw0LjI1LDQuMjUsMCwwLDEsMS40LjU5LDQuMzgsNC4zOCwwLDAs%0D%0AMSwxLjExLDEsNC43NCw0Ljc0LDAsMCwxLC43MywxLjMsNC4zNiw0LjM2LDAsMCwxLC4yNiwxLjQ5%0D%0AYzAsLjA1LDAsLjEsMCwuMTVzMCwuMTEsMCwuMTVBNC43MSw0LjcxLDAsMCwxLDI1LDE4LjJhNC45%0D%0AMSw0LjkxLDAsMCwxLC44Ni0uMDgsNC44Myw0LjgzLDAsMCwxLDEuNTcuMjYsNS4xLDUuMSwwLDAs%0D%0AMSwxLjM5LjcyLDQuNzgsNC43OCwwLDAsMSwxLjEsMS4xQTQuNzIsNC43MiwwLDAsMSwzMC42Nywy%0D%0AMS42MVptLTkuMTItMWE1LDUsMCwwLDEsMS4zNy0xLjUsMy4zLDMuMywwLDAsMC0uMTYtMi4zOCwz%0D%0ALjA4LDMuMDgsMCwwLDAtLjg4LTEuMTUsNS4yMSw1LjIxLDAsMCwxLS41NCwxLjczLDUuNTgsNS41%0D%0AOCwwLDAsMS0yLjQ4LDIuNDgsNS4zNyw1LjM3LDAsMCwxLTEuNzMuNTQsMy4xOCwzLjE4LDAsMCww%0D%0ALC45MS43NSw0LjE1LDQuMTUsMCwwLDEsMS4wNi0uNTEsMy43NSwzLjc1LDAsMCwxLDEuMTYtLjE3%0D%0AQTQuMTUsNC4xNSwwLDAsMSwyMS41NSwyMC41OVptMi43LDhhLjUyLjUyLDAsMCwxLC4yOC40OC41%0D%0ANC41NCwwLDAsMS0uMTcuNC41NC41NCwwLDAsMS0uMzkuMTcuNjguNjgsMCwwLDEtLjIzLS4wNSwx%0D%0ALjM0LDEuMzQsMCwwLDEtLjI0LS4xMmwtLjIzLS4xNC0uMi0uMTJjMCwuMTEsMCwuMjQsMCwuMzhh%0D%0AMS40NCwxLjQ0LDAsMCwxLDAsLjQyLjczLjczLDAsMCwxLS4xNi4zMy40Ni40NiwwLDAsMS0uMzgu%0D%0AMTQuNDcuNDcsMCwwLDEtLjM5LS4xNEEuNzMuNzMsMCwwLDEsMjIsMzBhMS40NCwxLjQ0LDAsMCwx%0D%0ALDAtLjQyYzAtLjE0LDAtLjI3LDAtLjM4bC0uMi4xMi0uMjMuMTRhMS4zNCwxLjM0LDAsMCwxLS4y%0D%0ANC4xMi42OC42OCwwLDAsMS0uMjMuMDUuNTYuNTYsMCwwLDEtLjQtLjE3LjU0LjU0LDAsMCwxLS4x%0D%0ANy0uNC40Ni40NiwwLDAsMSwuMDgtLjI4LjU4LjU4LDAsMCwxLC4yMS0uMmwuNjEtLjM2LS42MS0u%0D%0AMzZhLjU4LjU4LDAsMCwxLS4yMS0uMi40Ni40NiwwLDAsMS0uMDgtLjI4LjUxLjUxLDAsMCwxLC4x%0D%0ANy0uNC41NC41NCwwLDAsMSwuMzktLjE3LjY5LjY5LDAsMCwxLC4yNC4wNSwxLjM0LDEuMzQsMCww%0D%0ALDEsLjI0LjEybC4yMy4xNC4yLjEyYzAtLjExLDAtLjI0LDAtLjM4YTEuNDQsMS40NCwwLDAsMSww%0D%0ALS40Mi43My43MywwLDAsMSwuMTYtLjMzLjQ3LjQ3LDAsMCwxLC4zOS0uMTQuNDYuNDYsMCwwLDEs%0D%0ALjM4LjE0LjczLjczLDAsMCwxLC4xNi4zMywxLjQ0LDEuNDQsMCwwLDEsMCwuNDJjMCwuMTQsMCwu%0D%0AMjcsMCwuMzhsLjItLjEyTDIzLjUsMjdhMS4zNCwxLjM0LDAsMCwxLC4yNC0uMTIuNjkuNjksMCww%0D%0ALDEsLjI0LS4wNS41Ni41NiwwLDAsMSwuNTUuNTcuNTIuNTIsMCwwLDEtLjI4LjQ4bC0uNjIuMzZa%0D%0AbTQuMjMtMi44OWEuNS41LDAsMCwxLC4zOS4xNy41My41MywwLDAsMSwuMTYuMzkuNTIuNTIsMCww%0D%0ALDEtLjI4LjQ4bC0uNjIuMzYuNjIuMzdBLjUyLjUyLDAsMCwxLDI5LDI4YS41NC41NCwwLDAsMS0u%0D%0AMTcuNC41My41MywwLDAsMS0uMzkuMTYuNS41LDAsMCwxLS4yMy0uMDUsMS4zNCwxLjM0LDAsMCwx%0D%0ALS4yNC0uMTJsLS4yMy0uMTQtLjItLjEyYzAsLjExLDAsLjI0LDAsLjM5YTEuMzksMS4zOSwwLDAs%0D%0AMSwwLC40MS43Mi43MiwwLDAsMS0uMTYuMzQuNDkuNDksMCwwLDEtLjM4LjE0LjUuNSwwLDAsMS0u%0D%0AMzktLjE0LjcyLjcyLDAsMCwxLS4xNi0uMzQsMS4zOSwxLjM5LDAsMCwxLDAtLjQxYzAtLjE1LDAt%0D%0ALjI4LDAtLjM5bC0uMi4xMi0uMjMuMTRhMS4zNCwxLjM0LDAsMCwxLS4yNC4xMi41LjUsMCwwLDEt%0D%0ALjIzLjA1LjUyLjUyLDAsMCwxLS40LS4xN0EuNTQuNTQsMCwwLDEsMjUsMjhhLjQ0LjQ0LDAsMCwx%0D%0ALC4wOC0uMjguNTguNTgsMCwwLDEsLjIxLS4ybC42MS0uMzctLjYxLS4zNmEuNTguNTgsMCwwLDEt%0D%0ALjIxLS4yLjQ0LjQ0LDAsMCwxLS4wOC0uMjguNTguNTgsMCwwLDEsLjU2LS41Ni41Mi41MiwwLDAs%0D%0AMSwuMjQuMDUsMS4zNCwxLjM0LDAsMCwxLC4yNC4xMmwuMjMuMTQuMi4xMmMwLS4xMSwwLS4yNCww%0D%0ALS4zOWExLjM5LDEuMzksMCwwLDEsMC0uNDEuNzIuNzIsMCwwLDEsLjE2LS4zNC41LjUsMCwwLDEs%0D%0ALjM5LS4xMy40OS40OSwwLDAsMSwuMzguMTMuNzIuNzIsMCwwLDEsLjE2LjM0LDEuMzksMS4zOSww%0D%0ALDAsMSwwLC40MWMwLC4xNSwwLC4yOCwwLC4zOWwuMi0uMTIuMjMtLjE0YTEuMzQsMS4zNCwwLDAs%0D%0AMSwuMjQtLjEyQS41Mi41MiwwLDAsMSwyOC40OCwyNS43MloiLz48L3N2Zz4=";
                default:
                    return "";
            }
        }

        private Task<DialogTurnResult> End(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return stepContext.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string GeographyPrompt = "geographyPrompt";
            public const string GetForecastResponseDialog = "getForecastResponseDialog";
        }
    }
}
