using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class Rootobject
    {
        public Response response { get; set; }
    }

    public class Venue
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "contact")]
        public Contact Contact { get; set; }

        [JsonProperty(PropertyName = "location")]
        public Location Location { get; set; }

        [JsonProperty(PropertyName = "categories")]
        public Category[] Categories { get; set; }

        [JsonProperty(PropertyName = "verified")]
        public bool Verified { get; set; }

        [JsonProperty(PropertyName = "stats")]
        public Stats Stats { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        [JsonProperty(PropertyName = "hasMenu")]
        public bool HasMenu { get; set; }

        [JsonProperty(PropertyName = "delivery")]
        public Delivery Delivery { get; set; }

        [JsonProperty(PropertyName = "menu")]
        public Menu Menu { get; set; }

        [JsonProperty(PropertyName = "allowMenuUrlEdit")]
        public bool AllowMenuUrlEdit { get; set; }

        [JsonProperty(PropertyName = "beenHere")]
        public Beenhere BeenHere { get; set; }

        [JsonProperty(PropertyName = "specials")]
        public Specials Specials { get; set; }

        [JsonProperty(PropertyName = "storeId")]
        public string StoreId { get; set; }

        [JsonProperty(PropertyName = "hereNow")]
        public Herenow HereNow { get; set; }

        [JsonProperty(PropertyName = "referralId")]
        public string ReferralId { get; set; }

        [JsonProperty(PropertyName = "venueChains")]
        public Venuechain[] VenueChains { get; set; }

        [JsonProperty(PropertyName = "hasPerk")]
        public bool HasPerk { get; set; }

        [JsonProperty(PropertyName = "venuePage")]
        public Venuepage VenuePage { get; set; }
    }
}
