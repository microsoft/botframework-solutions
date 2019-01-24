using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmailSkill.Dialogs.Shared.Resources.Strings
{
    public class EmailCommonPhrase
    {
        public static bool GetIsSkip(string input)
        {
            var skipItems = EmailCommonStrings.Skip.Split(EmailCommonStrings.Split);

            for (int i = 0; i < skipItems.Count(); i++)
            {
                skipItems[i] = skipItems[i].Trim();
            }

            var isSkip = false;
            if (skipItems.Contains<string>(input.ToLowerInvariant()))
            {
                isSkip = true;
            }

            return isSkip;
        }
    }
}
