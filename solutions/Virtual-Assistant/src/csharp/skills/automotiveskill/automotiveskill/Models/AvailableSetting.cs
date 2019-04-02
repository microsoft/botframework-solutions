// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::AutomotiveSkill.Yaml;
    using SharpYaml;
    using SharpYaml.Events;

    /// <summary>
    /// A setting that is available on the current device and supported through natural language interactions.
    /// </summary>
    public class AvailableSetting : IEquatable<AvailableSetting>
    {
        /// <summary>
        /// Gets or sets the name of this setting.
        /// </summary>
        /// <value>The canonical value of this setting.</value>
        public string CanonicalName { get; set; }

        /// <summary>
        /// Gets or sets the image file name used to represent this setting.
        /// </summary>
        /// /// <value>The filename of the image.</value>
        public string ImageFileName { get; set; }

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

        public static AvailableSetting FromYaml(IParser parser)
        {
            YamlParseUtil.ConsumeMappingStart(parser);

            AvailableSetting result = new AvailableSetting();
            while (!(parser.Current is MappingEnd))
            {
                var key = YamlParseUtil.StringFromYaml(parser);
                switch (key)
                {
                    case "canonicalName":
                        result.CanonicalName = YamlParseUtil.StringFromYaml(parser);
                        break;
                    case "imageFileName":
                        result.ImageFileName = YamlParseUtil.StringFromYaml(parser);
                        break;
                    case "values":
                        result.Values = YamlParseUtil.ListFromYaml<AvailableSettingValue>(parser);
                        break;
                    case "allowsAmount":
                        result.AllowsAmount = YamlParseUtil.BoolFromYaml(parser);
                        break;
                    case "amounts":
                        result.Amounts = YamlParseUtil.ListFromYaml<AvailableSettingAmount>(parser);
                        break;
                    case "includedSettings":
                        result.IncludedSettings = YamlParseUtil.ListFromYaml<string>(parser);
                        break;
                    default:
                        throw YamlParseUtil.UnknownKeyWhileParsing<AvailableSetting>(parser, key);
                }
            }

            YamlParseUtil.ConsumeMappingEnd(parser);
            return result;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AvailableSetting);
        }

        public bool Equals(AvailableSetting other)
        {
            return other != null &&
                   CanonicalName == other.CanonicalName &&
                   ImageFileName == other.ImageFileName &&
                   (Values == null ? other.Values == null : Enumerable.SequenceEqual(Values, other.Values)) &&
                   AllowsAmount == other.AllowsAmount &&
                   (Amounts == null ? other.Amounts == null : Enumerable.SequenceEqual(Amounts, other.Amounts)) &&
                   (IncludedSettings == null ? other.IncludedSettings == null : Enumerable.SequenceEqual(IncludedSettings, other.IncludedSettings));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CanonicalName, ImageFileName, Values, AllowsAmount, Amounts, IncludedSettings);
        }
    }
}