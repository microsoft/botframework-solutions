// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Builder.Solutions.Testing
{
    using System;
    using Microsoft.Extensions.Configuration;

    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
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