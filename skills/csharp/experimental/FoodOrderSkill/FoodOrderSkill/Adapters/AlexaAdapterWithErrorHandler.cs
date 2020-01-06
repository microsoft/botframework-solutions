using Bot.Builder.Community.Adapters.Alexa;
using Bot.Builder.Community.Adapters.Alexa.Middleware;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Extensions.Logging;

namespace FoodOrderSkill.Adapters
{
    public class AlexaAdapterWithErrorHandler : AlexaAdapter
    {
        public AlexaAdapterWithErrorHandler(
            ILogger<AlexaAdapter> logger,
            UserState userState,
            ConversationState conversationState)
            : base(new AlexaAdapterOptions() { ShouldEndSessionByDefault = false, TryConcatMultipleTextActivties = true }, logger)
        {
            // Adapter.Use(new AlexaIntentRequestToMessageActivityMiddleware());
            OnTurnError = async (turnContext, exception) =>
            {
                // Log any leaked exception from the application.
                logger.LogError($"Exception caught : {exception.Message}");

                // Send a catch-all apology to the user.
                await turnContext.SendActivityAsync("Sorry, it looks like something went wrong.");
            };

            Use(new AlexaRequestToMessageEventActivitiesMiddleware());
        }
    }
}
