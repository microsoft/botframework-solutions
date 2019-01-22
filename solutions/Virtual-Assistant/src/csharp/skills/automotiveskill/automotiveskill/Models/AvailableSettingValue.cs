// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Models
{
    /// <summary>
    /// A supported named value of a particular setting.
    /// </summary>
    public class AvailableSettingValue
    {
        /// <summary>
        /// Gets or sets the name of this value.
        /// </summary>
        /// <value>Canonical name.</value>
        public string CanonicalName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether whether this value requires a numeric amount.
        /// </summary>
        /// <value>Whether this setting requires an amount.</value>
        public bool RequiresAmount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether whether changing the setting to this value requires explicit
        /// confirmation from the user.
        /// </summary>
        /// <value>Indicates whether changing this setting requires confirmation from the user.</value>
        public bool RequiresConfirmation { get; set; }

        /// <summary>
        /// Gets or sets the canonical name of a different value of the same setting that is an
        /// antonym (opposite) of this one, e.g., "On" and "Off".
        /// The antonym relation must be symmetric, but only needs to be declared on
        /// one of the two antonym values.
        /// </summary>
        /// <value>The antonym (opposite) of the current value.</value>
        public string Antonym { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether whether this value changes the sign of the amount.
        /// For example, the value "Decrease" would have this flag set to true
        /// because it implies a negative change in the amount. For example,
        /// "decrease by 5" means the same as "change by minus 5".
        /// </summary>
        /// <value>Whether this value changes the sign.</value>
        public bool ChangesSignOfAmount { get; set; }
    }
}