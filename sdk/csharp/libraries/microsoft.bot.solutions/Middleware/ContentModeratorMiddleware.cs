// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.ContentModerator;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Middleware
{
    /// <summary>
    /// Middleware component to run Content Moderator Service on all incoming activities.
    /// </summary>
    public class ContentModeratorMiddleware : IMiddleware
    {
        /// <summary>
        /// Key for Text Moderator result in Bot Context dictionary.
        /// </summary>
        public const string ServiceName = "ContentModerator";

        /// <summary>
        /// Key for Text Moderator result in Bot Context dictionary.
        /// </summary>
        public const string TextModeratorResultKey = "TextModeratorResult";

        /// <summary>
        /// Content Moderator service key.
        /// </summary>
        private readonly string subscriptionKey;

        /// <summary>
        /// Content Moderator service region.
        /// </summary>
        private readonly string region;

        private readonly ContentModeratorClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentModeratorMiddleware"/> class.
        /// </summary>
        /// <param name="subscriptionKey">Azure Service Key.</param>
        /// <param name="region">Azure Service Region.</param>
        /// <param name="client">Content Middleware Client.</param>
        public ContentModeratorMiddleware(string subscriptionKey, string region, IContentModeratorClient client)
        {
            this.subscriptionKey = subscriptionKey ?? throw new ArgumentNullException(nameof(subscriptionKey));
            this.region = region ?? throw new ArgumentNullException(nameof(region));
            this.client = (ContentModeratorClient)(client ?? new ContentModeratorClient(new ApiKeyServiceClientCredentials(this.subscriptionKey)));
        }

        /// <summary>
        /// Analyzes activity text with Content Moderator and adds result to Bot Context. Run on each turn of the conversation.
        /// </summary>
        /// <param name="context">The Bot Context object.</param>
        /// <param name="next">The next middleware component to run.</param>
        /// <param name="cancellationToken">The cancellation token for the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext context, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            BotAssert.ContextNotNull(context);

            if (context.Activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(context.Activity.Text))
            {
                var byteArray = Encoding.UTF8.GetBytes(context.Activity.Text);
                var textStream = new MemoryStream(byteArray);

                var region = this.region.StartsWith("https://") ? this.region : $"https://{this.region}";
                client.Endpoint = $"{region}.api.cognitive.microsoft.com";

                var screenResult = client.TextModeration.ScreenText(
                    textContentType: "text/plain",
                    textContent: textStream,
                    autocorrect: true,
                    pII: true,
                    listId: null,
                    classify: true);

                context.TurnState.Add(TextModeratorResultKey, screenResult);
            }

            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}