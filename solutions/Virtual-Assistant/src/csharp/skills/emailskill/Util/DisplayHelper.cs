using System;
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

        public static (List<Person> formattedPersonList, List<Person> formattedUserList) FormatRecipientList(List<Person> personList, List<Person> userList)
        {
            // Remove dup items
            List<Person> formattedPersonList = new List<Person>();
            List<Person> formattedUserList = new List<Person>();

            foreach (var person in personList)
            {
                var mailAddress = person.ScoredEmailAddresses.FirstOrDefault()?.Address ?? person.UserPrincipalName;

                bool isDup = false;
                foreach (var formattedPerson in formattedPersonList)
                {
                    var formattedMailAddress = formattedPerson.ScoredEmailAddresses.FirstOrDefault()?.Address ?? formattedPerson.UserPrincipalName;

                    if (mailAddress.Equals(formattedMailAddress, StringComparison.OrdinalIgnoreCase))
                    {
                        isDup = true;
                        break;
                    }
                }

                if (!isDup)
                {
                    formattedPersonList.Add(person);
                }
            }

            foreach (var user in userList)
            {
                var mailAddress = user.ScoredEmailAddresses.FirstOrDefault()?.Address ?? user.UserPrincipalName;

                bool isDup = false;
                foreach (var formattedPerson in formattedPersonList)
                {
                    var formattedMailAddress = formattedPerson.ScoredEmailAddresses.FirstOrDefault()?.Address ?? formattedPerson.UserPrincipalName;

                    if (mailAddress.Equals(formattedMailAddress, StringComparison.OrdinalIgnoreCase))
                    {
                        isDup = true;
                        break;
                    }
                }

                if (!isDup)
                {
                    foreach (var formattedUser in formattedUserList)
                    {
                        var formattedMailAddress = formattedUser.ScoredEmailAddresses.FirstOrDefault()?.Address ?? formattedUser.UserPrincipalName;

                        if (mailAddress.Equals(formattedMailAddress))
                        {
                            isDup = true;
                            break;
                        }
                    }
                }

                if (!isDup)
                {
                    formattedUserList.Add(user);
                }
            }

            return (formattedPersonList, formattedUserList);
        }
    }
}
