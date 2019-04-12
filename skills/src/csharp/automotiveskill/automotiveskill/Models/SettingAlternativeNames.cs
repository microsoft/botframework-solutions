// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AutomotiveSkill.Utilities;
    using SharpYaml;
    using SharpYaml.Events;

    /// <summary>
    /// Alternative names for a setting and its values.
    /// This is probably most useful as the value type of a map whose keys are the canonical names of the settings.
    /// </summary>
    public class SettingAlternativeNames : IEquatable<SettingAlternativeNames>
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

        public static SettingAlternativeNames FromYaml(IParser parser)
        {
            YamlParseUtil.ConsumeMappingStart(parser);

            var result = new SettingAlternativeNames();
            while (!(parser.Current is MappingEnd))
            {
                var key = YamlParseUtil.StringFromYaml(parser);
                switch (key)
                {
                    case "alternativeNames":
                        result.AlternativeNames = YamlParseUtil.ListFromYaml<string>(parser);
                        break;
                    case "alternativeValueNames":
                        result.AlternativeValueNames = YamlParseUtil.DictionaryFromYaml<string, IList<string>>(parser);
                        break;
                    default:
                        throw YamlParseUtil.UnknownKeyWhileParsing<SettingAlternativeNames>(parser, key);
                }
            }

            YamlParseUtil.ConsumeMappingEnd(parser);
            return result;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SettingAlternativeNames);
        }

        public bool Equals(SettingAlternativeNames other)
        {
            return other != null &&
                   (AlternativeNames == null ? other.AlternativeNames == null : Enumerable.SequenceEqual(AlternativeNames, other.AlternativeNames)) &&
                   (AlternativeValueNames == null ? other.AlternativeValueNames == null : AlternativeValueNames.Count == other.AlternativeValueNames.Count && !AlternativeValueNames.All(pair => other.AlternativeValueNames.Contains(pair)));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AlternativeNames, AlternativeValueNames);
        }
    }
}