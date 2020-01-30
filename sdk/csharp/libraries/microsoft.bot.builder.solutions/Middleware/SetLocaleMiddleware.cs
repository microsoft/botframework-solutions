// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace Microsoft.Bot.Builder.Solutions.Middleware
{
    /// <summary>
    /// Set locale by user input locale.
    /// </summary>
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class SetLocaleMiddleware : IMiddleware
    {
        private readonly string _defaultLocale;

        public SetLocaleMiddleware(string defaultLocale)
        {
            _defaultLocale = defaultLocale ?? throw new ArgumentNullException(nameof(defaultLocale));
        }

        public async Task OnTurnAsync(ITurnContext context, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            var cultureInfo = !string.IsNullOrWhiteSpace(context.Activity.Locale) ? new CultureInfo(context.Activity.Locale) : new CultureInfo(this._defaultLocale);

            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = cultureInfo;

            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}