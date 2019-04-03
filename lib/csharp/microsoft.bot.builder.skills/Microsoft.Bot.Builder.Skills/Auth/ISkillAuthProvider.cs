using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Builder.Skills.Auth
{
    public interface ISkillAuthProvider
    {
        bool Authenticate(HttpContext httpContext);
    }
}