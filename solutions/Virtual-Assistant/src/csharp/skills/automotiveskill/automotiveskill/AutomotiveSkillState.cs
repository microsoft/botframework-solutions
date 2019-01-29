// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill
{
    using System.Collections.Generic;
    using global::AutomotiveSkill.Models;
    using Luis;

    public class AutomotiveSkillState
    {
        public AutomotiveSkillState()
        {
        }

        public VehicleSettings VehicleSettingsLuisResult { get; set; }

        public IDictionary<string, IList<string>> Entities { get; set; } = new Dictionary<string, IList<string>>();

        public IList<SettingChange> Changes { get; set; } = new List<SettingChange>();

        public IList<SettingStatus> Statuses { get; set; } = new List<SettingStatus>();

        public IList<string> GetUniqueSettingNames()
        {
            IList<string> settingNames = new List<string>();
            ISet<string> seenSettingNames = new HashSet<string>();

            foreach (var change in Changes)
            {
                if (change.SettingName != null && seenSettingNames.Add(change.SettingName))
                {
                    settingNames.Add(change.SettingName);
                }
            }

            foreach (var status in Statuses)
            {
                if (status.SettingName != null && seenSettingNames.Add(status.SettingName))
                {
                    settingNames.Add(status.SettingName);
                }
            }

            return settingNames;
        }

        public IList<string> GetUniqueSettingValues()
        {
            IList<string> settingValues = new List<string>();
            ISet<string> seenSettingValues = new HashSet<string>();

            foreach (var change in Changes)
            {
                if (change.Value != null && seenSettingValues.Add(change.Value))
                {
                    settingValues.Add(change.Value);
                }
            }

            return settingValues;
        }

        public void Clear()
        {
            VehicleSettingsLuisResult = null;
            Entities = null;
            Changes = null;
            Statuses = null;
        }
    }
}