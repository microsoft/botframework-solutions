// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using global::AutomotiveSkill.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// List of available settings and their alternative names.
    /// </summary>
    public class SettingList
    {
        private static readonly string DefaultAlternativeValueNamesKey = "*DEFAULT*";

        private readonly IList<string> availableSettingNames = new List<string>();
        private readonly IDictionary<string, AvailableSetting> availableSettingMap = new Dictionary<string, AvailableSetting>();
        private readonly IDictionary<string, SettingAlternativeNames> alternativeNameMap;
        private readonly IDictionary<string, IList<string>> defaultAlternativeValueNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingList"/> class.
        /// Provides access to the available settings that are stored in the available_settings JSON file.
        /// </summary>
        /// <param name="availableSettingsPath">Path to available settings file.</param>
        /// <param name="settingAlternativeNamesPath">Path to alternative names file.</param>
        public SettingList(string availableSettingsPath, string settingAlternativeNamesPath)
        {
            if (string.IsNullOrEmpty(availableSettingsPath))
            {
                throw new ArgumentNullException(nameof(availableSettingsPath));
            }

            if (string.IsNullOrEmpty(settingAlternativeNamesPath))
            {
                throw new ArgumentNullException(nameof(settingAlternativeNamesPath));
            }

            using (StreamReader reader = new StreamReader(availableSettingsPath))
            {
                string json = reader.ReadToEnd();
                var availableSettings = JsonConvert.DeserializeObject<List<AvailableSetting>>(json);
                BuildSettingSearchIndexes(availableSettings, availableSettingsPath);
            }

            using (StreamReader reader = new StreamReader(settingAlternativeNamesPath))
            {
                string json = reader.ReadToEnd();
                this.alternativeNameMap = JsonConvert.DeserializeObject<Dictionary<string, SettingAlternativeNames>>(json);

                if (this.alternativeNameMap.TryGetValue(DefaultAlternativeValueNamesKey, out SettingAlternativeNames settingAlternativeNames))
                {
                    this.defaultAlternativeValueNames = settingAlternativeNames.AlternativeValueNames;
                    this.alternativeNameMap.Remove(DefaultAlternativeValueNamesKey);
                }
            }
        }

        private SettingList(IDictionary<string, SettingAlternativeNames> alternativeNameMap, IDictionary<string, IList<string>> defaultAlternativeValueNames)
        {
            this.alternativeNameMap = alternativeNameMap;
            this.defaultAlternativeValueNames = defaultAlternativeValueNames;
        }

        /// <summary>
        /// Create a sub-list with only some of the available settings in this list.
        /// </summary>
        /// <param name="availableSettings">The settings that should be available in the sub-list.</param>
        /// <returns>The sub-list.</returns>
        public SettingList CreateSubList(IList<AvailableSetting> availableSettings)
        {
            SettingList subList = new SettingList(this.alternativeNameMap, this.defaultAlternativeValueNames);
            subList.BuildSettingSearchIndexes(availableSettings, "CreateSubList");
            return subList;
        }

        public IList<string> GetAllSettingNames()
        {
            return this.availableSettingNames;
        }

        /// <summary>
        /// Find an available setting.
        /// </summary>
        /// <param name="settingName">The name of the setting to look for.</param>
        /// <returns>The available setting or null if no such setting was found.</returns>
        public AvailableSetting FindSetting(string settingName)
        {
            this.availableSettingMap.TryGetValue(settingName, out AvailableSetting availableSetting);
            return availableSetting;
        }

        /// <summary>
        /// Find an available setting value.
        /// </summary>
        /// <param name="settingName">The name of the setting that the value belongs to.</param>
        /// <param name="settingValue">The name of the setting value to look for.</param>
        /// <returns>The available setting value or null if no such value was found.</returns>
        public AvailableSettingValue FindSettingValue(string settingName, string settingValue)
        {
            if (string.IsNullOrEmpty(settingName))
            {
                throw new ArgumentNullException(nameof(settingName));
            }

            var availableSetting = FindSetting(settingName);
            return FindSettingValue(availableSetting, settingValue);
        }

        /// <summary>
        /// Find an available setting value.
        /// </summary>
        /// <param name="availableSetting">The available setting that the value belongs to. If this is null, then null is returned.</param>
        /// <param name="settingValue">The name of the setting value to look for.</param>
        /// <returns>The available setting value or null if no such value was found.</returns>
        public AvailableSettingValue FindSettingValue(AvailableSetting availableSetting, string settingValue)
        {
            if (availableSetting == null)
            {
                throw new ArgumentNullException(nameof(availableSetting));
            }

            if (availableSetting != null && availableSetting.Values != null)
            {
                foreach (var availableValue in availableSetting.Values)
                {
                    if (Util.NullSafeEquals(availableValue.CanonicalName, settingValue))
                    {
                        return availableValue;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get the alternative names of a setting.
        /// If a setting has alternative names, that does NOT imply that the setting is available.
        /// </summary>
        /// <param name="settingName">The canonical name of the setting to look for.</param>
        /// <returns>The alternative names for that setting, including its canonical name.</returns>
        public IList<string> GetAlternativeNamesForSetting(string settingName)
        {
            if (string.IsNullOrEmpty(settingName))
            {
                throw new ArgumentNullException(nameof(settingName));
            }

            IList<string> alternativeNames = new List<string>();

            if (alternativeNameMap.TryGetValue(settingName, out SettingAlternativeNames settingAlternativeNames))
            {
                if (settingAlternativeNames.AlternativeNames != null)
                {
                    alternativeNames = settingAlternativeNames.AlternativeNames;
                }

                if (!alternativeNames.Contains(settingName))
                {
                    alternativeNames = Util.CopyList(alternativeNames);
                    alternativeNames.Add(settingName);
                }
            }

            return alternativeNames;
        }

        /// <summary>
        /// Get the alternative names of a setting value.
        /// If a setting value has alternative names, that does NOT imply that the setting value is available.
        /// </summary>
        /// <param name="settingName">The canonical name of the setting that the value belongs to.</param>
        /// <param name="settingValue">The canonical name of the setting value to look for.</param>
        /// <returns>The alternative names for that setting value, including its canonical name.</returns>
        public IList<string> GetAlternativeNamesForSettingValue(string settingName, string settingValue)
        {
            if (string.IsNullOrEmpty(settingName))
            {
                throw new ArgumentNullException(nameof(settingName));
            }

            if (string.IsNullOrEmpty(settingValue))
            {
                throw new ArgumentNullException(nameof(settingValue));
            }

            IList<string> alternativeNames = new List<string>();

            var found = false;
            if (alternativeNameMap.TryGetValue(settingName, out SettingAlternativeNames settingAlternativeNames))
            {
                if (settingAlternativeNames.AlternativeValueNames != null
                    && settingAlternativeNames.AlternativeValueNames.TryGetValue(settingValue, out IList<string> alternativeNameList))
                {
                    alternativeNames = alternativeNameList;
                    found = true;
                }
            }

            if (!found && this.defaultAlternativeValueNames != null
                && this.defaultAlternativeValueNames.TryGetValue(settingValue, out IList<string> defaultAlternativeNameList))
            {
                alternativeNames = defaultAlternativeNameList;
                found = true;
            }

            if (found && !alternativeNames.Contains(settingValue))
            {
                alternativeNames = Util.CopyList(alternativeNames);
                alternativeNames.Add(settingValue);
            }

            return alternativeNames;
        }

        /// <summary>
        /// Build out available settings.
        /// </summary>
        /// <param name="availableSettings">Available settings.</param>
        /// <param name="errorMsgPrefix">Prefix for error message.</param>
        private void BuildSettingSearchIndexes(IList<AvailableSetting> availableSettings, string errorMsgPrefix)
        {
            if (availableSettings == null)
            {
                throw new ArgumentNullException(nameof(availableSettings));
            }

            if (string.IsNullOrEmpty(errorMsgPrefix))
            {
                throw new ArgumentNullException(nameof(errorMsgPrefix));
            }

            foreach (var availableSetting in availableSettings)
            {
                if (availableSetting.CanonicalName == null)
                {
                    throw new JsonSerializationException($"{errorMsgPrefix}: The canonical name of an available setting was null.");
                }

                // Set the antonym relation to be symmetric
                var valuesMap = availableSetting.Values.ToDictionary(v => v.CanonicalName, v => v);
                foreach (var value in availableSetting.Values)
                {
                    if (!string.IsNullOrEmpty(value.Antonym))
                    {
                        if (valuesMap.TryGetValue(value.Antonym, out var antonymValue))
                        {
                            if (string.IsNullOrEmpty(antonymValue.Antonym))
                            {
                                antonymValue.Antonym = value.CanonicalName;
                            }
                            else
                            {
                                if (!antonymValue.Antonym.Equals(value.CanonicalName))
                                {
                                    throw new JsonSerializationException($"{errorMsgPrefix}: The available setting values '{value.CanonicalName}' and '{antonymValue.CanonicalName}' do not have symmetric antonyms.");
                                }
                            }
                        }
                        else
                        {
                            throw new JsonSerializationException($"{errorMsgPrefix}: The antonym '{value.Antonym}' of '{value.CanonicalName}' was not a canonical name of an available setting value.");
                        }
                    }
                }

                this.availableSettingNames.Add(availableSetting.CanonicalName);
                this.availableSettingMap.Add(availableSetting.CanonicalName, availableSetting);
            }
        }
    }
}