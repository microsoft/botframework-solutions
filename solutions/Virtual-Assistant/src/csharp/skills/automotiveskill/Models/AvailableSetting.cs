// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// A setting that is available on the current device, but not necessarily supported through natural language interactions.
    /// </summary>
    public class AvailableSetting
    {
        /// <summary>
        /// Gets or sets the name of this setting.
        /// </summary>
        /// <value>The canonical value of this setting.</value>
        public string CanonicalName { get; set; }

        /// <summary>
        /// Gets or sets the categories that this setting belongs too.
        /// </summary>
        /// <value>The Categories of this setting.</value>
        public IList<string> Categories { get; set; }

        /// <summary>
        /// Gets or sets the values that are available for this setting.
        /// </summary>
        /// <value>The available setting values of this setting.</value>
        public IList<AvailableSettingValue> Values { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether whether a numeric amount makes sense for this setting.
        /// If an amount is allowed for this setting, the unit "%" (percent) is
        /// always considered supported with a min of 0 and a max of 100.
        /// </summary>
        /// <value>Indicates whether this setting allows an amount.</value>
        public bool AllowsAmount { get; set; }

        /// <summary>
        /// Gets or sets the supported amount ranges and units for this setting.
        /// If an amount is allowed for this setting, the unit "%" (percent) is
        /// always considered supported with a min of 0 and a max of 100.
        /// </summary>
        /// <value>The range and units of this setting.</value>
        public IList<AvailableSettingAmount> Amounts { get; set; }

        /// <summary>
        /// Gets or sets the canonical names of other settings that are 'included' in this one.
        /// If a query matches both this setting and one or more 'included'
        /// settings, only this setting will be returned.
        /// For example, the setting "Speaker Volume" may refer to the settings
        /// "Left Speaker Volume" and "Right Speaker Volume" as included settings.
        /// </summary>
        /// <value>Settings that are included.</value>
        public IList<string> IncludedSettings { get; set; }
    }
}