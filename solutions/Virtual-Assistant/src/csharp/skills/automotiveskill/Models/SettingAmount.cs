// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Models
{
    using System;

    /// <summary>
    /// Setting amount. This is used for numeric amounts. For non-numeric,
    /// enum-like values like "On" or "Off", a string is used instead.
    /// MIN and MAX are represented as 0% and 100%, respectively.
    /// </summary>
    public class SettingAmount : ICloneable
    {
        /// <summary>
        /// Gets or sets the numeric amount.
        /// </summary>
        /// <value>Amount for this setting.</value>
        public double Amount { get; set; }

        /// <summary>
        /// Gets or sets the unit that this amount is measured in.
        /// </summary>
        /// <value>Unit of this value.</value>
        public string Unit { get; set; }

        public object Clone()
        {
            SettingAmount clone = new SettingAmount
            {
                Amount = Amount,
                Unit = Unit
            };
            return clone;
        }
    }
}