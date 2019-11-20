// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ITSMSkill.Responses.Shared;
using ITSMSkill.Utilities;

namespace ITSMSkill.Models
{
    public enum AttributeType
    {
        None,
        [EnumLocalizedDescription("AttributeId", typeof(SharedStrings))]
        Id,
        [EnumLocalizedDescription("AttributeTitle", typeof(SharedStrings))]
        Title,
        [EnumLocalizedDescription("AttributeDescription", typeof(SharedStrings))]
        Description,
        [EnumLocalizedDescription("AttributeSearch", typeof(SharedStrings))]
        Search,
        [EnumLocalizedDescription("AttributeUrgency", typeof(SharedStrings))]
        Urgency,
        [EnumLocalizedDescription("AttributeState", typeof(SharedStrings))]
        State,
        [EnumLocalizedDescription("AttributeNumber", typeof(SharedStrings))]
        Number,
    }
}
