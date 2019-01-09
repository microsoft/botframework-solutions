// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Common
{
    using global::AutomotiveSkill.Models;
    using Microsoft.Bot.Schema;

    public static class SettingsEvents
    {
        public const string SettingStatusRequestEvent = "SettingStatusRequest";
        public const string SettingStatusResponseEvent = "SettingStatusResponse";
        public const string SettingChangeRequestEvent = "SettingChangeRequest";
        public const string SettingChangeResponseEvent = "SettingChangeResponse";

        public static Activity CreateSettingStatusRequestEvent(string settingName)
        {
            var settingEvent = Activity.CreateEventActivity();
            settingEvent.Name = SettingStatusRequestEvent;
            settingEvent.Value = settingName;
            return (Activity)settingEvent;
        }

        public static Activity CreateSettingChangeRequestEvent(SettingChange settingChange)
        {
            var settingEvent = Activity.CreateEventActivity();
            settingEvent.Name = SettingChangeRequestEvent;
            settingEvent.Value = settingChange;
            return (Activity)settingEvent;
        }
    }
}