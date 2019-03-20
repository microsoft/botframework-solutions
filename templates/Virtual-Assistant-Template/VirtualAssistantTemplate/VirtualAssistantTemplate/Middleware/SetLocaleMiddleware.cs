// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualAssistantTemplate.Middleware
{
    public class SetLocaleMiddleware : IMiddleware
    {
        private readonly string defaultLocale;

        public SetLocaleMiddleware(string defaultDefaultLocale)
        {
            defaultLocale = defaultDefaultLocale;
        }

        public async Task OnTurnAsync(ITurnContext context, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            var cultureInfo = context.Activity.Locale != null ? new CultureInfo(context.Activity.Locale) : new CultureInfo(defaultLocale);

            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = cultureInfo;

            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}
