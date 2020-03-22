namespace ITSMSkill.Controllers.ServiceNow
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;


    public interface IMessageReceiver<in T>
    {
        /// <summary>
        /// Create an Event Activity from an incoming event request and sends to Virtual Assistant.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <param name="cancellationToken">The CancellationToken.</param>
        /// <returns>The task of event processing.</returns>
        Task<ServiceResponse> Receive(T request, CancellationToken cancellationToken);
    }
}
