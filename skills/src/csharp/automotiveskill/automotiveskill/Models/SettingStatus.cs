// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Models
{
    /// <summary>
    /// Setting status check.
    /// </summary>
    public class SettingStatus : SettingOperation
    {
        /// <summary>
        /// Gets or sets the current value that the setting is set to or null
        /// if the setting only has a numeric amount.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the current amount that the setting is set to or null if the
        /// setting only has a non-numeric value.
        /// </summary>
        public SettingAmount Amount { get; set; }

        public override object Clone()
        {
            SettingStatus clone = new SettingStatus
            {
                OperationStatus = OperationStatus,
                SettingName = SettingName,
                Value = Value
            };
            if (Amount != null)
            {
                clone.Amount = (SettingAmount)Amount.Clone();
            }

            return clone;
        }
    }
}