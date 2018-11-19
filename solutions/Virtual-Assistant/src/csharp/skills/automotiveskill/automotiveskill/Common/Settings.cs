// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The status of a settings operation, such as getting the volume, setting
    /// the volume, etc.
    /// </summary>
    public enum SettingOperationStatus
    {
        /// <summary>
        /// The operation has not yet been attempted.
        /// </summary>
        TO_DO,
        /// <summary>
        /// The operation was completed successfully.
        /// </summary>
        SUCCESSFUL,
        /// <summary>
        /// The operation was unsuccessful due to an error.
        /// </summary>
        UNSUCCESSFUL,
        /// <summary>
        /// The operation is a no-op. Nothing needs to be done.
        /// </summary>
        NO_OP,
        /// <summary>
        /// The operation is not supported for reasons that cannot be expressed using
        /// one of the other enum values.
        /// </summary>
        UNSUPPORTED,
        /// <summary>
        /// The given setting is not supported on this device.
        /// </summary>
        UNSUPPORTED_SETTING_NAME,
        /// <summary>
        /// The given value is not supported for the given setting on this device
        /// (but other values of the setting are supported).
        /// </summary>
        UNSUPPORTED_SETTING_VALUE_COMBINATION,
        /// <summary>
        /// The given amount is not within an acceptable range for the given setting
        /// on this device.
        /// </summary>
        UNSUPPORTED_AMOUNT_OUT_OF_RANGE,
        /// <summary>
        /// The setting is invalid because an amount is expected, but not given.
        /// E.g., <tt>VOLUME + SET</tt>.
        /// </summary>
        UNSUPPORTED_MISSING_AMOUNT,
        /// <summary>
        /// The setting is invalid because an amount is given, but is not expected.
        /// E.g., <tt>VOLUME + MUTE + 20%</tt>.
        /// </summary>
        UNSUPPORTED_EXTRA_AMOUNT,
        /// <summary>
        /// The unit of the amount is not appropriate for the given setting on this device.
        /// </summary>
        UNSUPPORTED_AMOUNT_UNIT,
    }

    /// <summary>
    /// Setting amount. This is used for numeric amounts. For non-numeric,
    /// enum-like values like "On" or "Off", a string is used instead.
    /// MIN and MAX are represented as 0% and 100%, respectively.
    /// </summary>
    public class SettingAmount : ICloneable
    {
        /// <summary>
        /// The numeric amount.
        /// </summary>
        public double Amount { get; set; }
        /// <summary>
        /// The unit that this amount is measured in.
        /// </summary>
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

    public abstract class SettingOperation : ICloneable
    {
        /// <summary>
        /// The status of the operation.
        /// </summary>
        public SettingOperationStatus OperationStatus { get; set; }
        /// <summary>
        /// The name of this setting.
        /// </summary>
        public string SettingName { get; set; }

        public abstract object Clone();
    }

    /// <summary>
    /// Setting change.
    /// </summary>
    public class SettingChange : SettingOperation
    {
        /// <summary>
        /// The value to set the setting to or null to only change the numeric amount.
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// The amount to set the setting to or null to only change the non-numeric value.
        /// </summary>
        public SettingAmount Amount { get; set; }
        /// <summary>
        /// Whether the amount is a margin relative to the current amount of the setting.
        /// </summary>
        public bool IsRelativeAmount { get; set; }
        /// <summary>
        /// Whether the user has confirmed this change explicitly.
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

    /// <summary>
    /// Setting status check.
    /// </summary>
    public class SettingStatus : SettingOperation
    {
        /// <summary>
        /// The current value that the setting is set to or null
        /// if the setting only has a numeric amount.
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// The current amount that the setting is set to or null if the
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

    /// <summary>
    /// The available numeric amount range and unit of a particular setting.
    /// </summary>
    public class AvailableSettingAmount
    {
        /// <summary>
        /// The unit of the amount. This may be empty if the amount has no unit.
        /// </summary>
        public string Unit { get; set; }
        /// <summary>
        /// The minimum numeric amount (inclusive). If this is unset, then no lower
        /// bound will be enforced.
        /// </summary>
        public double? Min { get; set; }
        /// <summary>
        /// The maximum numeric amount (inclusive). If this is unset, then no upper
        /// bound will be enforced.
        /// </summary>
        public double? Max { get; set; }
    }

    /// <summary>
    /// A supported named value of a particular setting.
    /// </summary>
    public class AvailableSettingValue
    {
        /// <summary>
        /// The name of this value.
        /// </summary>
        public string CanonicalName { get; set; }
        /// <summary>
        /// Whether this value requires a numeric amount.
        /// </summary>
        public bool RequiresAmount { get; set; }
        /// <summary>
        /// Whether changing the setting to this value requires explicit
        /// confirmation from the user.
        /// </summary>
        public bool RequiresConfirmation { get; set; }
        /// <summary>
        /// The canonical name of a different value of the same setting that is an
        /// antonym (opposite) of this one, e.g., "On" and "Off".
        /// The antonym relation must be symmetric, but only needs to be declared on
        /// one of the two antonym values.
        /// </summary>
        public string Antonym { get; set; }
        /// <summary>
        /// Whether this value changes the sign of the amount.
        /// For example, the value "Decrease" would have this flag set to true
        /// because it implies a negative change in the amount. For example,
        /// "decrease by 5" means the same as "change by minus 5".
        /// </summary>
        public bool ChangesSignOfAmount { get; set; }
    }

    /// <summary>
    /// A setting that is available on the current device, but not necessarily supported through natural language interactions.
    /// </summary>
    public class AvailableSetting
    {
        /// <summary>
        /// The name of this setting.
        /// </summary>
        public string CanonicalName { get; set; }
        /// <summary>
        /// The categories that this setting belongs too.
        /// </summary>
        public IList<string> Categories { get; set; }
        /// <summary>
        /// The values that are available for this setting.
        /// </summary>
        public IList<AvailableSettingValue> Values { get; set; }
        /// <summary>
        /// Whether a numeric amount makes sense for this setting.
        /// If an amount is allowed for this setting, the unit "%" (percent) is
        /// always considered supported with a min of 0 and a max of 100.
        /// </summary>
        public bool AllowsAmount { get; set; }
        /// <summary>
        /// The supported amount ranges and units for this setting.
        /// If an amount is allowed for this setting, the unit "%" (percent) is
        /// always considered supported with a min of 0 and a max of 100.
        /// </summary>
        public IList<AvailableSettingAmount> Amounts { get; set; }
        /// <summary>
        /// The canonical names of other settings that are 'included' in this one.
        /// If a query matches both this setting and one or more 'included'
        /// settings, only this setting will be returned.
        /// For example, the setting "Speaker Volume" may refer to the settings
        /// "Left Speaker Volume" and "Right Speaker Volume" as included settings.
        /// </summary>
        public IList<string> IncludedSettings { get; set; }
    }

    /// <summary>
    /// Alternative names for a setting and its values.
    /// This is probably most useful as the value type of a map whose keys are the canonical names of the settings.
    /// </summary>
    public class SettingAlternativeNames
    {
        /// <summary>
        /// The alternative names for this setting, excluding its canonical name.
        /// </summary>
        public IList<string> AlternativeNames { get; set; }
        /// <summary>
        /// Map from the canonical name of a value of this setting to the list of alternative names of that value, excluding its canonical name.
        /// </summary>
        public IDictionary<string, IList<string>> AlternativeValueNames { get; set; }
    }
}
