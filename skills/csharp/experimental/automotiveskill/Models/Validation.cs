﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Models
{
    public enum ValidationStatus
    {
        /// <summary>
        /// No validation performed
        /// </summary>
        None,

        /// <summary>
        /// Setting is valid
        /// </summary>
        Valid,

        /// <summary>
        /// Missing Setting
        /// </summary>
        InvalidMissingSetting,

        /// <summary>
        /// Missing Value
        /// </summary>
        InvalidMissingValue,

        /// <summary>
        /// Invalid Value
        /// </summary>
        InvalidValue,

        /// <summary>
        /// Invalid Setting Name
        /// </summary>
        InvalidSettingName,

        /// <summary>
        /// Invalid Combination of Setting Values
        /// </summary>
        InvalidSettingValueCombination,

        /// <summary>
        /// Missing Amount
        /// </summary>
        InvalidMissingAmount,

        /// <summary>
        /// Extra Amount is Invalid
        /// </summary>
        InvalidExtraAmount,

        /// <summary>
        /// Invalid Unit for Amount
        /// </summary>
        InvalidAmountUnit,

        /// <summary>
        /// Amount out of range
        /// </summary>
        InvalidAmountOutOfRange,

        /// <summary>
        /// Invalid Amount
        /// </summary>
        InvalidAmount
    }
}