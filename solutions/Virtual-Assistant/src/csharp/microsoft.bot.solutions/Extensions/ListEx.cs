using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Bot.Solutions.Resources;

namespace Microsoft.Bot.Solutions.Extensions
{
    public static class ListEx
    {
        /// <summary>
        /// Converts a list into a string that can be used in speech.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to be converted.</param>
        /// <param name="finalSeparator">The separator to be used for the last element of the list ("and" or "or" for example).</param>
        /// <param name="stringAccessor">A method that can be used to extract the elements from the list if it is a complex type.</param>
        /// <returns>A comma separated string with the elements in the list.</returns>
        public static string ToSpeechString<T>(this IList<T> list, string finalSeparator, Func<T, string> stringAccessor = null)
        {
            var itemAccessor = stringAccessor ?? (li => li.ToString());
            var speech = new StringBuilder();
            for (var i = 0; i < list.Count; i++)
            {
                speech.Append(itemAccessor(list[i]));
                if (list.Count > 1)
                {
                    string value;
                    if (i == list.Count - 2)
                    {
                        value = string.Format(CommonStrings.SeparatorFormat, finalSeparator);
                    }
                    else
                    {
                        value = i != list.Count - 1 ? ", " : string.Empty;
                    }

                    speech.Append(value);
                }
            }

            return speech.ToString();
        }
    }
}