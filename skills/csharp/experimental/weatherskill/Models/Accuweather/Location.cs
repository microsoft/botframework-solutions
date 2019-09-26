using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeatherSkill.Models
{
    public class Location
    {
        public int Version { get; set; }

        public string Key { get; set; }

        public string Type { get; set; }

        public int Rank { get; set; }

        public string LocalizedName { get; set; }

        public string EnglishName { get; set; }

        public string PrimaryPostalCode { get; set; }

        public Region Region { get; set; }

        public Country Country { get; set; }

        public AdministrativeArea AdministrativeArea { get; set; }

        public Timezone TimeZone { get; set; }

        public Geoposition GeoPosition { get; set; }

        public bool IsAlias { get; set; }

        public SupplementalAdminArea[] SupplementalAdminAreas { get; set; }

        public string[] DataSets { get; set; }
    }
}
