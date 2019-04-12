// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Models
{
    using System.Collections.Generic;
    using global::AutomotiveSkill.Models;
    using Luis;

    public class AutomotiveSkillState
    {
        public AutomotiveSkillState()
        {
        }

        public VehicleSettingsLuis VehicleSettingsLuisResult { get; set; }

        public IDictionary<string, IList<string>> Entities { get; set; } = new Dictionary<string, IList<string>>();

        public IList<SettingChange> Changes { get; set; } = new List<SettingChange>();

        public IList<SettingStatus> Statuses { get; set; } = new List<SettingStatus>();

        public void AddRecognizerResult(VehicleSettingsLuis luisResult)
        {
            Clear();

            VehicleSettingsLuisResult = luisResult;

            if (luisResult.Entities.AMOUNT != null)
            {
                AddEntities(nameof(luisResult.Entities.AMOUNT), luisResult.Entities.AMOUNT);
            }

            if (luisResult.Entities.SETTING != null)
            {
                AddEntities(nameof(luisResult.Entities.SETTING), luisResult.Entities.SETTING);
            }

            if (luisResult.Entities.TYPE != null)
            {
                AddEntities(nameof(luisResult.Entities.TYPE), luisResult.Entities.TYPE);
            }

            if (luisResult.Entities.UNIT != null)
            {
                AddEntities(nameof(luisResult.Entities.UNIT), luisResult.Entities.UNIT);
            }

            if (luisResult.Entities.VALUE != null)
            {
                AddEntities(nameof(luisResult.Entities.VALUE), luisResult.Entities.VALUE);
            }
        }

        public void AddRecognizerResult(VehicleSettingsNameSelectionLuis luisResult)
        {
            // Remove transient entity types.
            Entities.Remove("INDEX");

            if (luisResult.Entities.INDEX != null)
            {
                AddEntities(nameof(luisResult.Entities.INDEX), luisResult.Entities.INDEX);
            }

            if (luisResult.Entities.SETTING != null)
            {
                AddEntities(nameof(luisResult.Entities.SETTING), luisResult.Entities.SETTING);
            }
        }

        public void AddRecognizerResult(VehicleSettingsValueSelectionLuis luisResult)
        {
            // Remove transient entity types.
            Entities.Remove("INDEX");

            if (luisResult.Entities.INDEX != null)
            {
                AddEntities(nameof(luisResult.Entities.INDEX), luisResult.Entities.INDEX);
            }

            if (luisResult.Entities.SETTING != null)
            {
                AddEntities(nameof(luisResult.Entities.SETTING), luisResult.Entities.SETTING);
            }

            if (luisResult.Entities.VALUE != null)
            {
                AddEntities(nameof(luisResult.Entities.VALUE), luisResult.Entities.VALUE);
            }
        }

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
            Entities.Clear();
            Changes.Clear();
            Statuses.Clear();
        }

        private void AddEntities(string key, IEnumerable<string> values)
        {
            if (!Entities.TryGetValue(key, out IList<string> entityValues))
            {
                entityValues = new List<string>();
                Entities.Add(key, entityValues);
            }

            foreach (string value in values)
            {
                entityValues.Add(value);
            }
        }
    }
}