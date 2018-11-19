// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill
{
    using Luis;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Graph;
    using System;
    using System.Collections.Generic;

    public class AutomotiveSkillState
    {
        public AutomotiveSkillState()
        {

        }

        public VehicleSettings LuisResult { get; set; }
        public RecognizerResult RawLuis { get; set; }

        public string Intent { get; set; }

        public VehicleSettingStage DialogStateType { get; set; } = VehicleSettingStage.None;

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

        public void AddRecognizerResult(RecognizerResult result, bool switchContext = false)
        {
            var (intent, score) = result.GetTopScoringIntent();
            if (switchContext)
            {
                Intent = intent;
                DialogStateType = VehicleSettingStage.None;
                Entities.Clear();
                Changes.Clear();
                Statuses.Clear();
            }
            else
            {
                // Remove transient entity types.
                Entities.Remove("INDEX");
            }

            RecognizerResultWrapper.AddEntitiesToMap(this.Entities, result);
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
    }

    public enum VehicleSettingStage
    {
        None = 0,
        NameSelection,
        ValueSelection,
        ChangeConfirmation
    }
}
