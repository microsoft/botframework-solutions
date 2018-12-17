namespace RestaurantBooking.Models
{
    using System;
    using Newtonsoft.Json;

    public class BookingPlace
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("subCategory")]
        public string SubCategory { get; set; }

        [JsonProperty("pictureUrl")]
        public string PictureUrl { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("docType")]
        public string DocType { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("duration")]
        public string Duration { get; set; }
    }
}