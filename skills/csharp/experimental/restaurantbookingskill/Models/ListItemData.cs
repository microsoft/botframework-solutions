// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace RestaurantBookingSkill.Models
{
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
