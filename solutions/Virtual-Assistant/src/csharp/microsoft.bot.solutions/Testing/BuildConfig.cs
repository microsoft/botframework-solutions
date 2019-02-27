// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Testing
{
    using Microsoft.Extensions.Configuration;

    public class BuildConfig
    {
        public BuildConfig()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();

            this.Configuration = config.Build();
        }

        public IConfigurationRoot Configuration { get; }
    }
}