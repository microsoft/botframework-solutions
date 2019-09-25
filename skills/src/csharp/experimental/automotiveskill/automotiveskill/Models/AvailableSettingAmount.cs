// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AutomotiveSkill.Utilities;
using SharpYaml;
using SharpYaml.Events;

namespace AutomotiveSkill.Models
{
    /// <summary>
    /// The available numeric amount range and unit of a particular setting.
    /// </summary>
    public class AvailableSettingAmount : IEquatable<AvailableSettingAmount>
    {
        /// <summary>
        /// Gets or sets the unit of the amount. This may be empty if the amount has no unit.
        /// </summary>
        /// <value>The unit for this setting amount.</value>
        public string Unit { get; set; }

        /// <summary>
        /// Gets or sets the minimum numeric amount (inclusive). If this is unset, then no lower
        /// bound will be enforced.
        /// </summary>
        /// <value>Minimum amount for a setting.</value>
        public double? Min { get; set; }

        /// <summary>
        /// Gets or sets the maximum numeric amount (inclusive). If this is unset, then no upper
        /// bound will be enforced.
        /// </summary>
        /// <value>Maximum amount for a setting.</value>
        public double? Max { get; set; }

        public static AvailableSettingAmount FromYaml(IParser parser)
        {
            YamlParseUtil.ConsumeMappingStart(parser);

            var result = new AvailableSettingAmount();
            while (!(parser.Current is MappingEnd))
            {
                var key = YamlParseUtil.StringFromYaml(parser);
                switch (key)
                {
                    case "unit":
                        result.Unit = YamlParseUtil.StringFromYaml(parser);
                        break;
                    case "min":
                        result.Min = YamlParseUtil.DoubleFromYaml(parser);
                        break;
                    case "max":
                        result.Max = YamlParseUtil.DoubleFromYaml(parser);
                        break;
                    default:
                        throw YamlParseUtil.UnknownKeyWhileParsing<AvailableSettingAmount>(parser, key);
                }
            }

            YamlParseUtil.ConsumeMappingEnd(parser);
            return result;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AvailableSettingAmount);
        }

        public bool Equals(AvailableSettingAmount other)
        {
            return other != null &&
                   Unit == other.Unit &&
                   EqualityComparer<double?>.Default.Equals(Min, other.Min) &&
                   EqualityComparer<double?>.Default.Equals(Max, other.Max);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Unit, Min, Max);
        }
    }
}