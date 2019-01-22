// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace PointOfInterestSkill.Models
{
    /// <summary>
    /// Enum for different Language Model types.
    /// </summary>
    public enum LanguageModelType
    {
        /// <summary>
        /// Language model used for top-level dispatch with LUIS
        /// </summary>
        Dispatch,

        /// <summary>
        /// Language model used with LUIS service
        /// </summary>
        Luis,

        /// <summary>
        /// Language model for QnAMaker service
        /// </summary>
        Qna,
    }

    /// <summary>
    /// Language model configuration class.
    /// </summary>
    public class LanguageModel
    {
        /// <summary>
        /// Gets or sets the language model type, i.e. Dispatcher, LUIS, QnAMaker.
        /// </summary>
        /// <value>
        /// The language model type, i.e. Dispatcher, LUIS, QnAMaker.
        /// </value>
        public LanguageModelType Type { get; set; }

        /// <summary>
        /// Gets or sets the id of the language model.
        /// </summary>
        /// <value>
        /// The id of the language model.
        /// </value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the language model.
        /// </summary>
        /// <value>
        /// The name of the language model.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Azure service key for the language model.
        /// </summary>
        /// <value>
        /// The Azure service key for the language model.
        /// </value>
        public string SubscriptionKey { get; set; }

        /// <summary>
        /// Gets or sets the Azure service endpoint for the language model.
        /// </summary>
        /// <value>
        /// The Azure service endpoint for the language model.
        /// </value>
        public string Endpoint { get; set; }
    }
}