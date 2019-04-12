using Microsoft.Bot.Builder.Skills.Auth;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Skills.Tests.Mocks
{
    public class DummyMicrosoftAppCredentialsEx : MicrosoftAppCredentialsEx
    {
        public DummyMicrosoftAppCredentialsEx(string appId, string password, string scope) : base(appId, password, scope)
        {
        }

        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
