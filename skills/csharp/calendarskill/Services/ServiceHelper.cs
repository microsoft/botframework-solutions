// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CalendarSkill.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Xml;
    using Microsoft.Bot.Builder.Skills;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Graph;
    using Newtonsoft.Json;
    using CalendarSkill.Models;
    using CalendarSkill.Responses.Shared;

    /// <summary>
    /// To Do skill helper class.
    /// </summary>
    public class ServiceHelper
    {
        private const string APIErrorAccessDenied = "erroraccessdenied";
        private const string APIErrorMessageSubmissionBlocked = "errormessagesubmissionblocked";

        private static readonly Regex ComplexTokensRegex = new Regex(@"\{[^{\}]+(?=})\}", RegexOptions.Compiled);
        private static HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Generate httpClient.
        /// </summary>
        /// <param name="accessToken">API access token.</param>
        /// <returns>Generated httpClient.</returns>
        public static HttpClient GetHttpClient(string accessToken)
        {
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return httpClient;
        }

        public static ServiceException GenerateServiceException(dynamic errorResponse)
        {
            var errorObject = errorResponse.error;
            Error error = new Error();
            error.Code = errorObject.code.ToString();
            error.Message = errorObject.message.ToString();
            return new ServiceException(error);
        }

        public static SkillException HandleGraphAPIException(ServiceException ex)
        {
            var skillExceptionType = SkillExceptionType.Other;
            if (ex.Error.Code.Equals(APIErrorAccessDenied, StringComparison.InvariantCultureIgnoreCase))
            {
                skillExceptionType = SkillExceptionType.APIAccessDenied;
            }
            else if (ex.Error.Code.Equals(APIErrorMessageSubmissionBlocked, StringComparison.InvariantCultureIgnoreCase))
            {
                skillExceptionType = SkillExceptionType.AccountNotActivated;
            }

            return new SkillException(skillExceptionType, ex.Message, ex);
        }
    }
}