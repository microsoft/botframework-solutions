// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Models
{
    /// <summary>
    /// Setting change.
    /// </summary>
    public class SettingChange : SettingOperation
    {
        /// <summary>
        /// Gets or sets the value to set the setting to or null to only change the numeric amount.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the amount to set the setting to or null to only change the non-numeric value.
        /// </summary>
        public SettingAmount Amount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether whether the amount is a margin relative to the current amount of the setting.
        /// </summary>
        public bool IsRelativeAmount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether whether the user has confirmed this change explicitly.
        /// </summary>
        public bool IsConfirmed { get; set; }

        public override object Clone()
        {
            SettingChange clone = new SettingChange
            {
                OperationStatus = OperationStatus,
                SettingName = SettingName,
                Value = Value
            };
            if (Amount != null)
            {
                clone.Amount = (SettingAmount)Amount.Clone();
            }

            clone.IsRelativeAmount = IsRelativeAmount;
            clone.IsConfirmed = IsConfirmed;
            return clone;
        }
    }
}