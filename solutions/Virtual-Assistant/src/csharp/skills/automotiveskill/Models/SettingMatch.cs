// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutomotiveSkill.Models
{
    /// <summary>
    /// A matching setting-value pair.
    /// </summary>
    public class SettingMatch
    {
        public string SettingName { get; set; }

        public string Value { get; set; }
    }
}
