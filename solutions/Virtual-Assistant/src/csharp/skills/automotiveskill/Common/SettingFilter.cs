// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.IO;

    /// <summary>
    /// Filters the available device settings based on the NLU result and the state.
    /// </summary>
    public class SettingFilter
    {
        private static readonly double setting_name_score_threshold = 0.6;
        private static readonly double setting_name_antonym_disamb_percentage_of_max = 0.9;
        private static readonly double setting_value_score_threshold = 0.6;
        private static readonly double setting_value_antonym_disamb_threshold = 0.1;
        private static readonly double setting_value_antonym_disamb_percentage_of_max = 0.9;
        private static readonly Regex to_as_2_pattern = new Regex("^2[0-9][0-9]$", RegexOptions.Compiled);
        private static readonly IList<string> VALUE_RELATED_VALIDITIES = new List<string>()
        {
            "INVALID_MISSING_VALUE",
            "INVALID_SETTING_VALUE_COMBINATION",
            "INVALID_VALUE",
        };

        private readonly SettingList settingList;
        private readonly SettingMatcher settingMatcher;
        private readonly NumberNormalizer numberNormalizer;
        private readonly EntityNormalizer amount_normalizer;
        private readonly EntityNormalizer type_normalizer;
        private readonly EntityNormalizer unit_normalizer;
        private readonly EntityNormalizer index_normalizer;

        public SettingFilter(SettingList settingList)
        {
            this.settingList = settingList;
            this.settingMatcher = new SettingMatcher(this.settingList);
            this.numberNormalizer = new NumberNormalizer();
            this.amount_normalizer = new EntityNormalizer("Dialogs/VehicleSettings/Resources/normalization/amount_percentage.tsv");
            this.type_normalizer = new EntityNormalizer("Dialogs/VehicleSettings/Resources/normalization/amount_type.tsv");
            this.unit_normalizer = new EntityNormalizer("Dialogs/VehicleSettings/Resources/normalization/amount_unit.tsv");
            this.index_normalizer = new EntityNormalizer("Dialogs/VehicleSettings/Resources/normalization/index_map.tsv");
        }

        public void Filter(AutomotiveSkillState state, RecognizerResultWrapper luisResult)
        {
            if (state.DialogStateType == VehicleSettingStage.None)
            {
                PostProcessSettings(state, luisResult);
                ApplyContentLogic(state);
            }
            else if (state.DialogStateType == VehicleSettingStage.NameSelection)
            {
                if (Util.IsChangeIntent(state.Intent))
                {
                    state.Changes = ApplySelectionToSettings(state, luisResult, state.Changes);
                }
                else if (Util.IsCheckIntent(state.Intent))
                {
                    state.Statuses = ApplySelectionToSettings(state, luisResult, state.Statuses);
                }
            }
            else if (state.DialogStateType == VehicleSettingStage.ValueSelection)
            {
                state.Changes = ApplySelectionToSettingValues(state, luisResult);
            }
            else if (state.DialogStateType == VehicleSettingStage.ChangeConfirmation)
            {
                ApplyConfirmation(state, luisResult);
            }
        }

        private void PostProcessSettings(AutomotiveSkillState state, RecognizerResultWrapper luisResult)
        {
            IList<SettingMatch> setting_matches = new List<SettingMatch>();
            var has_matching_value_for_any_setting = false;
            ISet<string> setting_names_to_remove = new HashSet<string>();

            IList<AvailableSetting> selected_settings = new List<AvailableSetting>();
            if (state.Entities.ContainsKey("SETTING"))
            {
                selected_settings = this.settingMatcher.MatchSettingNamesExactly(luisResult, "SETTING");

                if (!selected_settings.Any() && state.Entities.ContainsKey("VALUE"))
                {
                    // First try SETTING + VALUE entities combined to catch cases like "warm my seat",
                    // where the value can help disambiguate which setting the user meant.
                    IList<string> entityTypes = new List<string>
                    {
                        "SETTING",
                        "VALUE",
                    };
                    selected_settings = this.settingMatcher.MatchSettingNames(luisResult, entityTypes,
                  setting_name_score_threshold, setting_name_antonym_disamb_percentage_of_max, false);
                }

                if (!selected_settings.Any())
                {
                    IList<string> entityTypes = new List<string>
                    {
                        "SETTING",
                    };
                    selected_settings = this.settingMatcher.MatchSettingNames(luisResult, entityTypes,
                  setting_name_score_threshold, setting_name_antonym_disamb_percentage_of_max, false);
                }

            }
            else if ("SETTING_STATUS_FOLLOWUP".Equals(state.DialogStateType)
              && ("VEHICLE_SETTINGS_CHANGE".Equals(state.Intent) || "VEHICLE_SETTINGS_DECLARATIVE".Equals(state.Intent)))
            {
                // TODO get the settings from the Statuses on the state
                //var arguments = query.dialogue().arguments<SettingNameSelectionArguments>();
                //selected_settings = arguments.settings();
            }

            if (selected_settings.Any())
            {
                IList<string> entity_types_for_value_disamb = new List<string>();
                if (state.Entities.ContainsKey("VALUE"))
                {
                    entity_types_for_value_disamb.Add("VALUE");
                }
                else if (state.Entities.ContainsKey("SETTING"))
                {
                    // Sometimes the setting name itself is also a value, e.g., "defog"
                    entity_types_for_value_disamb.Add("SETTING");
                }

                foreach (var setting_info in selected_settings)
                {
                    IList<SelectableSettingValue> selected_values = new List<SelectableSettingValue>();

                    if (entity_types_for_value_disamb.Any())
                    {
                        IList<SelectableSettingValue> selectable_values = new List<SelectableSettingValue>();
                        foreach (var value in setting_info.Values)
                        {
                            SelectableSettingValue selectable = new SelectableSettingValue
                            {
                                canonicalSettingName = setting_info.CanonicalName,
                                value = value
                            };
                            selectable_values.Add(selectable);
                        }

                        selected_values = this.settingMatcher.DisambiguateSettingValues(luisResult, entity_types_for_value_disamb,
                            selectable_values, setting_value_antonym_disamb_threshold, setting_value_antonym_disamb_percentage_of_max);

                        // If we don't even have a VALUE entity, we can't match multiple values.
                        // If the SETTING entity is really also a value, then it must match only one value.
                        if (!state.Entities.ContainsKey("VALUE") && selected_values.Count() > 1)
                        {
                            selected_values.Clear();
                        }

                        foreach (var selected_value in selected_values)
                        {
                            SettingMatch match = new SettingMatch
                            {
                                setting_name = setting_info.CanonicalName,
                                value = selected_value.value.CanonicalName
                            };
                            setting_matches.Add(match);
                            has_matching_value_for_any_setting = true;
                        }
                    }

                    if (!selected_values.Any())
                    {
                        SettingMatch match = new SettingMatch
                        {
                            setting_name = setting_info.CanonicalName
                        };
                        setting_matches.Add(match);
                    }

                    AddAll(setting_names_to_remove, setting_info.IncludedSettings);
                }

            }
            else if (state.Entities.ContainsKey("VALUE") && !state.Entities.ContainsKey("SETTING"))
            {
                // If we have no SETTING entity, match the VALUE entities against all the values of all the settings.
                // This handles queries like "make it warmer" or "defog", where the value implies the setting.
                IList<string> entityTypes = new List<string>
                {
                    "VALUE"
                };
                setting_matches = this.settingMatcher.MatchSettingValues(luisResult, entityTypes,
                setting_value_score_threshold, setting_value_antonym_disamb_percentage_of_max);

                has_matching_value_for_any_setting = true;

                foreach (var match in setting_matches)
                {
                    var setting_info = this.settingList.FindSetting(match.setting_name);
                    if (setting_info != null)
                    {
                        AddAll(setting_names_to_remove, setting_info.IncludedSettings);
                    }
                }
            }

            // If at least one setting has a matching value, remove all settings with no matching value.
            // This effectively disambiguates the settings by their available values.
            // Also remove 'included' settings.
            IList<SettingMatch> new_setting_matches = new List<SettingMatch>();
            foreach (var match in setting_matches)
            {
                if ((!has_matching_value_for_any_setting || !string.IsNullOrEmpty(match.value))
                    && !setting_names_to_remove.Contains(match.setting_name))
                {
                    new_setting_matches.Add(match);
                }
            }
            setting_matches = new_setting_matches;

            if ("VEHICLE_SETTINGS_CHECK".Equals(state.Intent))
            {
                foreach (var setting_match in setting_matches)
                {
                    SettingStatus setting_status = new SettingStatus
                    {
                        SettingName = setting_match.setting_name
                    };
                    state.Statuses.Add(setting_status);
                }

            }
            else
            {
                var (opt_amount, isRelative) = OptionalAmount(state, false);

                foreach (var setting_match in setting_matches)
                {
                    SettingChange setting_change = new SettingChange
                    {
                        SettingName = setting_match.setting_name
                    };

                    var value_info = this.settingList.FindSettingValue(setting_match.setting_name, setting_match.value);
                    if ("VEHICLE_SETTINGS_DECLARATIVE".Equals(state.Intent))
                    {
                        // If the user makes a declarative statement, it means that they're unhappy with the status quo.
                        // So, we use the antonym of the value to get the opposite of the thing they're unhappy with,
                        // which should hopefully make them happy.
                        // If there is no antonym listed, then we want to return an empty value because we were unable to find
                        // the correct value.
                        if (value_info != null)
                        {
                            setting_change.Value = value_info.Antonym;
                        }
                    }
                    else
                    {
                        setting_change.Value = setting_match.value;
                    }

                    if (opt_amount != null && value_info != null && value_info.ChangesSignOfAmount)
                    {
                        (opt_amount, isRelative) = OptionalAmount(state, true);
                    }
                    setting_change.Amount = opt_amount;
                    setting_change.IsRelativeAmount = isRelative;

                    state.Changes.Add(setting_change);
                }

                if (!setting_matches.Any() && opt_amount != null)
                {
                    SettingChange setting_change = new SettingChange
                    {
                        Amount = opt_amount,
                        IsRelativeAmount = isRelative
                    };
                    state.Changes.Add(setting_change);
                }
            }
        }

        private (SettingAmount amount, bool isRelative) OptionalAmount(AutomotiveSkillState state, bool change_sign_of_amount)
        {
            SettingAmount optional_amount = null;
            bool isRelative = false;

            if (state.Entities.TryGetValue("AMOUNT", out var amountEntityValues))
            {
                foreach (var amount_entity_value in amountEntityValues)
                {
                    var normalized_amount = this.amount_normalizer.NormalizeOrNull(amount_entity_value);
                    if (normalized_amount != null)
                    {
                        optional_amount = new SettingAmount
                        {
                            Unit = "%"
                        };

                        if ("+-".Equals(normalized_amount))
                        {
                            if (change_sign_of_amount)
                            {
                                optional_amount.Amount = 0.0;
                            }
                            else
                            {
                                optional_amount.Amount = 100.0;
                            }
                        }
                        else
                        {
                            optional_amount.Amount = double.Parse(normalized_amount, CultureInfo.InvariantCulture);
                        }

                    }
                    else
                    {
                        foreach (var chunk in this.numberNormalizer.SplitNumbers(amount_entity_value))
                        {
                            if (chunk.numeric_value != null)
                            {
                                optional_amount = new SettingAmount();
                                optional_amount.Amount = chunk.numeric_value.Value;

                                // Deal with ASR error that transcribes "to 24" as "224"
                                if (!state.Entities.ContainsKey("TYPE") && to_as_2_pattern.Match(amount_entity_value).Success)
                                {
                                    optional_amount.Amount = optional_amount.Amount - 200;
                                    isRelative = false;
                                }
                                break;
                            }
                        }
                    }

                    if (optional_amount != null)
                    {
                        if (state.Entities.TryGetValue("TYPE", out var typeEntityValues))
                        {
                            foreach (var typeEntityValue in typeEntityValues)
                            {
                                var normalized_type = this.type_normalizer.NormalizeOrNull(typeEntityValue);
                                if (normalized_type != null)
                                {
                                    isRelative = "DELTA".Equals(normalized_type);
                                    break;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(optional_amount.Unit) && state.Entities.TryGetValue("UNIT", out var unitEntityValues))
                        {
                            foreach (var unitEntityValue in unitEntityValues)
                            {
                                var normalized_unit = this.unit_normalizer.NormalizeOrNull(unitEntityValue);
                                if (normalized_unit != null)
                                {
                                    optional_amount.Unit = normalized_unit;
                                }
                                else
                                {
                                    optional_amount.Unit = unitEntityValue;
                                }
                                break;
                            }
                        }

                        break;
                    }
                }
            }

            if (change_sign_of_amount && optional_amount != null && isRelative)
            {
                optional_amount.Amount = -optional_amount.Amount;
            }

            return (optional_amount, isRelative);
        }

        private void AddAll(ISet<string> to, IList<string> from)
        {
            if (from != null)
            {
                foreach (var element in from)
                {
                    to.Add(element);
                }
            }
        }

        private void ApplyContentLogic(AutomotiveSkillState state)
        {
            if (Util.IsChangeIntent(state.Intent) && !Util.IsNullOrEmpty(state.Changes))
            {
                IList<SettingChange> validChanges = new List<SettingChange>();
                IList<SettingChange> invalidChanges = new List<SettingChange>();
                foreach (var change in state.Changes)
                {
                    var validity = ValidateChange(change);
                    if ("VALID".Equals(validity))
                    {
                        validChanges.Add(change);
                    }
                    else if (VALUE_RELATED_VALIDITIES.Contains(validity))
                    {
                        var settingInfo = settingList.FindSetting(change.SettingName);
                        if (settingInfo != null && !Util.IsNullOrEmpty(settingInfo.Values))
                        {
                            IList<SettingChange> validReplacements = new List<SettingChange>();
                            string replacementValidity = null;
                            foreach (var valueInfo in settingInfo.Values)
                            {
                                var newChange = (SettingChange)change.Clone();
                                newChange.Value = valueInfo.CanonicalName;
                                validity = ValidateChange(newChange);
                                if ("VALID".Equals(validity))
                                {
                                    validReplacements.Add(newChange);
                                }
                                else if (replacementValidity == null)
                                {
                                    replacementValidity = validity;
                                }
                            }
                            if (!Util.IsNullOrEmpty(validReplacements))
                            {
                                state.Entities.Remove("VALUE");
                                foreach (var replacement in validReplacements)
                                {
                                    validChanges.Add(replacement);
                                }
                            }
                            else
                            {
                                invalidChanges.Add(change);
                            }
                        }
                        else
                        {
                            invalidChanges.Add(change);
                        }

                    }
                    else
                    {
                        invalidChanges.Add(change);
                    }
                }

                if (validChanges.Any())
                {
                    state.Changes = validChanges;
                }
                else
                {
                    state.Changes = invalidChanges;
                }
            }
        }

        private string ValidateChange(SettingChange setting)
        {
            var validity = "VALID";

            if (string.IsNullOrEmpty(setting.SettingName))
            {
                return "INVALID_MISSING_SETTING";
            }

            if (string.IsNullOrEmpty(setting.Value) && setting.Amount == null)
            {
                return "INVALID_MISSING_VALUE";
            }

            var settingInfo = settingList.FindSetting(setting.SettingName);
            if (settingInfo == null)
            {
                return "INVALID_SETTING_NAME";
            }

            AvailableSettingValue settingValueInfo = null;
            foreach (var valueInfo in settingInfo.Values)
            {
                if (Util.NullSafeEquals(setting.Value, valueInfo.CanonicalName)
                        || (string.IsNullOrEmpty(setting.Value) && "SET".Equals(valueInfo.CanonicalName.ToUpperInvariant())))
                {
                    settingValueInfo = valueInfo;
                    setting.Value = valueInfo.CanonicalName;
                    break;
                }
            }
            if (settingValueInfo == null)
            {
                return "INVALID_SETTING_VALUE_COMBINATION";
            }

            if (setting.Amount == null)
            {
                if (settingValueInfo.RequiresAmount)
                {
                    validity = "INVALID_MISSING_AMOUNT";
                }
                return validity;
            }

            if (!settingInfo.AllowsAmount || Util.IsNullOrEmpty(settingInfo.Amounts))
            {
                return "INVALID_EXTRA_AMOUNT";
            }

            AvailableSettingAmount settingAmountInfo = null;
            foreach (var amountInfo in settingInfo.Amounts)
            {
                if (Util.NullSafeEquals(setting.Amount.Unit, amountInfo.Unit))
                {
                    settingAmountInfo = amountInfo;
                    break;
                }
            }
            if (settingAmountInfo == null)
            {
                if ("%".Equals(setting.Amount.Unit))
                {
                    settingAmountInfo = new AvailableSettingAmount()
                    {
                        Unit = "%",
                        Min = 0,
                        Max = 100,
                    };
                }
                else
                {
                    return "INVALID_AMOUNT_UNIT";
                }
            }

            if (!setting.IsRelativeAmount)
            {
                if (setting.Amount.Amount < settingAmountInfo.Min || setting.Amount.Amount > settingAmountInfo.Max)
                {
                    return "INVALID_AMOUNT_OUT_OF_RANGE";
                }

            }
            else if (settingAmountInfo.Min != null && settingAmountInfo.Max != null)
            {
                var maxRelative = settingAmountInfo.Max - settingAmountInfo.Min;
                var minRelative = -maxRelative;
                if (setting.Amount.Amount < minRelative || setting.Amount.Amount > maxRelative)
                {
                    return "INVALID_AMOUNT_OUT_OF_RANGE";
                }
            }

            return validity;
        }

        private IList<T> ApplySelectionToSettings<T>(AutomotiveSkillState state, RecognizerResultWrapper luisResult, IList<T> changesOrStatuses) where T : SettingOperation
        {
            var settingNames = state.GetUniqueSettingNames();

            IList<string> entityTypes = new List<string>();
            if (luisResult.HasEntity("SETTING"))
            {
                entityTypes.Add("SETTING");
            }
            else if (luisResult.HasEntity("VALUE"))
            {
                entityTypes.Add("VALUE");
            }

            ISet<string> selectedSettingNames = new HashSet<string>();
            if (entityTypes.Any() && settingNames.Any())
            {
                IList<AvailableSetting> resolvedSettings = new List<AvailableSetting>();
                foreach (var settingName in settingNames)
                {
                    var setting = this.settingList.FindSetting(settingName);
                    if (setting != null)
                    {
                        resolvedSettings.Add(setting);
                    }
                    else
                    {
                        setting = new AvailableSetting
                        {
                            CanonicalName = settingName
                        };
                        resolvedSettings.Add(setting);
                    }
                }

                IList<AvailableSetting> settings_to_select_from = Util.CopyList(resolvedSettings);
                foreach (var setting in resolvedSettings)
                {
                    if (setting.IncludedSettings != null)
                    {
                        foreach (var included_setting_name in setting.IncludedSettings)
                        {
                            if (!settingNames.Contains(included_setting_name))
                            {
                                var included_setting = this.settingList.FindSetting(included_setting_name);
                                if (included_setting == null)
                                {
                                    // Unreachable.
                                    throw new Exception("The included settings of setting \"" + setting.CanonicalName
                                        + "\" must be canonical names of other settings, but \"" + included_setting_name
                                        + "\" is not and this should already have been checked when loading the SettingList.");
                                }
                                settings_to_select_from.Add(included_setting);
                            }
                        }
                    }
                }

                var setting_matcher = new SettingMatcher(this.settingList.CreateSubList(settings_to_select_from));
                var selected_settings = setting_matcher.MatchSettingNamesExactly(luisResult, entityTypes[0]);

                if (!selected_settings.Any())
                {
                    selected_settings = setting_matcher.MatchSettingNames(luisResult, entityTypes,
                        setting_name_score_threshold, setting_name_antonym_disamb_percentage_of_max, true);
                }

                foreach (var setting_info in selected_settings)
                {
                    selectedSettingNames.Add(setting_info.CanonicalName);
                }
            }

            var (selectedIndices, hasIndexLast) = GetSelectedIndices(luisResult);
            var hasAnyIndex = hasIndexLast || selectedIndices.Any();

            if (settingNames.Count() <= 1
                    || !(hasAnyIndex || selectedSettingNames.Any()))
            {
                return changesOrStatuses;
            }

            // Indices are relative to settingNames, because that's what's shown to the user.
            for (var i = 0; i < settingNames.Count(); ++i)
            {
                if ((hasIndexLast && i == settingNames.Count() - 1)
                        || selectedIndices.Contains(i))
                {
                    selectedSettingNames.Add(settingNames[i]);
                }
            }

            IList<T> newCandidates = new List<T>();
            ISet<string> addedSettingNames = new HashSet<string>();
            foreach (var candidate in changesOrStatuses)
            {
                if (candidate == null)
                {
                    continue;
                }

                if (selectedSettingNames.Contains(candidate.SettingName))
                {
                    newCandidates.Add(candidate);
                    addedSettingNames.Add(candidate.SettingName);
                }
            }

            // If NLP tells us to select something that isn't on the list,
            // it's because it's included in one of the settings on the list.
            foreach (var selectedName in selectedSettingNames)
            {
                if (!addedSettingNames.Contains(selectedName))
                {
                    // This search is inefficient, but the lists will be short, so it doesn't matter.
                    foreach (var candidate in changesOrStatuses)
                    {
                        var supportedSetting = settingList.FindSetting(candidate.SettingName);
                        if (supportedSetting != null && supportedSetting.IncludedSettings != null && supportedSetting.IncludedSettings.Contains(selectedName))
                        {
                            var newCandidate = (T)candidate.Clone();
                            newCandidate.SettingName = selectedName;
                            newCandidates.Add(newCandidate);
                            break;
                        }
                    }
                }
            }

            if (!Util.IsNullOrEmpty(newCandidates))
            {
                return newCandidates;
            }

            return changesOrStatuses;
        }

        private IList<SettingChange> ApplySelectionToSettingValues(AutomotiveSkillState state, RecognizerResultWrapper luisResult)
        {
            var settingValues = state.GetUniqueSettingValues();

            IList<string> entityTypes = new List<string>();
            if (state.Entities.ContainsKey("VALUE"))
            {
                entityTypes.Add("VALUE");
            }
            else if (state.Entities.ContainsKey("SETTING"))
            {
                entityTypes.Add("SETTING");
            }

            ISet<string> selectedSettingValues = new HashSet<string>();
            if (entityTypes.Any() && settingValues.Any())
            {
                IList<SelectableSettingValue> selectableSettingValues = new List<SelectableSettingValue>();
                foreach (var change in state.Changes)
                {
                    SelectableSettingValue selectable = new SelectableSettingValue
                    {
                        canonicalSettingName = change.SettingName
                    };
                    var availableValue = this.settingList.FindSettingValue(change.SettingName, change.Value);
                    if (availableValue != null)
                    {
                        selectable.value = availableValue;
                    }
                    else
                    {
                        availableValue = new AvailableSettingValue
                        {
                            CanonicalName = change.Value
                        };
                        selectable.value = availableValue;
                    }
                    selectableSettingValues.Add(selectable);
                }

                var selected_values = this.settingMatcher.DisambiguateSettingValues(luisResult, entityTypes,
                    selectableSettingValues, setting_value_antonym_disamb_threshold, setting_value_antonym_disamb_percentage_of_max);

                foreach (var selected_value in selected_values)
                {
                    selectedSettingValues.Add(selected_value.value.CanonicalName);
                }
            }

            var (selectedIndices, hasIndexLast) = GetSelectedIndices(luisResult);
            var hasAnyIndex = hasIndexLast || selectedIndices.Any();

            if (settingValues.Count() <= 1
              || !(hasAnyIndex || selectedSettingValues.Any()))
            {
                return state.Changes;
            }

            // Indices are relative to settingValues, because that's what's shown to the user.
            if (!Util.IsNullOrEmpty(settingValues))
            {
                for (var i = 0; i < settingValues.Count(); ++i)
                {
                    if ((hasIndexLast && i == settingValues.Count() - 1)
                            || selectedIndices.Contains(i))
                    {
                        selectedSettingValues.Add(settingValues[i]);
                    }
                }
            }

            IList<SettingChange> newCandidates = new List<SettingChange>();
            foreach (var candidate in state.Changes)
            {
                if (candidate == null)
                {
                    continue;
                }

                if (selectedSettingValues.Contains(candidate.Value))
                {
                    newCandidates.Add(candidate);
                }
            }

            if (!Util.IsNullOrEmpty(newCandidates))
            {
                return newCandidates;
            }

            return state.Changes;
        }

        private (ISet<int>, bool) GetSelectedIndices(RecognizerResultWrapper luisResult)
        {
            ISet<int> selectedIndices = new HashSet<int>();
            bool hasIndexLast = false;
            var indexEntityValues = luisResult.GetEntityValues("INDEX");
            if (indexEntityValues.Any())
            {
                foreach (var indexEntityValue in indexEntityValues)
                {
                    var newIndexEntityValue = this.index_normalizer.Normalize(indexEntityValue);
                    if ("LAST".Equals(newIndexEntityValue))
                    {
                        hasIndexLast = true;
                    }
                    else
                    {
                        try
                        {
                            // Humans speak in one-based indices, but we need zero-based indices.
                            selectedIndices.Add(int.Parse(newIndexEntityValue) - 1);
                        }
                        catch (FormatException)
                        { }
                        catch (OverflowException)
                        { }
                    }
                }
            }
            return (selectedIndices, hasIndexLast);
        }

        private void ApplyConfirmation(AutomotiveSkillState state, RecognizerResultWrapper luisResult)
        {
            if (state.Changes.Any() && "SETTING_CHANGE_CONFIRMATION_YES".Equals(luisResult.GetIntent()))
            {
                var change = state.Changes[0];
                change.IsConfirmed = true;
            }
        }
    }

    public class SettingMatcher
    {
        private static readonly Regex whitespace = new Regex("\\s+", RegexOptions.Compiled);

        private readonly SettingList settingList;
        private readonly IList<MatchableBagOfTokens> matchable_setting_name_bags = new List<MatchableBagOfTokens>();
        private readonly IList<MatchableBagOfTokens> matchable_value_name_bags = new List<MatchableBagOfTokens>();
        private readonly IDictionary<string, AvailableSetting> pre_processed_canonical_name_map = new Dictionary<string, AvailableSetting>();

        public SettingMatcher(SettingList settingList)
        {
            this.settingList = settingList;

            foreach (var settingName in this.settingList.GetAllSettingNames())
            {
                var setting_info = this.settingList.FindSetting(settingName);
                var pre_processed_canonical_name = PreProcessName(setting_info.CanonicalName);

                var alternative_names = this.settingList.GetAlternativeNamesForSetting(setting_info.CanonicalName);

                matchable_setting_name_bags.Add(MakeMatchableBagOfTokens(pre_processed_canonical_name,
                alternative_names,
                setting_info.CanonicalName,
                ""));

                this.pre_processed_canonical_name_map.Add(pre_processed_canonical_name, setting_info);

                foreach (var value_info in setting_info.Values)
                {
                    var alternativeValueNames = this.settingList.GetAlternativeNamesForSettingValue(setting_info.CanonicalName, value_info.CanonicalName);
                    matchable_value_name_bags.Add(MakeMatchableBagOfTokens(PreProcessName(value_info.CanonicalName),
                      alternativeValueNames,
                      setting_info.CanonicalName,
                      value_info.CanonicalName));
                }
            }
        }

        private IList<MatchResult> FindNearestMatchesWithin(MatchableBagOfTokens matchable_entity_bag, IList<MatchableBagOfTokens> matchable_names, double threshold)
        {
            IList<MatchResult> matches = new List<MatchResult>();
            foreach (var matchable_name in matchable_names)
            {
                double score = ComputeSimilarityScore(matchable_entity_bag, matchable_name);
                MatchResult match = new MatchResult
                {
                    element = matchable_name,
                    score = score
                };
                matches.Add(match);
            }
            matches = matches.OrderByDescending(match => match.score).ToList();

            IList<MatchResult> selected_matches = new List<MatchResult>();
            if (matches.Any())
            {
                double max_score = matches[0].score;
                if (max_score > 0)
                {
                    foreach (var match in matches)
                    {
                        if (match.score >= threshold)
                        {
                            selected_matches.Add(match);
                        }
                    }
                }
            }

            return selected_matches;
        }

        private string PreProcessName(string name)
        {
            // Setting and value names are sometimes ALL_UPPERCASE_WITH_UNDERSCORES.
            // We replace the underscores with spaces so that we get separate tokens like e.g.,
            // [ALL, UPPERCASE, WITH, UNDERSCORES]
            var pre_processed_name = name.Replace("_", " ");
            return PreProcessPartial(pre_processed_name);
        }

        private string PreProcessPartial(string text)
        {
            text = text.ToLowerInvariant();
            text = whitespace.Replace(text, " ");
            return text;
        }

        private IList<string> Tokenize(string text)
        {
            return whitespace.Split(text).ToList();
        }

        private void AddNameToMatchable(MatchableBagOfTokens matchable, string pre_processed_name)
        {
            var tokens = Tokenize(pre_processed_name);
            IList<string> tokens_per_name = new List<string>();

            foreach (var token in tokens)
            {
                matchable.tokens.Add(token);
                tokens_per_name.Add(token);
            }

            matchable.tokens_list.Add(tokens_per_name);
        }

        private MatchableBagOfTokens MakeMatchableBagOfTokens(RecognizerResultWrapper luisResult, IList<string> entity_types)
        {
            MatchableBagOfTokens matchable = new MatchableBagOfTokens();

            IList<string> names = new List<string>();
            foreach (var entityType in entity_types)
            {
                foreach (var entityValue in luisResult.GetEntityValues(entityType))
                {
                    names.Add(entityValue);
                }
            }
            string extracted_setting_name = string.Join(" ", names);

            AddNameToMatchable(matchable, extracted_setting_name);

            return matchable;
        }

        private MatchableBagOfTokens MakeMatchableBagOfTokens(string pre_processed_canonical_name, IList<string> alternative_names,
            string canonical_setting_name, string canonical_value_name)
        {
            MatchableBagOfTokens matchable = new MatchableBagOfTokens
            {
                canonical_setting_name = canonical_setting_name,
                canonical_value_name = canonical_value_name
            };

            AddNameToMatchable(matchable, pre_processed_canonical_name);
            foreach (var name in alternative_names)
            {
                AddNameToMatchable(matchable, PreProcessName(name));
            }

            return matchable;
        }

        private IList<MatchableBagOfTokens> FindSemanticMatches(MatchableBagOfTokens matchable_entity_bag,
            IList<MatchableBagOfTokens> matchable_names,
            double semantic_threshold)
        {
            var matches = FindNearestMatchesWithin(matchable_entity_bag, matchable_names, semantic_threshold);

            IList<MatchableBagOfTokens> matching_bags = new List<MatchableBagOfTokens>();
            foreach (var match in matches)
            {
                matching_bags.Add(match.element);
            }

            return matching_bags;
        }

        // We disambiguate between antonyms using TF-IDF with the names of each candidate forming a "document."
        private IList<MatchableBagOfTokens> DisambiguateAntonyms(MatchableBagOfTokens matchable_entity_bag,
            IList<MatchableBagOfTokens> matchable_candidate_bags,
            double threshold,
            double percentage_of_max,
            bool use_coverage_filter)
        {
            // Precompute the document frequency.
            IDictionary<string, int> document_freq = new Dictionary<string, int>();
            foreach (var matchable_candidate_bag in matchable_candidate_bags)
            {
                foreach (var token in UniqueElements(matchable_candidate_bag.tokens))
                {
                    if (!document_freq.TryAdd(token, 1))
                    {
                        ++document_freq[token];
                    }
                }
            }

            IList<ScoredMatchableBagOfTokens> tf_idf_scored_bags = new List<ScoredMatchableBagOfTokens>();
            IDictionary<string, IDictionary<string, double>> coverage_scores = new Dictionary<string, IDictionary<string, double>>();
            foreach (var matchable_candidate_bag in matchable_candidate_bags)
            {
                ISet<string> matching_tokens = new HashSet<string>();
                double tf_idf_sum = 0.0;
                foreach (var token in matchable_entity_bag.tokens)
                {
                    // Using binary TF because the alternative names contain a lot of repeated tokens and are not weighted by
                    // how frequent each name is in natural language, so the numeric TF of a token is not representative of how
                    // much that token is associated with the setting/ value.
                    double tf_idf = 0.0;
                    if (matchable_candidate_bag.tokens.Contains(token))
                    {
                        matching_tokens.Add(token);
                        if (document_freq.TryGetValue(token, out var freq))
                        {
                            tf_idf = 1.0 / freq;
                        }
                    }
                    tf_idf_sum += tf_idf;
                }
                var tf_idf_avg = tf_idf_sum / matchable_entity_bag.tokens.Count();

                // Given two candidates with a similar score, we want to select the one that has the highest percentage of
                // matching tokens out of all its tokens. E.g., if one candidate is a substring of the other and the entities
                // match the shared substring, we want to select the shorter candidate because it has a better coverage.
                if (use_coverage_filter)
                {
                    ISet<string> unique_candidate_tokens = new HashSet<string>(UniqueElements(matchable_candidate_bag.tokens));
                    var coverage_score = (double)matching_tokens.Count() / unique_candidate_tokens.Count();
                    if (!coverage_scores.TryGetValue(matchable_candidate_bag.canonical_setting_name, out IDictionary<string, double> innerMap))
                    {
                        innerMap = new Dictionary<string, double>();
                    }
                    innerMap.Add(matchable_candidate_bag.canonical_value_name, coverage_score);
                    coverage_scores[matchable_candidate_bag.canonical_setting_name] = innerMap;
                }

                if (tf_idf_avg > threshold)
                {
                    ScoredMatchableBagOfTokens scoredBag = new ScoredMatchableBagOfTokens
                    {
                        option = matchable_candidate_bag,
                        score = tf_idf_avg
                    };
                    tf_idf_scored_bags.Add(scoredBag);
                }
            }

            var selected_tf_idf_scored_bags = SelectPercentageOfMax(tf_idf_scored_bags, percentage_of_max);

            IList<ScoredMatchableBagOfTokens> selected_scored_bags;
            if (use_coverage_filter)
            {
                IList<ScoredMatchableBagOfTokens> coverage_scored_bags = new List<ScoredMatchableBagOfTokens>();
                foreach (var tf_idf_scored_bag in selected_tf_idf_scored_bags)
                {
                    ScoredMatchableBagOfTokens scoredBag = new ScoredMatchableBagOfTokens
                    {
                        option = tf_idf_scored_bag.option,
                        score = coverage_scores[tf_idf_scored_bag.option.canonical_setting_name][tf_idf_scored_bag.option.canonical_value_name]
                    };
                    coverage_scored_bags.Add(scoredBag);
                }
                selected_scored_bags = SelectPercentageOfMax(coverage_scored_bags, percentage_of_max);
            }
            else
            {
                selected_scored_bags = selected_tf_idf_scored_bags;
            }

            IList<MatchableBagOfTokens> selected_candidate_bags = new List<MatchableBagOfTokens>();
            foreach (var scored_bag in selected_scored_bags)
            {
                selected_candidate_bags.Add(scored_bag.option);
            }
            return selected_candidate_bags;
        }

        private IList<string> UniqueElements(IList<string> list)
        {
            IList<string> uniqueList = new List<string>();
            ISet<string> seenElements = new HashSet<string>();
            foreach (var element in list)
            {
                if (seenElements.Add(element))
                {
                    uniqueList.Add(element);
                }
            }
            return uniqueList;
        }

        private IList<ScoredMatchableBagOfTokens> SelectPercentageOfMax(IList<ScoredMatchableBagOfTokens> scored_options, double percentage_of_max)
        {
            if (scored_options.Count() < 2)
            {
                return scored_options;
            }

            scored_options = scored_options.OrderByDescending(scoredOption => scoredOption.score).ToList();

            IList<ScoredMatchableBagOfTokens> selected = new List<ScoredMatchableBagOfTokens>();
            var threshold = scored_options[0].score * percentage_of_max;
            foreach (var scoredOption in scored_options)
            {
                // It's sorted by score, so we can stop once the score is no longer above the threshold.
                if (scoredOption.score <= threshold)
                {
                    break;
                }
                selected.Add(scoredOption);
            }

            return selected;
        }

        public IList<AvailableSetting> MatchSettingNamesExactly(RecognizerResultWrapper luisResult, string entityType)
        {
            string entity_str = string.Join(" ", luisResult.GetEntityValues(entityType));

            if (this.pre_processed_canonical_name_map.TryGetValue(entity_str, out var setting_info))
            {
                return new List<AvailableSetting> { setting_info };
            }
            else
            {
                return new List<AvailableSetting>();
            }
        }

        public IList<AvailableSetting> MatchSettingNames(RecognizerResultWrapper luisResult,
            IList<string> entity_types,
            double semantic_threshold,
            double antonym_disamb_percentage_of_max,
            bool use_coverage_filter)
        {
            var matchable_entity_bag = this.MakeMatchableBagOfTokens(luisResult, entity_types);
            if (matchable_entity_bag.IsEmpty())
            {
                return new List<AvailableSetting>();
            }
            var matching_bags = this.FindSemanticMatches(matchable_entity_bag, this.matchable_setting_name_bags, semantic_threshold);

            // If there are multiple semantic matches, they might be antonyms of each other (e.g., "right" vs. "left") because
            // the embeddings of antonyms are often very similar.
            if (matching_bags.Count() > 1)
            {
                matching_bags = this.DisambiguateAntonyms(matchable_entity_bag,
                    matching_bags,
                    0.0,
                    antonym_disamb_percentage_of_max,
                    use_coverage_filter);
            }

            IList<AvailableSetting> selected_settings = new List<AvailableSetting>();
            foreach (var bag in matching_bags)
            {
                var setting_info = this.settingList.FindSetting(bag.canonical_setting_name);
                if (setting_info != null)
                {
                    selected_settings.Add(setting_info);
                }
                else
                {
                    // This should be impossible to reach because we made the bags based on the setting list.
                    throw new Exception("Failed to find setting with canonical name: " + bag.canonical_setting_name);
                }
            }
            return selected_settings;
        }

        public IList<SettingMatch> MatchSettingValues(RecognizerResultWrapper luisResult,
            IList<string> entity_types,
            double semantic_threshold,
            double antonym_disamb_percentage_of_max)
        {
            var matchable_entity_bag = this.MakeMatchableBagOfTokens(luisResult, entity_types);
            if (matchable_entity_bag.IsEmpty())
            {
                return new List<SettingMatch>();
            }
            var matching_bags = this.FindSemanticMatches(matchable_entity_bag, this.matchable_value_name_bags, semantic_threshold);

            // If there are multiple semantic matches, they might be antonyms of each other (e.g., "right" vs. "left") because
            // the embeddings of antonyms are often very similar.
            if (matching_bags.Count() > 1)
            {
                matching_bags = this.DisambiguateAntonyms(matchable_entity_bag,
                    matching_bags,
                    0.0,
                    antonym_disamb_percentage_of_max,
                    false);
            }

            IList<SettingMatch> matches = new List<SettingMatch>();
            foreach (var bag in matching_bags)
            {
                SettingMatch match = new SettingMatch
                {
                    setting_name = bag.canonical_setting_name,
                    value = bag.canonical_value_name
                };
                matches.Add(match);
            }
            return matches;
        }

        public IList<SelectableSettingValue> DisambiguateSettingValues(RecognizerResultWrapper luisResult,
            IList<string> entity_types,
            IList<SelectableSettingValue> values,
            double antonym_disamb_threshold,
            double antonym_disamb_percentage_of_max)
        {
            if (!values.Any())
            {
                return new List<SelectableSettingValue>();
            }

            // Not using semantic matching because we expect the values to be antonyms of each other, e.g., "on" and "off"

            var matchable_entity_bag = this.MakeMatchableBagOfTokens(luisResult, entity_types);
            if (matchable_entity_bag.IsEmpty())
            {
                return new List<SelectableSettingValue>();
            }

            IList<MatchableBagOfTokens> matchable_candidate_bags = new List<MatchableBagOfTokens>();
            IDictionary<string, SelectableSettingValue> value_search_index = new Dictionary<string, SelectableSettingValue>();
            foreach (var selectable_value in values)
            {
                var alternative_names = this.settingList.GetAlternativeNamesForSettingValue(selectable_value.canonicalSettingName, selectable_value.value.CanonicalName);
                matchable_candidate_bags.Add(this.MakeMatchableBagOfTokens(
                this.PreProcessName(selectable_value.value.CanonicalName),
                      alternative_names,
                      "",
                      selectable_value.value.CanonicalName));
                value_search_index.Add(selectable_value.value.CanonicalName, selectable_value);
            }

            var matching_bags = this.DisambiguateAntonyms(matchable_entity_bag, matchable_candidate_bags,
                antonym_disamb_threshold, antonym_disamb_percentage_of_max, true);

            IList<SelectableSettingValue> selected_values = new List<SelectableSettingValue>();
            foreach (var bag in matching_bags)
            {
                if (value_search_index.TryGetValue(bag.canonical_value_name, out var value))
                {
                    selected_values.Add(value);
                }
                else
                {
                    // Unreachable.
                    throw new Exception("Failed to find value with canonical name: " + bag.canonical_value_name);
                }
            }
            return selected_values;
        }

        private double ComputeSimilarityScore(MatchableBagOfTokens lhs, MatchableBagOfTokens rhs)
        {
            // Given two lists of tokenized setting names
            // Return the maximum similarity score between setting names from these two lists
            double score_final = -1;
            foreach (var lhs_tokens in lhs.tokens_list)
            {
                double score_lhs = -1;
                foreach (var rhs_tokens in rhs.tokens_list)
                {
                    double score = 0;
                    foreach (var token in lhs_tokens)
                    {
                        if (rhs_tokens.Contains(token))
                        {
                            score += 1;
                        }
                    }
                    // Multiplying by 0.5 to normalize the scores into the [0,1] interval.
                    score = 0.5 * (score / lhs_tokens.Count() + score / rhs_tokens.Count());
                    if (score > score_lhs)
                    {
                        score_lhs = score;
                    }
                }
                if (score_lhs > score_final)
                {
                    score_final = score_lhs;
                }
            }
            return score_final;
        }
    }
    /// <summary>
    /// Precomputed information about a bag of tokens.
    /// We purposely disregard the order of the tokens because we want e.g.,
    /// "left rear temperature" to match "rear left temperature".
    /// </summary>
    public class MatchableBagOfTokens
    {
        public string canonical_setting_name;
        public string canonical_value_name;
        public IList<string> tokens = new List<string>();
        // The tokens_list contains a list of tokenized setting names
        // An element in the tokens_list is a set of tokens for a setting name
        public IList<IList<string>> tokens_list = new List<IList<string>>();

        public bool IsEmpty()
        {
            return !tokens.Any() && !tokens_list.Any();
        }
    }

    public class ScoredMatchableBagOfTokens
    {
        public MatchableBagOfTokens option;
        public double score = 0.0;
    }

    public class MatchResult
    {
        public MatchableBagOfTokens element;
        public double score = 0.0;
    }

    /// <summary>
    /// A matching setting-value pair.
    /// </summary>
    public class SettingMatch
    {
        public string setting_name;
        public string value;
    }

    /// <summary>
    /// A setting value that can be selected from a list.
    /// </summary>
    public class SelectableSettingValue
    {
        /// <summary>
        /// The canonical name of the setting this value belongs to.
        /// </summary>
        public string canonicalSettingName;

        /// <summary>
        /// The setting value.
        /// </summary>
        public AvailableSettingValue value;
    }
}
