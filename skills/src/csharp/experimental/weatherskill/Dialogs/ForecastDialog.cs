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

            // if state.location exists, call api
            // if not, textprompt and validate against luis geogrpahy entity result. Retry if still missing (3 x)?
            // call api to get location code
            // call1 day forecast api
            // show adaptive card

            var sample = new WaterfallStep[]
            {
                RouteToGeographyPromptOrServiceCall,
                GeographyPrompt,
                GetWeatherResponse,
                End,
            };

            AddDialog(new WaterfallDialog(nameof(ForecastDialog), sample));
            AddDialog(new TextPrompt(DialogIds.GeographyPrompt, GeographyLuisValidatorAsync));

            InitialDialogId = nameof(ForecastDialog);
        }

        private async Task<DialogTurnResult> RouteToGeographyPromptOrServiceCall(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context);
            var geography = state.Geography;

            if (string.IsNullOrEmpty(geography))
            {
                return await stepContext.NextAsync();

            }
            else
            {
                // TODO: Route to API call
                return await stepContext.NextAsync();
            }
        }

        private async Task<DialogTurnResult> GeographyPrompt(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var prompt = ResponseManager.GetResponse(SharedResponses.LocationPrompt);
            return await stepContext.PromptAsync(DialogIds.GeographyPrompt, new PromptOptions { Prompt = prompt });
        }



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

        private async Task<DialogTurnResult> GetWeatherResponse(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context);

            var service = new AccuweatherService();
            state.GeographyLocation = await service.GetLocationByQueryAsync(state.Geography);

            var forecastResponse = await service.GetOneDayForecastAsync(state.GeographyLocation.Key);
            var tokens = new StringDictionary
            {
                { "Name", stepContext.Result.ToString() },
            };

            var response = ResponseManager.GetResponse(SampleResponses.HaveNameMessage, tokens);
            await stepContext.Context.SendActivityAsync(response);

            return await stepContext.NextAsync();
        }

        private Task<DialogTurnResult> End(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return stepContext.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string GeographyPrompt = "geographyPrompt";
        }
    }
}
