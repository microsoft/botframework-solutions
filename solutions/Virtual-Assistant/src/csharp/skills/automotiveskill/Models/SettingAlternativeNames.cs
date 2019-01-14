// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Alternative names for a setting and its values.
    /// This is probably most useful as the value type of a map whose keys are the canonical names of the settings.
    /// </summary>
    public class SettingAlternativeNames
    {
        /// <summary>
        /// Gets or sets the alternative names for this setting, excluding its canonical name.
        /// </summary>
        /// <value>Alternative names.</value>
        public IList<string> AlternativeNames { get; set; }

        /// <summary>
        /// Gets or sets map from the canonical name of a value of this setting to the list of alternative names of that value, excluding its canonical name.
        /// </summary>
        /// <value>Alternative value names.</value>
        public IDictionary<string, IList<string>> AlternativeValueNames { get; set; }
    }
}