// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Testing
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

#pragma warning disable SA1402 // Static elements should appear before instance elements
#pragma warning disable SA1649 // Static elements should appear before instance elements
    [Serializable]
    public class LuisDateTime
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("timex")]
        public List<string> Timex { get; set; }
    }

    public class LuisDateTimeContainer
    {
        private List<LuisDateTime> value;

        public LuisDateTimeContainer(LuisDateTime data)
        {
            this.value = new List<LuisDateTime>() { data };
        }

        [JsonProperty("datetime")]
        public LuisDateTime[] Value => this.value.ToArray();

        public static LuisDateTimeContainer New(string type, string timex)
        {
            return new LuisDateTimeContainer(new LuisDateTime
            {
                Type = type,
                Timex = new List<string> { timex },
            });
        }
    }

    public class LuisOrdinal
    {
        private List<int> value;

        public LuisOrdinal(int data)
        {
            this.value = new List<int> { data };
        }

        [JsonProperty("ordinal")]
        public int[] Value => this.value.ToArray();
    }

    public class LuisNumber
    {
        private List<int> value;

        public LuisNumber(int data)
        {
            this.value = new List<int> { data };
        }

        [JsonProperty("number")]
        public int[] Value => this.value.ToArray();
    }

    public class LuisContactName
    {
        private List<string> value;

        public LuisContactName(string data)
        {
            this.value = new List<string> { data };
        }

        [JsonProperty("Communication_ContactName")]
        public string[] Value => this.value.ToArray();
    }

    [Serializable]
    public class LuisListItem
    {
        public LuisListItem(string value)
        {
            this.Values.Add(new List<string> { value });
        }

        public List<List<string>> Values { get; set; } = new List<List<string>>();
    }
}
#pragma warning restore SA1204 // Static elements should appear before instance elements
#pragma warning restore SA1649 // Static elements should appear before instance elements