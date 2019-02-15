// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace Microsoft.Bot.Solutions.Translation
{
    public class TranslatorLocaleHelper
    {
        public static string GetActiveLanguage(ITurnContext context, string defaultLocale)
        {
            var locale = defaultLocale;
            if (context.Activity.Locale != null)
            {
                locale = context.Activity.Locale;
            }

            return locale;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<bool> CheckUserChangedLanguage(ITurnContext arg)
        {
            return await Task.Run(() => false);
        }
    }
}