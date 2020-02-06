// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Web.Http;

namespace Bot.Builder.Community.Adapters.Google.Integration.AspNet.WebApi
{
    public static class HttpConfigurationExtensions
    {
        public static HttpConfiguration MapGoogleBotFramework(this HttpConfiguration httpConfiguration, Action<GoogleBotConfigurationBuilder> configurer)
        {
            var options = new GoogleBotOptions();
            var optionsBuilder = new GoogleBotConfigurationBuilder(options);

            configurer(optionsBuilder);

            ConfiguregoogleBotRoutes(BuildAdapter());

            return httpConfiguration;

            GoogleAdapter BuildAdapter()
            {
                var adapter = new GoogleAdapter();

                foreach (var middleware in options.Middleware)
                {
                    adapter.Use(middleware);
                }

                return adapter;
            }

            void ConfiguregoogleBotRoutes(GoogleAdapter adapter)
            {
                var routes = httpConfiguration.Routes;
                var baseUrl = options.Paths.BasePath;

                if (!baseUrl.StartsWith("/"))
                {
                    baseUrl = baseUrl.Substring(1, baseUrl.Length - 1);
                }

                if (!baseUrl.EndsWith("/"))
                {
                    baseUrl += "/";
                }

                routes.MapHttpRoute(
                        "Google Action Requests Handler",
                        baseUrl + options.Paths.SkillRequestsPath,
                        defaults: null,
                        constraints: null,
                        handler: new GoogleRequestHandler(adapter, options.GoogleOptions));
            }
        }
    }
}