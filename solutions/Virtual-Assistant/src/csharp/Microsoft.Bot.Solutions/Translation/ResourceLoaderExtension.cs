// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Resources;

namespace Microsoft.Bot.Solutions.Translation
{
    public static class ResourceLoaderExtension
    {
        /// <summary>
        /// Supports languages that have only one for each of a singular and a plural form (ex. English, German, Spanish).
        /// </summary>
        /// <param name="resource">resource mananger.</param>
        /// <param name="key">the source key.</param>
        /// <param name="quantity">the quantity of the source.</param>
        /// <returns>the string been pluralized.</returns>
        public static string Pluralize(this ResourceManager resource, string key, decimal quantity)
        {
            string selectedSentence = null;
            var pluralType = quantity == 1 ? PluralType.One : PluralType.MoreThanOne;
            if (pluralType == PluralType.One)
            {
                selectedSentence = resource.GetString(key + "_One");
            }
            else if (pluralType == PluralType.MoreThanOne)
            {
                selectedSentence = resource.GetString(key + "_MoreThanOne");
            }

            return !string.IsNullOrWhiteSpace(selectedSentence)
                ? string.Format(selectedSentence, quantity)
                : string.Empty;
        }
    }
}