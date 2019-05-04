// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Models
{
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
}