using System.Collections.Generic;
using System.Text;

namespace PhoneSkill.Common
{
    public static class ToStringExtensions
    {
        public static string ToPrettyString<T>(this T[] list)
        {
            return ToPrettyString(list, "[", "]");
        }

        public static string ToPrettyString<T>(this IList<T> list)
        {
            return ToPrettyString(list, "[", "]");
        }

        private static string ToPrettyString<T>(this IEnumerable<T> enumerable, string prefix, string suffix)
        {
            var builder = new StringBuilder();
            builder.Append(prefix);

            var isFirst = true;
            foreach (var element in enumerable)
            {
                if (!isFirst)
                {
                    builder.Append(", ");
                }

                var elementList = element as IList<object>;
                if (elementList != null)
                {
                    builder.Append(elementList.ToPrettyString());
                }
                else
                {
                    var elementArray = element as object[];
                    if (elementArray != null)
                    {
                        builder.Append(elementArray.ToPrettyString());
                    }
                    else
                    {
                        builder.Append(element);
                    }
                }

                isFirst = false;
            }

            builder.Append(suffix);
            return builder.ToString();
        }
    }
}
