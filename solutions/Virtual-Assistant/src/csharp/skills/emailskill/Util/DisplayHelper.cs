using System;
using System.Collections.Generic;
using System.Linq;
using EmailSkill.Dialogs.Shared.Resources.Strings;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Graph;

namespace EmailSkill.Util
{
    public class DisplayHelper
    {
        public static string ToDisplayRecipientsString_Summay(IEnumerable<Recipient> recipients)
        {
            if (recipients == null || recipients.Count() == 0)
            {
                throw new Exception("No recipient!");
            }

            string toRecipient = !string.IsNullOrEmpty(recipients.FirstOrDefault()?.EmailAddress?.Name)
                                 ? recipients.FirstOrDefault()?.EmailAddress?.Name : EmailCommonStrings.UnknownRecipient;

            var nameListString = toRecipient;
            if (recipients.Count() > 1)
            {
                nameListString += string.Format(CommonStrings.RecipientsSummary, recipients.Count() - 1);
            }

            return nameListString;
        }

        public static string ToDisplayRecipientsString(IEnumerable<Recipient> recipients)
        {
            if (recipients == null || recipients.Count() == 0)
            {
                throw new Exception("No recipient!");
            }

            string toRecipient = !string.IsNullOrEmpty(recipients.FirstOrDefault()?.EmailAddress?.Name)
                                 ? recipients.FirstOrDefault()?.EmailAddress?.Name : EmailCommonStrings.UnknownRecipient;

            var displayString = toRecipient;
            if (recipients.Count() > 1)
            {
                for (int i = 1; i < recipients.Count(); i++)
                {
                    if (string.IsNullOrEmpty(recipients.ElementAt(i)?.EmailAddress?.Name))
                    {
                        displayString += string.Format("; {0}", EmailCommonStrings.UnknownRecipient);
                    }
                    else
                    {
                        displayString += string.Format("; {0}", recipients.ElementAt(i)?.EmailAddress?.Name);
                    }
                }
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
