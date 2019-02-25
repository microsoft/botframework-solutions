// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Middleware
{
    /// <summary>
    /// A Middleware for running the Luis recognizer.
    /// This could eventually be generalized and moved to the core Bot Builder library
    /// in order to support multiple recognizers.
    /// </summary>
    public class LuisRecognizerMiddleware : IMiddleware
    {
        /// <summary>
        /// The service key to use to retrieve recognition results.
        /// </summary>
        public const string LuisRecognizerResultKey = "LuisRecognizerResult";

        /// <summary>
        /// The value type for a LUIS trace activity.
        /// </summary>
        public const string LuisTraceType = "https://www.luis.ai/schemas/trace";

        /// <summary>
        /// The context label for a LUIS trace activity.
        /// </summary>
        public const string LuisTraceLabel = "Luis Trace";

        /// <summary>
        /// A string used to obfuscate the LUIS subscription key.
        /// </summary>
        public const string Obfuscated = "****";

        private readonly LuisApplication luisModel;

        private readonly IRecognizer luisRecognizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisRecognizerMiddleware"/> class.
        /// </summary>
        /// <param name="luisModel">The LUIS model to use to recognize text.</param>
        /// <param name="luisRecognizerOptions">The LUIS recognizer options to use.</param>
        /// <param name="luisOptions">The LUIS request options to use.</param>
        public LuisRecognizerMiddleware(LuisApplication luisModel, LuisPredictionOptions luisRecognizerOptions = null)
        {
            this.luisModel = luisModel ?? throw new ArgumentNullException(nameof(luisModel));

            this.luisRecognizer = new LuisRecognizer(luisModel, luisRecognizerOptions, true);
        }

        /// <summary>
        /// Processess an incoming activity.
        /// </summary>
        /// <param name="context">The context object for this turn.</param>
        /// <param name="next">The delegate to call to continue the bot middleware pipeline.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext context, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            BotAssert.ContextNotNull(context);

            if (context.Activity.Type == ActivityTypes.Message)
            {
                var result = await this.luisRecognizer.RecognizeAsync(context, cancellationToken).ConfigureAwait(false);
                context.TurnState.Add(LuisRecognizerResultKey, result);
            }

            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}