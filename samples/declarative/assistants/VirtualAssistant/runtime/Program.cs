// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Bot.Builder.ComposerBot.Json
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, builder) =>
            {
                var env = hostingContext.HostingEnvironment;
                
                builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                if (env.IsDevelopment())
                {
                    // Local Debug
                    builder.AddJsonFile("appsettings.development.json", optional: true, reloadOnChange: true);
                }

                var configuration = builder.Build();

                // Hard code the settings path to 'ComposerDialogs' while deployment
                var botRoot = configuration.GetValue<string>("bot") ?? "ComposerDialogs";
                var configFile = Path.GetFullPath(Path.Combine(botRoot, @"settings/appsettings.json"));

                builder.AddJsonFile(configFile, optional: true, reloadOnChange: true);

                // Need to put this part here to override the any customized settings
                if (!env.IsDevelopment())
                {
                    //Azure Deploy
                    builder.AddJsonFile("appsettings.deployment.json", optional: true, reloadOnChange: true);
                }

                builder.UseLuisConfigAdaptor()
                    .UseLuisSettings();

                builder.AddEnvironmentVariables()
                       .AddCommandLine(args);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
    }
}
