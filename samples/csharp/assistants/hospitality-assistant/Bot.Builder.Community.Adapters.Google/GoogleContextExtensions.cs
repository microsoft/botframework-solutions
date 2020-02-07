using System;
using System.Collections.Generic;
using System.Linq;
using Bot.Builder.Community.Adapters.Google.Model;
using Microsoft.Bot.Builder;

namespace Bot.Builder.Community.Adapters.Google
{
    public static class GoogleContextExtensions
    {
        public static ActionsPayload GetGoogleRequestPayload(this ITurnContext context)
        {
            try
            {
                return (ActionsPayload)context.Activity.ChannelData;
            }
            catch
            {
                return null;
            }
        }

        public static List<string> GoogleGetSurfaceCapabilities(this ITurnContext context)
        {
            var payload = (ActionsPayload)context.Activity.ChannelData;
            var capabilities = payload.Surface.Capabilities.Select(c => c.Name);
            return capabilities.ToList();
        }
    }
}
