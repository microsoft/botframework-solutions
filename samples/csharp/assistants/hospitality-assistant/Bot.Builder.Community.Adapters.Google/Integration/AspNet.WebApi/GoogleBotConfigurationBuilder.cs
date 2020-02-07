// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Builder;

namespace Bot.Builder.Community.Adapters.Google.Integration.AspNet.WebApi
{
    public class GoogleBotConfigurationBuilder
    {
        private readonly GoogleBotOptions _options;

        public GoogleBotConfigurationBuilder(GoogleBotOptions googleBotOptions)
        {
            _options = googleBotOptions;
        }

        public GoogleBotOptions googleBotOptions => _options;

        public GoogleBotConfigurationBuilder UseMiddleware(IMiddleware middleware)
        {
            _options.Middleware.Add(middleware);
            return this;
        }

        public GoogleBotConfigurationBuilder UsePaths(Action<GoogleBotPaths> configurePaths)
        {
            configurePaths(_options.Paths);
            return this;
        }
    }
}