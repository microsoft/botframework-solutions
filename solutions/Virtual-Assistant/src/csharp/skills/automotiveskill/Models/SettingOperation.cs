// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Models
{
    using System;

    public abstract class SettingOperation : ICloneable
    {
        /// <summary>
        /// Gets or sets the status of the operation.
        /// </summary>
        /// <value>Status of an operation.</value>
        public SettingOperationStatus OperationStatus { get; set; }

        /// <summary>
        /// Gets or sets the name of this setting.
        /// </summary>
        /// <value>Name of the setting.</value>
        public string SettingName { get; set; }

        public abstract object Clone();
    }
}