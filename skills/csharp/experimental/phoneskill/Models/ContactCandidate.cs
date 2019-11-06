// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using PhoneSkill.Common;

namespace PhoneSkill.Models
{
    /// <summary>
    /// A contact from the user's contact list that serves as a candidate for who to call.
    /// </summary>
    public class ContactCandidate : ICloneable, IEquatable<ContactCandidate>
    {
        /// <summary>
        /// Gets or sets the ID of the corresponding contact in the user's contact list (optional).
        /// </summary>
        /// <value>
        /// The ID of the corresponding contact in the user's contact list (optional).
        /// This is not used by the Phone skill and may be empty or non-unique.
        /// It is passed along for the convenience of the client.
        /// </value>
        public string CorrespondingId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the contact.
        /// </summary>
        /// <value>
        /// The name of the contact. This should be the same name that is displayed in the user's contact list for this contact.
        /// </value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the phone numbers of the contact.
        /// </summary>
        /// <value>
        /// The phone numbers of the contact.
        /// </value>
        public IList<PhoneNumber> PhoneNumbers { get; set; } = new List<PhoneNumber>();

        public object Clone()
        {
            ContactCandidate clone = new ContactCandidate
            {
                CorrespondingId = CorrespondingId,
                Name = Name,
            };

            foreach (PhoneNumber phoneNumber in PhoneNumbers)
            {
                clone.PhoneNumbers.Add((PhoneNumber)phoneNumber.Clone());
            }

            return clone;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ContactCandidate);
        }

        public bool Equals(ContactCandidate other)
        {
            return other != null &&
                   CorrespondingId == other.CorrespondingId &&
                   Name == other.Name &&
                   (PhoneNumbers == null ? other.PhoneNumbers == null : Enumerable.SequenceEqual(PhoneNumbers, other.PhoneNumbers));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CorrespondingId, Name, PhoneNumbers);
        }

        public override string ToString()
        {
            return $"ContactCandidate{{CorrespondingId={CorrespondingId}, Name={Name}, PhoneNumbers={PhoneNumbers.ToPrettyString()}}}";
        }
    }
}
