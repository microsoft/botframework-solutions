// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;

// https://stackoverflow.com/questions/17380900/enum-localization
namespace ITSMSkill.Utilities
{
    public static class EnumToLocalizedString
    {
        public static string ToLocalizedString(this Enum enumValue)
        {
            FieldInfo fi = enumValue.GetType().GetField(enumValue.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].Description;
            }
            else
            {
                return enumValue.ToString();
            }
        }
    }

    public class EnumLocalizedDescription : DescriptionAttribute
    {
        private readonly string key;
        private readonly ResourceManager resource;

        public EnumLocalizedDescription(string key, Type type)
        {
            this.resource = new ResourceManager(type);
            this.key = key;
        }

        public override string Description
        {
            get
            {
                return resource.GetString(key);
            }
        }
    }
}
