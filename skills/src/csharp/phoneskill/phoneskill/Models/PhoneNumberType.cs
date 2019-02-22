using System;
using System.Linq;

namespace PhoneSkill.Models
{
    /// <summary>
    /// The type or label of a phone number.
    /// This can be a standard type (HOME, BUSINESS, or MOBILE) or a free-form string label (or both if the free-form string label can be mapped to a standard type).
    /// </summary>
    public class PhoneNumberType : ICloneable, IEquatable<PhoneNumberType>
    {
        /// <summary>
        /// Commonly used standard types of phone numbers.
        /// </summary>
        public enum StandardType
        {
            /// <summary>
            /// There is no corresponding standardized type; it's a free-form label.
            /// </summary>
            NONE,

            /// <summary>
            /// A home phone number.
            /// </summary>
            HOME,

            /// <summary>
            /// A business phone number.
            /// </summary>
            BUSINESS,

            /// <summary>
            /// A mobile phone number.
            /// </summary>
            MOBILE,
        }

        /// <summary>
        /// Gets or sets the standardized type of the phone number.
        /// </summary>
        /// <value>
        /// The standardized type of the phone number.
        /// </value>
        public StandardType Standardized { get; set; } = StandardType.NONE;

        /// <summary>
        /// Gets or sets the free-form label for the phone number.
        /// </summary>
        /// <value>
        /// The free-form label for the phone number.
        /// </value>
        public string FreeForm { get; set; } = string.Empty;

        public object Clone()
        {
            return new PhoneNumberType
            {
                Standardized = Standardized,
                FreeForm = FreeForm,
            };
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PhoneNumberType);
        }

        public bool Equals(PhoneNumberType other)
        {
            return other != null &&
                   Standardized == other.Standardized &&
                   FreeForm == other.FreeForm;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Standardized, FreeForm);
        }

        public override string ToString()
        {
            return $"PhoneNumberType{{Standardized={Standardized}, FreeForm={FreeForm}}}";
        }

        /// <summary>
        /// Returns whether the phone number type is specified.
        /// </summary>
        /// <returns>Whether the phone number type is specified.</returns>
        public bool Any()
        {
            return Standardized != StandardType.NONE || FreeForm.Any();
        }
    }
}
