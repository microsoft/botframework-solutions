// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Bot.StreamingExtensions
{
    /// <summary>
    /// This class defines the settings used when serializing data contained by objects
    /// included as part of the Bot Framework Protocol v3 with Streaming Extensions.
    /// </summary>
    public static class SerializationSettings
    {
        /// <summary>
        /// The value that should be used as the content-type header for application json.
        /// </summary>
        public const string ApplicationJson = "application/json";

        /// <summary>
        /// The serialization settings for use when operating on objects defined within the bot schema.
        /// </summary>
        public static readonly JsonSerializerSettings BotSchemaSerializationSettings = new JsonSerializerSettings
        {
            Formatting = Newtonsoft.Json.Formatting.None,
            DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

        /// <summary>
        /// The default serialization settings for use in most cases.
        /// </summary>
        public static readonly JsonSerializerSettings DefaultSerializationSettings = new JsonSerializerSettings
        {
            Formatting = Newtonsoft.Json.Formatting.None,
            DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
        };

        /// <summary>
        /// The default deserialization settings for use in most cases.
        /// </summary>
        public static readonly JsonSerializerSettings DefaultDeserializationSettings = new JsonSerializerSettings
        {
            DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
        };
    }
}
