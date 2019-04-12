// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// This is the default Controller that contains APIs for handling
    /// calls from a channel and calls from a parent bot (to a skill bot).
    /// </summary>
    [Authorize]
    public abstract class SkillController : ControllerBase
    {
        private readonly IBot _bot;
        private readonly IBotFrameworkHttpAdapter _botFrameworkHttpAdapter;
        private readonly SkillAdapter _skillAdapter;
        private readonly ISkillAuthProvider _skillAuthProvider;
        private readonly BotSettingsBase _botSettings;
        private readonly JsonSerializer _jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            ContractResolver = new ReadOnlyJsonContractResolver(),
            Converters = new List<JsonConverter> { new Iso8601TimeSpanConverter() },
        });

        private string manifestTemplateFilename = "manifestTemplate.json";
        private HttpClient httpClient = new HttpClient();

        public SkillController(IServiceProvider serviceProvider, BotSettingsBase botSettings)
        {
            _botFrameworkHttpAdapter = serviceProvider.GetService<IBotFrameworkHttpAdapter>() ?? throw new ArgumentNullException(nameof(IBotFrameworkHttpAdapter));
            _skillAdapter = serviceProvider.GetService<SkillAdapter>() ?? throw new ArgumentNullException(nameof(SkillAdapter));
            _bot = serviceProvider.GetService<IBot>() ?? throw new ArgumentNullException(nameof(IBot));
            _skillAuthProvider = serviceProvider.GetService<ISkillAuthProvider>();
            _botSettings = botSettings;
        }

        // Each skill provides a template manifest file which we use to fill in the dynamic elements.
        // There are protected to enable unit tests to mock.
        protected HttpClient HttpClient { get => httpClient; set => httpClient = value; }

        protected string ManifestTemplateFilename { get => manifestTemplateFilename; set => manifestTemplateFilename = value; }

        /// <summary>
        /// This API is the endpoint for when a bot receives a message from a channel or a parent bot
        /// </summary>
        /// <returns>Task.</returns>
        [Route("api/messages")]
        [HttpPost]
        [AllowAnonymous]
        public async Task BotMessage()
        {
            await _botFrameworkHttpAdapter.ProcessAsync(Request, Response, _bot, default(CancellationToken));
        }

        /// <summary>
        /// This API is the endpoint the bot exposes as skill.
        /// </summary>
        /// <returns>Task.</returns>
        [Route("api/skill/messages")]
        [HttpPost]
        public async Task SkillMessage()
        {
            if (_skillAuthProvider != null && !_skillAuthProvider.Authenticate(HttpContext))
            {
                Response.StatusCode = 401;
                return;
            }

            await _skillAdapter.ProcessAsync(Request, Response, _bot, default(CancellationToken));
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
        [AllowAnonymous]
        public async Task SkillManifest([Bind, FromQuery] bool inlineTriggerUtterances = false)
        {
            try
            {
                string skillUriBase = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

                SkillManifestGenerator manifestGenerator = new SkillManifestGenerator(HttpClient);
                var skillManifest = await manifestGenerator.GenerateManifest(ManifestTemplateFilename, _botSettings.MicrosoftAppId, _botSettings.CognitiveModels, skillUriBase, inlineTriggerUtterances);

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
            catch (Exception e)
            {
                Response.ContentType = "application/json";
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                using (var writer = new StreamWriter(Response.Body))
                {
                    using (var jsonWriter = new JsonTextWriter(writer))
                    {
                        _jsonSerializer.Serialize(jsonWriter, e.Message);
                    }
                }
            }
        }
    }
}