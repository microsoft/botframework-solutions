// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Newtonsoft.Json;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class Photo
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "createdAt")]
        public int CreatedAt { get; set; }

        [JsonProperty(PropertyName = "source")]
        public Source Source { get; set; }

        [JsonProperty(PropertyName = "prefix")]
        public string Prefix { get; set; }

        [JsonProperty(PropertyName = "suffix")]
        public string Suffix { get; set; }

        [JsonProperty(PropertyName = "width")]
        public int Width { get; set; }

        [JsonProperty(PropertyName = "height")]
        public int Height { get; set; }

        [JsonProperty(PropertyName = "visibility")]
        public string Visibility { get; set; }

        [JsonProperty(PropertyName = "absoluteUrl")]
        public string AbsoluteUrl
        {
            get
            {
                return string.Format($"{{0}}{{1}}x{{2}}{{3}}", Prefix, Width, Height, Suffix);
            }

            set
            {
                AbsoluteUrl = value;
            }
        }
    }
}