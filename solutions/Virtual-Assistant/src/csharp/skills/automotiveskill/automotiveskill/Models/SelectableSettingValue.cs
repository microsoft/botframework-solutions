// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutomotiveSkill.Models
{
    /// <summary>
    /// A setting value that can be selected from a list.
    /// </summary>
    public class SelectableSettingValue
    {
        /// <summary>
        /// Gets or sets the canonical name of the setting this value belongs to.
        /// </summary>
        /// <value>The canonical name of the setting.</value>
        public string CanonicalSettingName { get; set; }

        /// <summary>
        /// Gets or sets the setting value.
        /// </summary>
        /// <value>The Available Setting value.</value>
        public AvailableSettingValue Value { get; set; }
    }
}
