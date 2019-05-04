// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using AutomotiveSkill.Utilities;
using SharpYaml;
using SharpYaml.Events;

namespace AutomotiveSkill.Models
{
    /// <summary>
    /// A supported named value of a particular setting.
    /// </summary>
    public class AvailableSettingValue : IEquatable<AvailableSettingValue>
    {
        /// <summary>
        /// Gets or sets the name of this value.
        /// </summary>
        /// <value>Canonical name.</value>
        public string CanonicalName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether whether this value requires a numeric amount.
        /// </summary>
        /// <value>Whether this setting requires an amount.</value>
        public bool RequiresAmount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether whether changing the setting to this value requires explicit
        /// confirmation from the user.
        /// </summary>
        /// <value>Indicates whether changing this setting requires confirmation from the user.</value>
        public bool RequiresConfirmation { get; set; }

        /// <summary>
        /// Gets or sets the canonical name of a different value of the same setting that is an
        /// antonym (opposite) of this one, e.g., "On" and "Off".
        /// The antonym relation must be symmetric, but only needs to be declared on
        /// one of the two antonym values.
        /// </summary>
        /// <value>The antonym (opposite) of the current value.</value>
        public string Antonym { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether whether this value changes the sign of the amount.
        /// For example, the value "Decrease" would have this flag set to true
        /// because it implies a negative change in the amount. For example,
        /// "decrease by 5" means the same as "change by minus 5".
        /// </summary>
        /// <value>Whether this value changes the sign.</value>
        public bool ChangesSignOfAmount { get; set; }

        public static AvailableSettingValue FromYaml(IParser parser)
        {
            YamlParseUtil.ConsumeMappingStart(parser);

            var result = new AvailableSettingValue();
            while (!(parser.Current is MappingEnd))
            {
                var key = YamlParseUtil.StringFromYaml(parser);
                switch (key)
                {
                    case "canonicalName":
                        result.CanonicalName = YamlParseUtil.StringFromYaml(parser);
                        break;
                    case "requiresAmount":
                        result.RequiresAmount = YamlParseUtil.BoolFromYaml(parser);
                        break;
                    case "requiresConfirmation":
                        result.RequiresConfirmation = YamlParseUtil.BoolFromYaml(parser);
                        break;
                    case "antonym":
                        result.Antonym = YamlParseUtil.StringFromYaml(parser);
                        break;
                    case "changesSignOfAmount":
                        result.ChangesSignOfAmount = YamlParseUtil.BoolFromYaml(parser);
                        break;
                    default:
                        throw YamlParseUtil.UnknownKeyWhileParsing<AvailableSettingValue>(parser, key);
                }
            }

            YamlParseUtil.ConsumeMappingEnd(parser);
            return result;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AvailableSettingValue);
        }

        public bool Equals(AvailableSettingValue other)
        {
            return other != null &&
                   CanonicalName == other.CanonicalName &&
                   RequiresAmount == other.RequiresAmount &&
                   RequiresConfirmation == other.RequiresConfirmation &&
                   Antonym == other.Antonym &&
                   ChangesSignOfAmount == other.ChangesSignOfAmount;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CanonicalName, RequiresAmount, RequiresConfirmation, Antonym, ChangesSignOfAmount);
        }
    }
}