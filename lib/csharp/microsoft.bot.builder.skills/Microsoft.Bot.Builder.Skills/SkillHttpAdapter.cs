using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// This adapter is responsible for accepting a bot-to-bot call over http transport.
    /// It'll perform the following tasks:
    /// 1. Authentication.
    /// 2. Call SkillHttpBotAdapter to process the incoming activity.
    /// </summary>
    public class SkillHttpAdapter : IBotFrameworkHttpAdapter
    {
        private readonly ICredentialProvider _credentialProvider;
        private readonly IAuthenticationProvider _authenticationProvider;
        private readonly ILogger _logger;
        private readonly IActivityHandler _skillHttpBotAdapter;
        private readonly JsonSerializer botMessageSerializer = JsonSerializer.Create(Serialization.Settings);

        public SkillHttpAdapter(IServiceProvider serviceProvider)
        {
            _skillHttpBotAdapter = serviceProvider.GetService<SkillHttpBotAdapter>() ?? throw new ArgumentNullException(nameof(SkillHttpBotAdapter));
            _credentialProvider = serviceProvider.GetService<ICredentialProvider>() ?? throw new ArgumentNullException(nameof(ICredentialProvider));
            _authenticationProvider = serviceProvider.GetService<IAuthenticationProvider>();
            _logger = serviceProvider.GetService<ILogger>();
        }

        public async Task ProcessAsync(HttpRequest httpRequest, HttpResponse httpResponse, IBot bot, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (httpRequest == null)
            {
                throw new ArgumentNullException(nameof(httpRequest));
            }

            if (httpResponse == null)
            {
                throw new ArgumentNullException(nameof(httpResponse));
            }

            if (bot == null)
            {
                throw new ArgumentNullException(nameof(bot));
            }

            if (_authenticationProvider != null)
            {
                var authenticated = await _authenticationProvider.AuthenticateAsync(httpRequest.Headers["Authorization"], _credentialProvider);

                if (!authenticated)
                {
                    httpResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return;
                }
            }

            // deserialize the incoming Activity
            var activity = ReadRequest(httpRequest);

            var cancellationTokenSource = new CancellationTokenSource();

            // process the inbound activity with the bot
            var invokeResponse = await _skillHttpBotAdapter.ProcessActivityAsync(activity, bot.OnTurnAsync, cancellationTokenSource.Token).ConfigureAwait(false);

            // trigger cancel token after activity is handled. this will stop the typing indicator
            cancellationTokenSource.Cancel();

            // write the response, potentially serializing the InvokeResponse
            WriteResponse(httpResponse, invokeResponse);
        }

        private Activity ReadRequest(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var activity = default(Activity);

            using (var bodyReader = new JsonTextReader(new StreamReader(request.Body, Encoding.UTF8)))
            {
                activity = botMessageSerializer.Deserialize<Activity>(bodyReader);
            }

            return activity;
        }

        private void WriteResponse(HttpResponse response, InvokeResponse invokeResponse)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (invokeResponse == null)
            {
                response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                response.ContentType = "application/json";
                response.StatusCode = invokeResponse.Status;

                using (var writer = new StreamWriter(response.Body))
                {
                    using (var jsonWriter = new JsonTextWriter(writer))
                    {
                        botMessageSerializer.Serialize(jsonWriter, invokeResponse.Body);
                    }
                }
            }
        }
    }
}