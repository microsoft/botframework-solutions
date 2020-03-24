// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ITSMSkill.Controllers.ServiceNow
{
    using System.Threading;
    using System.Threading.Tasks;
    using ITSMSkill.Models.ServiceNow;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;

    /// <summary>
    /// Controller to Process ServiceNow Events
    /// </summary>
    [Route("api/servicenow")]
    [ApiController]
    public class ServiceNowController : ServiceNowControllerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceNowController"/> class.
        /// </summary>
        /// <param name="messageReceiver">Botframework Adapter.</param>
        /// <param name="telemetryClient">The Assistant.</param>
        public ServiceNowController(
            IMessageReceiver<ServiceNowNotification> messageReceiver,
            IBotTelemetryClient telemetryClient)
            : base(messageReceiver, telemetryClient)
        {
        }

        [HttpPost]
        public Task<IActionResult> Post([FromBody] string request, CancellationToken cancellationToken)
        {
            var notiication = JsonConvert.DeserializeObject<ServiceNowNotification>(request);
            return this.Post(notiication, cancellationToken);
        }
    }
}
