using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Bot.Builder.ComposerBot.Json
{
    public static class LuisConfigAdaptor
    {
        public static IConfigurationBuilder UseLuisConfigAdaptor(this IConfigurationBuilder builder)
        {
            var configuration = builder.Build();
            var settings = new Dictionary<string, string>();
            settings["environment"] = configuration.GetValue<string>("luis:environment");
            settings["region"] = configuration.GetValue<string>("luis:authoringRegion");
            builder.AddInMemoryCollection(settings);
            return builder;
        }
    }
}
