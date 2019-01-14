// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace Microsoft.Bot.Solutions.Middleware
{
    /// <summary>
    /// Set locale by user input locale.
    /// </summary>
    public class SetLocaleMiddleware : IMiddleware
    {
        private readonly string defaultLocale;

        public SetLocaleMiddleware(string defaultDefaultLocale)
        {
            this.defaultLocale = defaultDefaultLocale;
        }

        public async Task OnTurnAsync(ITurnContext context, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            var cultureInfo = !string.IsNullOrWhiteSpace(context.Activity.Locale) ? new CultureInfo(context.Activity.Locale) : new CultureInfo(this.defaultLocale);

            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = cultureInfo;

            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}