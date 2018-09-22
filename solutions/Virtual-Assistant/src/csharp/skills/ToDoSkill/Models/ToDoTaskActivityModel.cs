// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// To Do task activity.
    /// </summary>
    public class ToDoTaskActivityModel
    {
        /// <summary>
        /// Gets or sets To Do task activity id.
        /// </summary>
        /// <value>
        /// To Do task activity id.
        /// </value>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets To Do task activity content.
        /// </summary>
        /// <value>
        /// To Do task activity content.
        /// </value>
        [JsonProperty(PropertyName = "topic")]
        public string Topic { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether To Do task is completed or not.
        /// </summary>
        /// <value>
        /// A value indicating whether To Do task is completed or not.
        /// </value>
        [JsonProperty(PropertyName = "isCompleted")]
        public bool IsCompleted { get; set; }
    }
}
