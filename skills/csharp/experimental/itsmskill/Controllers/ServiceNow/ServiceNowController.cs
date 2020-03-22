namespace ITSMSkill.Controllers.ServiceNow
{
    using ITSMSkill.Models.ServiceNow;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    [Route("api/servicenow")]
    [ApiController]
    public class ServiceNowController : ServiceNowControllerBase
    {
        public ServiceNowController(
            IBotFrameworkHttpAdapter httpAdapter,
            IBot bot,
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
