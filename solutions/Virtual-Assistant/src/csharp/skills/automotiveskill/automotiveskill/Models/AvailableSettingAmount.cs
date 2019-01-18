// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Models
{
    /// <summary>
    /// The available numeric amount range and unit of a particular setting.
    /// </summary>
    public class AvailableSettingAmount
    {
        /// <summary>
        /// Gets or sets the unit of the amount. This may be empty if the amount has no unit.
        /// </summary>
        /// <value>The unit for this setting amount.</value>
        public string Unit { get; set; }

        /// <summary>
        /// Gets or sets the minimum numeric amount (inclusive). If this is unset, then no lower
        /// bound will be enforced.
        /// </summary>
        /// <value>Minimum amount for a setting.</value>
        public double? Min { get; set; }

        /// <summary>
        /// Gets or sets the maximum numeric amount (inclusive). If this is unset, then no upper
        /// bound will be enforced.
        /// </summary>
        /// <value>Maximum amount for a setting.</value>
        public double? Max { get; set; }
    }
}