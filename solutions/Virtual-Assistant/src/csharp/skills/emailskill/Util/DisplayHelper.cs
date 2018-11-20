using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Graph;

namespace EmailSkill.Util
{
    public class DisplayHelper
    {
        public static string ToDisplayRecipientsString_Summay(IEnumerable<Recipient> recipients)
        {
            string toRecipient = ((recipients?.FirstOrDefault()?.EmailAddress?.Name != string.Empty)
                                 || (recipients?.FirstOrDefault()?.EmailAddress?.Name != null))
                                 ? recipients?.FirstOrDefault()?.EmailAddress?.Name : CommonStrings.UnknownRecipient;

            var nameListString = toRecipient;
            if (recipients.Count() > 1)
            {
                nameListString += string.Format(CommonStrings.RecipientsSummary, recipients.Count() - 1);
            }

            return nameListString;
        }

        public static string ToDisplayRecipientsString(IEnumerable<Recipient> recipients)
        {
            string displayString = string.Empty;

            foreach (var recipient in recipients)
            {
                var recipientName = ((recipient.EmailAddress?.Name != string.Empty)
                    || (recipient.EmailAddress?.Name != null))
                    ? recipient.EmailAddress?.Name : CommonStrings.UnknownRecipient;

                displayString += string.Join("; ", recipientName);
            }

            return displayString;
        }
    }
}
