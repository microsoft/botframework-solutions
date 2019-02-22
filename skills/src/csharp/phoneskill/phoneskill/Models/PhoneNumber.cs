using System;
using System.Collections.Generic;

namespace PhoneSkill.Models
{
    /// <summary>
    /// A phone number of one of the user's contacts.
    /// </summary>
    public class PhoneNumber : ICloneable, IEquatable<PhoneNumber>
    {
        /// <summary>
        /// Gets or sets the literal phone number.
        /// </summary>
        /// <value>
        /// The literal phone number.
        /// </value>
        public string Number { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the phone number.
        /// </summary>
        /// <value>
        /// The type of the phone number.
        /// </value>
        public PhoneNumberType Type { get; set; } = new PhoneNumberType();

        public object Clone()
        {
            return new PhoneNumber
            {
                Number = Number,
                Type = (PhoneNumberType)Type.Clone(),
            };
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PhoneNumber);
        }

        public bool Equals(PhoneNumber other)
        {
            return other != null &&
                   Number == other.Number &&
                   EqualityComparer<PhoneNumberType>.Default.Equals(Type, other.Type);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Number, Type);
        }

        public override string ToString()
        {
            return $"PhoneNumber{{Number={Number}, Type={Type}}}";
        }
    }
}
