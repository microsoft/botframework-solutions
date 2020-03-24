// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ITSMSkill.Controllers.ServiceNow
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>Represents the response from a service call.</summary>
    public class ServiceResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceResponse"/> class.</summary>
        /// <param name="code">indicates the overall status of the operation.</param>
        /// <param name="message">provides details about the status of the operation.</param>
        public ServiceResponse(
            HttpStatusCode code,
            string message)
        {
            this.Message = message;
            this.Code = code;
        }

        /// <summary>Gets a code indicating the overall status of the operation.</summary>
        public HttpStatusCode Code { get; }

        /// <summary>Gets a message that provides details about the status of the operation.</summary>
        public string Message { get; }
    }
}
