// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Testing
{
    using System;
    using System.IO;
    using System.Linq;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

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