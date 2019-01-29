// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Common
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using global::AutomotiveSkill.Models;

    /// <summary>
    /// Filters the available device settings based on the NLU result and the state.
    /// </summary>
    public class SettingFilter
    {
        private static readonly double SettingNameScoreThreshold = 0.6;
        private static readonly double SettingNameAntonymDisambPercentageOfMax = 0.9;
        private static readonly double SettingValueScoreThreshold = 0.6;
        private static readonly double SettingValueAntonymDisambThreshold = 0.1;
        private static readonly double SettingValueAntonymDisambPercentageOfMax = 0.9;
        private static readonly Regex ToAs2Pattern = new Regex("^2[0-9][0-9]$", RegexOptions.Compiled);
        private static readonly IList<ValidationStatus> ValueRelatedValidities = new List<ValidationStatus>()
        {
            ValidationStatus.InvalidMissingValue,
            ValidationStatus.InvalidSettingValueCombination,
            ValidationStatus.InvalidValue
        };

        private readonly SettingList settingList;
        private readonly SettingMatcher settingMatcher;
        private readonly NumberNormalizer numberNormalizer;
        private readonly EntityNormalizer amountNormalizer;
        private readonly EntityNormalizer typeNormalizer;
        private readonly EntityNormalizer unitNormalizer;

        public SettingFilter(SettingList settingList)
        {
            this.settingList = settingList;
            this.settingMatcher = new SettingMatcher(this.settingList);
            this.numberNormalizer = new NumberNormalizer();
            this.amountNormalizer = new EntityNormalizer("Dialogs/VehicleSettings/Resources/normalization/amount_percentage.tsv");
            this.typeNormalizer = new EntityNormalizer("Dialogs/VehicleSettings/Resources/normalization/amount_type.tsv");
            this.unitNormalizer = new EntityNormalizer("Dialogs/VehicleSettings/Resources/normalization/amount_unit.tsv");
        }

        /// <summary>
        /// Take the entities provided by LUIS (Setting and Value) to try and identify the vehicle setting we need to process.
        /// </summary>
        /// <param name="state">State object.</param>
        /// <param name="declarative">Indicates if special process for declarative utterances should be performed.</param>
        public void PostProcessSettingName(AutomotiveSkillState state, bool declarative = false)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            IList<SettingMatch> setting_matches = new List<SettingMatch>();
            var has_matching_value_for_any_setting = false;
            ISet<string> setting_names_to_remove = new HashSet<string>();

            // The Setting entity will contain any identified vehicle setting that was present in the utterance, e.g. front right airflow
            // The Value entity will contain any identified value relating to a vehicle setting that was present in the utterance, e.g. warm
            IList<AvailableSetting> selected_settings = new List<AvailableSetting>();

            if (state.Entities.ContainsKey("SETTING"))
            {
                // If we have a Setting then try to find a match between the setting name provided and the available settings
                selected_settings = this.settingMatcher.MatchSettingNamesExactly(state.Entities["SETTING"].First());

                // If we have not found an exact setting match but we have a value then combine Setting and Value together to identify a match
                if (!selected_settings.Any() && state.Entities.ContainsKey("VALUE"))
                {
                    /* First try SETTING + VALUE entities combined to catch cases like "warm my seat",
                       where the value can help disambiguate which setting the user meant.*/

                    List<string> entityValuesToMatch = new List<string>();
                    entityValuesToMatch.AddRange(state.Entities["SETTING"]);
                    entityValuesToMatch.AddRange(state.Entities["VALUE"]);

                    selected_settings = this.settingMatcher.MatchSettingNames(
                        entityValuesToMatch, SettingNameScoreThreshold,
                        SettingNameAntonymDisambPercentageOfMax, false);
                }

                // If we still haven't found a match then try to match with just the setting but not exactly this time
                if (!selected_settings.Any())
                {
                    List<string> entityValuesToMatch = new List<string>();
                    entityValuesToMatch.AddRange(state.Entities["SETTING"]);

                    selected_settings = this.settingMatcher.MatchSettingNames(
                        entityValuesToMatch, SettingNameScoreThreshold,
                        SettingNameAntonymDisambPercentageOfMax, false);
                }
            }

            // Do we have a selected setting name?
            if (selected_settings.Any())
            {
                List<string> entityValuesToMatch = new List<string>();

                List<string> entity_types_for_value_disamb = new List<string>();
                if (state.Entities.ContainsKey("VALUE"))
                {
                    entityValuesToMatch.AddRange(state.Entities["VALUE"]);
                }
                else if (state.Entities.ContainsKey("SETTING"))
                {
                    // Sometimes the setting name itself is also a value, e.g., "defog"
                    entityValuesToMatch.AddRange(state.Entities["SETTING"]);
                }

                foreach (var setting_info in selected_settings)
                {
                    IList<SelectableSettingValue> selected_values = new List<SelectableSettingValue>();

                    if (entityValuesToMatch.Any())
                    {
                        IList<SelectableSettingValue> selectable_values = new List<SelectableSettingValue>();
                        foreach (var value in setting_info.Values)
                        {
                            SelectableSettingValue selectable = new SelectableSettingValue
                            {
                                CanonicalSettingName = setting_info.CanonicalName,
                                Value = value
                            };
                            selectable_values.Add(selectable);
                        }

                        /* From the available setting values for the given setting name identify which one applies for this setting name
                        e.g. Set (when users says set temperature to 21 degrees
                        e.g. Increase (when user says increase temperature)
                        e.g. Decrease (when user says decrease temperature)
                        e.g. Off, Alert, Alert and Brake when user wants to control Park Assist
                        */

                        selected_values = this.settingMatcher.DisambiguateSettingValues(
                            entityValuesToMatch, selectable_values,
                            SettingValueAntonymDisambThreshold, SettingValueAntonymDisambPercentageOfMax);

                        // If we don't even have a VALUE entity, we can't match multiple values.
                        // If the SETTING entity is really also a value, then it must match only one value.
                        if (!state.Entities.ContainsKey("VALUE") && selected_values.Count() > 1)
                        {
                            selected_values.Clear();
                        }

                        // For all selected values we return the canonical name for both the name and value
                        foreach (var selected_value in selected_values)
                        {
                            SettingMatch match = new SettingMatch
                            {
                                SettingName = setting_info.CanonicalName,
                                Value = selected_value.Value.CanonicalName
                            };
                            setting_matches.Add(match);
                            has_matching_value_for_any_setting = true;
                        }
                    }

                    if (!selected_values.Any())
                    {
                        SettingMatch match = new SettingMatch
                        {
                            SettingName = setting_info.CanonicalName
                        };
                        setting_matches.Add(match);
                    }

                    AddAll(setting_names_to_remove, setting_info.IncludedSettings);
                }
            }
            else if (state.Entities.ContainsKey("VALUE") && !state.Entities.ContainsKey("SETTING"))
            {
                /*  If we have no SETTING entity, match the VALUE entities against all the values of all the settings.
                    This handles queries like "make it warmer" or "defog", where the value implies the setting.*/
                List<string> entityValuesToMatch = new List<string>();
                entityValuesToMatch.AddRange(state.Entities["VALUE"]);

                setting_matches = this.settingMatcher.MatchSettingValues(
                    entityValuesToMatch, SettingValueScoreThreshold,
                    SettingValueAntonymDisambPercentageOfMax);

                has_matching_value_for_any_setting = true;

                foreach (var match in setting_matches)
                {
                    var setting_info = this.settingList.FindSetting(match.SettingName);
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
                if ((!has_matching_value_for_any_setting || !string.IsNullOrEmpty(match.Value))
                    && !setting_names_to_remove.Contains(match.SettingName))
                {
                    new_setting_matches.Add(match);
                }
            }

            setting_matches = new_setting_matches;

            var (opt_amount, isRelative) = OptionalAmount(state, false);

            foreach (var setting_match in setting_matches)
            {
                SettingChange setting_change = new SettingChange
                {
                    SettingName = setting_match.SettingName
                };

                var value_info = this.settingList.FindSettingValue(setting_match.SettingName, setting_match.Value);
                if (declarative)
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
                    setting_change.Value = setting_match.Value;
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

        /// <summary>
        /// Further process the entities and remove those that are invalid based on the entity to ensure we prompt for values and don't accept incorrect values.
        /// </summary>
        /// <param name="state">State object.</param>
        public void ApplyContentLogic(AutomotiveSkillState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.Changes != null && state.Changes.Count > 0)
            {
                IList<SettingChange> validChanges = new List<SettingChange>();
                IList<SettingChange> invalidChanges = new List<SettingChange>();
                foreach (var change in state.Changes)
                {
                    var validity = ValidateChange(change);
                    if (validity == ValidationStatus.Valid)
                    {
                        validChanges.Add(change);
                    }
                    else if (ValueRelatedValidities.Contains(validity))
                    {
                        var settingInfo = settingList.FindSetting(change.SettingName);
                        if (settingInfo != null && !Util.IsNullOrEmpty(settingInfo.Values))
                        {
                            IList<SettingChange> validReplacements = new List<SettingChange>();
                            ValidationStatus replacementValidity = ValidationStatus.None;
                            foreach (var valueInfo in settingInfo.Values)
                            {
                                var newChange = (SettingChange)change.Clone();
                                newChange.Value = valueInfo.CanonicalName;
                                validity = ValidateChange(newChange);
                                if (validity == ValidationStatus.Valid)
                                {
                                    validReplacements.Add(newChange);
                                }
                                else if (replacementValidity == ValidationStatus.None)
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

        /// <summary>
        /// Apply the selecting setting value to the setting values.
        /// </summary>
        /// <param name="state">State object.</param>
        /// <param name="entityValues">List of entity values.</param>
        /// <returns>Setting.</returns>
        public IList<SettingChange> ApplySelectionToSettingValues(AutomotiveSkillState state, List<string> entityValues)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (entityValues == null)
            {
                throw new ArgumentNullException(nameof(entityValues));
            }

            var settingValues = state.GetUniqueSettingValues();

            ISet<string> selectedSettingValues = new HashSet<string>();
            if (entityValues.Any() && settingValues.Any())
            {
                IList<SelectableSettingValue> selectableSettingValues = new List<SelectableSettingValue>();
                foreach (var change in state.Changes)
                {
                    SelectableSettingValue selectable = new SelectableSettingValue
                    {
                        CanonicalSettingName = change.SettingName
                    };
                    var availableValue = this.settingList.FindSettingValue(change.SettingName, change.Value);
                    if (availableValue != null)
                    {
                        selectable.Value = availableValue;
                    }
                    else
                    {
                        availableValue = new AvailableSettingValue
                        {
                            CanonicalName = change.Value
                        };
                        selectable.Value = availableValue;
                    }

                    selectableSettingValues.Add(selectable);
                }

                var selected_values = this.settingMatcher.DisambiguateSettingValues(
                    entityValues,
                    selectableSettingValues, SettingValueAntonymDisambThreshold, SettingValueAntonymDisambPercentageOfMax);

                foreach (var selected_value in selected_values)
                {
                    selectedSettingValues.Add(selected_value.Value.CanonicalName);
                }
            }

            IList<SettingChange> newCandidates = new List<SettingChange>();
            foreach (var candidate in state.Changes)
            {
                if (selectedSettingValues.Contains(candidate.Value))
                {
                    newCandidates.Add(candidate);
                }
            }

            if (!Util.IsNullOrEmpty(newCandidates))
            {
                return newCandidates;
            }

            return null;
        }

        /// <summary>
        /// Validate a change.
        /// </summary>
        /// <param name="setting">A setting for validation.</param>
        /// <returns>Validation Status enumeration.</returns>
        private ValidationStatus ValidateChange(SettingChange setting)
        {
            if (setting == null)
            {
                throw new ArgumentNullException(nameof(setting));
            }

            ValidationStatus validity = ValidationStatus.Valid;

            if (string.IsNullOrEmpty(setting.SettingName))
            {
                return ValidationStatus.InvalidMissingSetting;
            }

            if (string.IsNullOrEmpty(setting.Value) && setting.Amount == null)
            {
                return ValidationStatus.InvalidMissingValue;
            }

            var settingInfo = settingList.FindSetting(setting.SettingName);
            if (settingInfo == null)
            {
                return ValidationStatus.InvalidSettingName;
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
                return ValidationStatus.InvalidSettingValueCombination;
            }

            if (setting.Amount == null)
            {
                if (settingValueInfo.RequiresAmount)
                {
                    validity = ValidationStatus.InvalidMissingAmount;
                }

                return validity;
            }

            if (!settingInfo.AllowsAmount || Util.IsNullOrEmpty(settingInfo.Amounts))
            {
                return ValidationStatus.InvalidExtraAmount;
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
                    return ValidationStatus.InvalidAmountUnit;
                }
            }

            if (!setting.IsRelativeAmount)
            {
                if (setting.Amount.Amount < settingAmountInfo.Min || setting.Amount.Amount > settingAmountInfo.Max)
                {
                    return ValidationStatus.InvalidAmountOutOfRange;
                }
            }
            else if (settingAmountInfo.Min != null && settingAmountInfo.Max != null)
            {
                var maxRelative = settingAmountInfo.Max - settingAmountInfo.Min;
                var minRelative = -maxRelative;
                if (setting.Amount.Amount < minRelative || setting.Amount.Amount > maxRelative)
                {
                    return ValidationStatus.InvalidAmountOutOfRange;
                }
            }

            return validity;
        }

        private IList<T> ApplySelectionToSettings<T>(AutomotiveSkillState state, List<string> settingEntities, IList<T> changesOrStatuses)
            where T : SettingOperation
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (settingEntities == null)
            {
                throw new ArgumentNullException(nameof(settingEntities));
            }

            if (changesOrStatuses == null)
            {
                throw new ArgumentNullException(nameof(changesOrStatuses));
            }

            var settingNames = state.GetUniqueSettingNames();

            ISet<string> selectedSettingNames = new HashSet<string>();
            if (settingEntities.Any() && settingNames.Any())
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
                var selected_settings = setting_matcher.MatchSettingNamesExactly(settingEntities.First());

                if (!selected_settings.Any())
                {
                    selected_settings = setting_matcher.MatchSettingNames(
                        settingEntities,
                        SettingNameScoreThreshold, SettingNameAntonymDisambPercentageOfMax, true);
                }

                foreach (var setting_info in selected_settings)
                {
                    selectedSettingNames.Add(setting_info.CanonicalName);
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

        /// <summary>
        /// If we have an amount then perform normalization.
        /// </summary>
        /// <param name="state">State.</param>
        /// <param name="change_sign_of_amount">Indicate whether to change sign on amount.</param>
        /// <returns>SettingAmount and relative indication.</returns>
        private (SettingAmount amount, bool isRelative) OptionalAmount(AutomotiveSkillState state, bool change_sign_of_amount)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            SettingAmount optional_amount = null;
            bool isRelative = false;

            if (state.Entities.TryGetValue("AMOUNT", out var amountEntityValues))
            {
                foreach (var amount_entity_value in amountEntityValues)
                {
                    var normalized_amount = this.amountNormalizer.NormalizeOrNull(amount_entity_value);
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
                            if (chunk.NumericValue != null)
                            {
                                optional_amount = new SettingAmount();
                                optional_amount.Amount = chunk.NumericValue.Value;

                                // Deal with ASR error that transcribes "to 24" as "224"
                                if (!state.Entities.ContainsKey("TYPE") && ToAs2Pattern.Match(amount_entity_value).Success)
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
                                var normalized_type = this.typeNormalizer.NormalizeOrNull(typeEntityValue);
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
                                var normalized_unit = this.unitNormalizer.NormalizeOrNull(unitEntityValue);
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
    }
}