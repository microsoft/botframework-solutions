=using Bot.Builder.Community.Adapters.Google;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualAssistantSample.Adapters
{
    public class GoogleAdapterWithErrorHandler : GoogleAdapter
    {
        public GoogleAdapterWithErrorHandler(ILogger<GoogleAdapter> logger, GoogleAdapterOptions adapterOptions)
        : base(adapterOptions, logger)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                // Log any leaked exception from the application.
                logger.LogError($"Exception caught : {exception.Message}");

                // Send a catch-all apology to the user.
                await turnContext.SendActivityAsync("Sorry, it looks like something went wrong.");
            };
        }
    }
}
