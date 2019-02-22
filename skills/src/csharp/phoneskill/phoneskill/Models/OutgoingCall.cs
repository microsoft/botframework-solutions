using System;
using System.Collections.Generic;

namespace PhoneSkill.Models
{
    /// <summary>
    /// An outgoing call to be placed.
    /// </summary>
    public class OutgoingCall : IEquatable<OutgoingCall>
    {
        /// <summary>
        /// Gets or sets the phone number to call.
        /// </summary>
        /// <value>
        /// The phone number to call.
        /// </value>
        public string Number { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the contact to call.
        /// </summary>
        /// <value>
        /// The contact to call.
        /// This may be null if, for example, the user asked to call a phone number that is not in their contact list.
        /// Note that the list of phone numbers of this contact may have been shortened to only the number to be called.
        /// </value>
        public ContactCandidate Contact { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as OutgoingCall);
        }

        public bool Equals(OutgoingCall other)
        {
            return other != null &&
                   Number == other.Number &&
                   (Contact == null ? other.Contact == null : EqualityComparer<ContactCandidate>.Default.Equals(Contact, other.Contact));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Number, Contact);
        }

        public override string ToString()
        {
            return $"OutgoingCall{{Number={Number}, Contact={Contact}}}";
        }
    }
}
