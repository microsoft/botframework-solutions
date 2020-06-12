// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;
using System;

namespace SkillSample.Models
{
    public class SkillState
    {
        public string Token { get; set; }

        public TimeZoneInfo TimeZone { get; set; }

        public void Clear()
        {
        }

        // reference to previously stored activities that were sent that we may want to update
        // instead of sending a new activity
        public Activity CardsToUpdate = new Activity();
    }
}
