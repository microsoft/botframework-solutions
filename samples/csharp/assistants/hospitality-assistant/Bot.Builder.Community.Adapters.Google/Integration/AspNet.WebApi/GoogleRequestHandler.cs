// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;
using Microsoft.Bot.Builder;

namespace Bot.Builder.Community.Adapters.Google.Integration.AspNet.WebApi
{
    internal sealed class GoogleRequestHandler : HttpMessageHandler
    {
        public static readonly MediaTypeFormatter[] googleMessageMediaTypeFormatters = {
            new JsonMediaTypeFormatter
            {
                SerializerSettings =
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                }
            }
        };

        private readonly GoogleAdapter _googleAdapter;
        private readonly GoogleOptions _googleOptions;

        public GoogleRequestHandler(GoogleAdapter googleAdapter, GoogleOptions googleOptions)
        {
            _googleAdapter = googleAdapter;
            _googleOptions = googleOptions;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Post)
            {
                return request.CreateResponse(HttpStatusCode.MethodNotAllowed);
            }

            var requestContentHeaders = request.Content.Headers;

            if (requestContentHeaders.ContentLength == 0)
            {
                return request.CreateErrorResponse(HttpStatusCode.BadRequest, "Request body should not be empty.");
            }

            try
            {
                return await ProcessMessageRequestAsync(
                    request,
                    _googleAdapter,
                    context =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        IBot bot;

                        try
                        {
                            bot = (IBot)request.GetDependencyScope().GetService(typeof(IBot));
                        }
                        catch
                        {
                            bot = null;
                        }

                        if (bot == null)
                        {
                            throw new InvalidOperationException($"Did not find an {typeof(IBot).Name} service via the dependency resolver. Please make sure you have registered your bot with your dependency injection container.");
                        }

                        return bot.OnTurnAsync(context);
                    },
                    cancellationToken);
            }
            catch (UnauthorizedAccessException e)
            {
                return request.CreateErrorResponse(HttpStatusCode.Unauthorized, e.Message);
            }
            catch (InvalidOperationException e)
            {
                return request.CreateErrorResponse(HttpStatusCode.NotFound, e.Message);
            }
        }

        public async Task<HttpResponseMessage> ProcessMessageRequestAsync(HttpRequestMessage request, GoogleAdapter googleAdapter, Func<ITurnContext, Task> botCallbackHandler, CancellationToken cancellationToken)
        {
            GoogleRequestBody skillRequest;
            Payload actionPayload;

            byte[] requestByteArray;

            try
            {
                requestByteArray = await request.Content.ReadAsByteArrayAsync();
                skillRequest = await request.Content.ReadAsAsync<GoogleRequestBody>(googleMessageMediaTypeFormatters, cancellationToken);
                actionPayload = skillRequest.OriginalDetectIntentRequest.Payload;
            }
            catch (Exception)
            {
                try
                {
                    actionPayload = await request.Content.ReadAsAsync<Payload>(googleMessageMediaTypeFormatters, cancellationToken);
                }
                catch (Exception e)
                {
                    throw new JsonSerializationException("Invalid JSON received");
                }
            }

            var GoogleResponseBody = await googleAdapter.ProcessActivity(
                actionPayload,
                _googleOptions,
                null);

            if (GoogleResponseBody == null)
            {
                return null;
            }

            var GoogleResponseBodyJson = JsonConvert.SerializeObject(GoogleResponseBody, Formatting.None,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
                });

            var response = request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(GoogleResponseBodyJson);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return response;
        }
    }
}
