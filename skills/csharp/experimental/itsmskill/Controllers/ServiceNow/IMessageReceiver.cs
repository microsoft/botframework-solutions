// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ITSMSkill.Controllers.ServiceNow
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IMessageReceiver<in T>
    {
        /// <summary>
        /// Create an Event Activity from an incoming event request.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <param name="cancellationToken">The CancellationToken.</param>
        /// <returns>The task of event processing.</returns>
        Task<ServiceResponse> Receive(T request, CancellationToken cancellationToken);
    }
}
