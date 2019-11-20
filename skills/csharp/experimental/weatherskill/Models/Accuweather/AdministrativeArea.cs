// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeatherSkill.Models
{
    public class AdministrativeArea
    {
        public string ID { get; set; }

        public string LocalizedName { get; set; }

        public string EnglishName { get; set; }

        public int Level { get; set; }

        public string LocalizedType { get; set; }

        public string EnglishType { get; set; }

        public string CountryID { get; set; }
    }
}
