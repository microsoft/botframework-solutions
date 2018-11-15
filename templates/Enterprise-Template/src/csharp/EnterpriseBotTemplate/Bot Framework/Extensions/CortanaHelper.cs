// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;
using System;
using System.Linq;
using static Microsoft.Bot.Builder.Dialogs.Choices.Channel;

namespace $safeprojectname$.Extensions
{
    public static class CortanaHelper
    {
        public static bool IsLaunchActivity(Activity activity)
        {
            if (activity.ChannelId != Channels.Cortana)
            {
                return false;
            }
            
            Entity intent = activity.Entities?.FirstOrDefault(entity => entity.Type.Equals("Intent", StringComparison.Ordinal));
            string intentName = string.Empty;
            if (intent != null)
            {
                intentName = intent.Properties["name"]?.ToString();
            }
            
            return (intent != null 
                    && !string.IsNullOrEmpty(intentName) 
                    && intentName == "Microsoft.Launch");
        }
    }
}