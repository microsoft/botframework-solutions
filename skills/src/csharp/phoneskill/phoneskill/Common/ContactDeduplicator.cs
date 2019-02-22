using System.Collections.Generic;
using PhoneSkill.Models;

namespace PhoneSkill.Common
{
    /// <summary>
    /// Deduplicates contacts.
    /// </summary>
    public class ContactDeduplicator
    {
        private ContactDeduplicator()
        {
        }

        /// <summary>
        /// Deduplicate the given contacts by merging contacts that share at least one phone number.
        /// When merging contacts, all other properties (except the phone numbers) are retained from the first contact.
        /// </summary>
        /// <param name="contacts">The contacts to deduplicate.</param>
        /// <returns>A deduplicated copy of the given contacts.</returns>
        public static IList<ContactCandidate> DeduplicateByPhoneNumbers(IList<ContactCandidate> contacts)
        {
            var numberToContactIndices = new Dictionary<string, IList<int>>();
            for (int i = 0; i < contacts.Count; ++i)
            {
                foreach (PhoneNumber phoneNumber in contacts[i].PhoneNumbers)
                {
                    numberToContactIndices.TryGetValue(phoneNumber.Number, out var contactList);
                    if (contactList == null)
                    {
                        contactList = new List<int>();
                    }

                    contactList.Add(i);
                    numberToContactIndices[phoneNumber.Number] = contactList;
                }
            }

            var contactsToMerge = new List<ContactCandidate>(contacts);
            var mergedContacts = new List<ContactCandidate>();
            for (int i = 0; i < contactsToMerge.Count; ++i)
            {
                if (contactsToMerge[i] != null)
                {
                    var cluster = new SortedSet<int>() { i };
                    var q = new Queue<int>();
                    q.Enqueue(i);
                    while (q.Count != 0)
                    {
                        int index = q.Dequeue();
                        foreach (PhoneNumber phoneNumber in contactsToMerge[index].PhoneNumbers)
                        {
                            foreach (int j in numberToContactIndices[phoneNumber.Number])
                            {
                                if (contactsToMerge[j] != null && cluster.Add(j))
                                {
                                    q.Enqueue(j);
                                }
                            }
                        }
                    }

                    var mergedContact = (ContactCandidate)contactsToMerge[i].Clone();
                    mergedContact.PhoneNumbers = new List<PhoneNumber>();
                    var seenNumbers = new HashSet<string>();
                    foreach (int j in cluster)
                    {
                        foreach (PhoneNumber phoneNumber in contactsToMerge[j].PhoneNumbers)
                        {
                            if (seenNumbers.Add(phoneNumber.Number))
                            {
                                mergedContact.PhoneNumbers.Add(phoneNumber);
                            }
                        }

                        contactsToMerge[j] = null;
                    }

                    mergedContacts.Add(mergedContact);
                }
            }

            return mergedContacts;
        }
    }
}
