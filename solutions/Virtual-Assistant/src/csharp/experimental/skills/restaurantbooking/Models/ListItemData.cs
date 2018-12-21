namespace RestaurantBooking.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class ListItemData
    {
        [JsonProperty("commandName")]
        public string CommandName { get; set; }

        [JsonProperty("selectedItem")]
        public string SelectedItem { get; set; }

        [JsonProperty("selectedItemIndex")]
        public int? SelectedItemIndex { get; set; }
    }
}
