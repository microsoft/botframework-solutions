﻿using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Bot.Builder.Skills.Auth
{
    public interface IAuthenticator
    {
        Task<ClaimsIdentity> Authenticate(HttpRequest httpRequest, HttpResponse httpResponse);
    }
}