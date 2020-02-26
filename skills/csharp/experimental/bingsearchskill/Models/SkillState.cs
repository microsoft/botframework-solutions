// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace BingSearchSkill.Models
{
    public class SkillState
    {
        public SkillState()
        {
            SearchEntityType = SearchResultModel.EntityType.Unknown;
        }

        public string Token { get; internal set; }

        public BingSearchSkillLuis LuisResult { get; internal set; }

        public string SearchEntityName { get; set; }

        public SearchResultModel.EntityType SearchEntityType { get; set; }

        public bool NewConversation { get; set; } = true;

        public bool IsAction { get; set; } = false;

        public void Clear()
        {
            SearchEntityName = string.Empty;
            SearchEntityType = SearchResultModel.EntityType.Unknown;
            IsAction = false;
        }
    }
}
