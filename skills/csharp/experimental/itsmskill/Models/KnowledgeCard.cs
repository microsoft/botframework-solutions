// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace ITSMSkill.Models
{
    public class KnowledgeCard : ICardData
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public string UpdatedTime { get; set; }

        public string Content { get; set; }

        public string Speak { get; set; }

        public string Number { get; set; }

        public string UrlTitle { get; set; }

        public string UrlLink { get; set; }

        public string ProviderDisplayText { get; set; }
    }
}
