using Microsoft.Bot.Builder.AI.LanguageGeneration.Resolver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Solutions.Util
{
    public static class LanguageGenerationUtilities
    {
        /// <summary>
        /// Creates an instance of <see cref="LanguageGenerationResolver"/>.
        /// </summary>
        /// <param name="applicationId">Language generation application id.</param>
        /// <param name="endpointKey">The language generation service subscription key.</param>
        /// <param name="endpointRegion">The language generation region.</param>
        /// <returns>An instance of the language generation resolver.</returns>
        public static LanguageGenerationResolver CreateResolver(string applicationId, string endpointKey, string endpointRegion)
        {
            if (string.IsNullOrEmpty(applicationId))
            {
                throw new ArgumentException("Application id can't be null or empty.", nameof(applicationId));
            }

            if (string.IsNullOrEmpty(endpointKey))
            {
                throw new ArgumentException("Endpoint subscription key can't be null or empty.", nameof(applicationId));
            }

            if (string.IsNullOrEmpty(endpointRegion))
            {
                // Set default region to westus
                endpointRegion = "westus";
            }

            var languageGenerationEndpoint = $"https://{endpointRegion}.cts.speech.microsoft.com/v1/lg";
            var tokenIssuingEndpoint = $"https://{endpointRegion}.api.cognitive.microsoft.com/sts/v1.0/issueToken";

            var application = new LanguageGenerationApplication(applicationId, endpointKey, languageGenerationEndpoint);
            var options = new LanguageGenerationOptions
            {
                TokenGenerationApiEndpoint = tokenIssuingEndpoint,
            };

            return new LanguageGenerationResolver(application, options);
        }
    }
}