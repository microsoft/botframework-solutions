// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions.Skills.Auth;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Solutions.Skills
{
    /// <summary>
    /// This is the default Controller that contains APIs for handling
    /// calls from a channel and calls from a parent bot (to a skill bot).
    /// </summary>
    public abstract class SkillController : ControllerBase
    {
        private readonly IAuthenticator _authenticator;
        private readonly IBot _bot;
        private readonly IBotFrameworkHttpAdapter _botFrameworkHttpAdapter;
        private readonly BotSettingsBase _botSettings;
        private readonly JsonSerializer _jsonSerializer = JsonSerializer.Create();

        protected SkillController(
            IBot bot,
            BotSettingsBase botSettings,
            IBotFrameworkHttpAdapter botFrameworkHttpAdapter,
            IWhitelistAuthenticationProvider whitelistAuthenticationProvider)
        {
            _bot = bot ?? throw new ArgumentNullException(nameof(bot));
            _botSettings = botSettings ?? throw new ArgumentNullException(nameof(botSettings));
            _botFrameworkHttpAdapter = botFrameworkHttpAdapter ?? throw new ArgumentNullException(nameof(botFrameworkHttpAdapter));
            if (whitelistAuthenticationProvider == null)
            {
                throw new ArgumentNullException(nameof(whitelistAuthenticationProvider));
            }

            var authenticationProvider = new MSJwtAuthenticationProvider(_botSettings.MicrosoftAppId);
            _authenticator = new Authenticator(authenticationProvider, whitelistAuthenticationProvider);
        }

        // Each skill provides a template manifest file which we use to fill in the dynamic elements.
        // There are protected to enable unit tests to mock.
        protected HttpClient HttpClient { get; set; } = new HttpClient();

        protected string ManifestTemplateFilename { get; set; } = "manifestTemplate.json";

        /// <summary>
        /// This API is the endpoint for when a bot receives a message from a channel or a parent bot.
        /// </summary>
        /// <returns>Task.</returns>
        [Route("api/messages")]
        [HttpPost]
        [HttpGet]
        public async Task BotMessage()
        {
            await _botFrameworkHttpAdapter.ProcessAsync(Request, Response, _bot).ConfigureAwait(false);
        }

        /// <summary>
        /// This API is the manifest endpoint that surfaces a self-describing document of the Skill.
        /// The base template is provided by the template and at runtime we fill in endpoint, msaAppId
        /// and as requested the triggering utterances inline within the manifest.
        /// </summary>
        /// <param name="inlineTriggerUtterances">Include triggering utterances inline in manifest.</param>
        /// <returns>Task.</returns>
        [Route("api/skill/manifest")]
        [HttpGet]
        public async Task SkillManifest([Bind, FromQuery] bool inlineTriggerUtterances = false)
        {
            try
            {
                var skillUriBase = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
                var manifestGenerator = new SkillManifestGenerator(HttpClient);
                var skillManifest = await manifestGenerator.GenerateManifest(ManifestTemplateFilename, _botSettings.MicrosoftAppId, _botSettings.CognitiveModels, skillUriBase, inlineTriggerUtterances).ConfigureAwait(false);

                Response.ContentType = "application/json";
                Response.StatusCode = 200;

                using (var writer = new StreamWriter(Response.Body))
                {
                    using (var jsonWriter = new JsonTextWriter(writer))
                    {
                        _jsonSerializer.Serialize(jsonWriter, skillManifest);
                    }
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types (disabling it, used to log the exception before returning it)
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Response.ContentType = "application/json";
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                using (var writer = new StreamWriter(Response.Body))
                {
                    using (var jsonWriter = new JsonTextWriter(writer))
                    {
                        _jsonSerializer.Serialize(jsonWriter, ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// This API is for calling bots to see if a skill is live
        /// Additional it can also be used to verify if calling bot can pass authentication
        /// because this API verifies the bearer token in the process.
        /// </summary>
        /// <returns>Task.</returns>
        [Route("api/skill/ping")]
        [HttpGet]
        public async Task SkillPing()
            => await _authenticator.Authenticate(Request, Response).ConfigureAwait(false);
    }
}
