using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AutomotiveSkill.Models;

namespace AutomotiveSkill.Common
{
    public class SettingMatcher
    {
        private static readonly Regex Whitespace = new Regex("\\s+", RegexOptions.Compiled);

        private readonly SettingList settingList;
        private readonly IList<MatchableBagOfTokens> matchableSettingNameBags = new List<MatchableBagOfTokens>();
        private readonly IList<MatchableBagOfTokens> matchableValueNameBags = new List<MatchableBagOfTokens>();
        private readonly IDictionary<string, AvailableSetting> preProcessedCanonicalNameMap = new Dictionary<string, AvailableSetting>();

        public SettingMatcher(SettingList settingList)
        {
            this.settingList = settingList;

            foreach (var settingName in this.settingList.GetAllSettingNames())
            {
                var setting_info = this.settingList.FindSetting(settingName);
                var pre_processed_canonical_name = PreProcessName(setting_info.CanonicalName);

                var alternative_names = this.settingList.GetAlternativeNamesForSetting(setting_info.CanonicalName);

                matchableSettingNameBags.Add(MakeMatchableBagOfTokens(
                    pre_processed_canonical_name, alternative_names,
                    setting_info.CanonicalName, string.Empty));

                this.preProcessedCanonicalNameMap.Add(pre_processed_canonical_name, setting_info);

                foreach (var value_info in setting_info.Values)
                {
                    var alternativeValueNames = this.settingList.GetAlternativeNamesForSettingValue(setting_info.CanonicalName, value_info.CanonicalName);
                    matchableValueNameBags.Add(MakeMatchableBagOfTokens(
                        PreProcessName(value_info.CanonicalName), alternativeValueNames,
                        setting_info.CanonicalName, value_info.CanonicalName));
                }
            }
        }

        /// <summary>
        /// See if the provided setting name can be exactly matched to a setting (e.g. temperature)        ///. </summary>
        /// <param name="entityValue">Entity value to match.</param>
        /// <returns>Matched available setting.</returns>
        public IList<AvailableSetting> MatchSettingNamesExactly(string entityValue)
        {
            if (this.preProcessedCanonicalNameMap.TryGetValue(entityValue, out var setting_info))
            {
                return new List<AvailableSetting> { setting_info };
            }
            else
            {
                return new List<AvailableSetting>();
            }
        }

        public IList<AvailableSetting> MatchSettingNames(
            List<string> entityValuesToMatch,
            double semantic_threshold,
            double antonym_disamb_percentage_of_max,
            bool use_coverage_filter)
        {
            var matchable_entity_bag = this.MakeMatchableBagOfTokens(entityValuesToMatch);
            if (matchable_entity_bag.IsEmpty())
            {
                return new List<AvailableSetting>();
            }

            var matching_bags = this.FindSemanticMatches(matchable_entity_bag, this.matchableSettingNameBags, semantic_threshold);

            // If there are multiple semantic matches, they might be antonyms of each other (e.g., "right" vs. "left") because
            // the embeddings of antonyms are often very similar.
            if (matching_bags.Count() > 1)
            {
                matching_bags = this.DisambiguateAntonyms(
                    matchable_entity_bag,
                    matching_bags,
                    0.0,
                    antonym_disamb_percentage_of_max,
                    use_coverage_filter);
            }

            IList<AvailableSetting> selected_settings = new List<AvailableSetting>();
            foreach (var bag in matching_bags)
            {
                var setting_info = this.settingList.FindSetting(bag.CanonicalSettingName);
                if (setting_info != null)
                {
                    selected_settings.Add(setting_info);
                }
                else
                {
                    // This should be impossible to reach because we made the bags based on the setting list.
                    throw new Exception("Failed to find setting with canonical name: " + bag.CanonicalSettingName);
                }
            }

            return selected_settings;
        }

        public IList<SettingMatch> MatchSettingValues(
            List<string> entity_types,
            double semantic_threshold,
            double antonym_disamb_percentage_of_max)
        {
            var matchable_entity_bag = this.MakeMatchableBagOfTokens(entity_types);
            if (matchable_entity_bag.IsEmpty())
            {
                return new List<SettingMatch>();
            }

            var matching_bags = this.FindSemanticMatches(matchable_entity_bag, this.matchableValueNameBags, semantic_threshold);

            // If there are multiple semantic matches, they might be antonyms of each other (e.g., "right" vs. "left") because
            // the embeddings of antonyms are often very similar.
            if (matching_bags.Count() > 1)
            {
                matching_bags = this.DisambiguateAntonyms(
                    matchable_entity_bag,
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
                    SettingName = bag.CanonicalSettingName,
                    Value = bag.CanonicalValueName
                };
                matches.Add(match);
            }

            return matches;
        }

        public IList<SelectableSettingValue> DisambiguateSettingValues(
            List<string> entity_types,
            IList<SelectableSettingValue> values,
            double antonym_disamb_threshold,
            double antonym_disamb_percentage_of_max)
        {
            if (!values.Any())
            {
                return new List<SelectableSettingValue>();
            }

            // Not using semantic matching because we expect the values to be antonyms of each other, e.g., "on" and "off"
            var matchable_entity_bag = this.MakeMatchableBagOfTokens(entity_types);
            if (matchable_entity_bag.IsEmpty())
            {
                return new List<SelectableSettingValue>();
            }

            IList<MatchableBagOfTokens> matchable_candidate_bags = new List<MatchableBagOfTokens>();
            IDictionary<string, SelectableSettingValue> value_search_index = new Dictionary<string, SelectableSettingValue>();
            foreach (var selectable_value in values)
            {
                // Get all of the alternative names for adjusting this given setting, e.g. higher, hike, increase for temperature
                var alternative_names = this.settingList.GetAlternativeNamesForSettingValue(selectable_value.CanonicalSettingName, selectable_value.Value.CanonicalName);

                // Add these candidates to the bag
                matchable_candidate_bags.Add(this.MakeMatchableBagOfTokens(
                this.PreProcessName(selectable_value.Value.CanonicalName),
                alternative_names,
                string.Empty,
                selectable_value.Value.CanonicalName));
                value_search_index.Add(selectable_value.Value.CanonicalName, selectable_value);
            }

            // Antonyms are the opposite of a given word - e.g. hot and cold. Dismabiguate the
            var matching_bags = this.DisambiguateAntonyms(matchable_entity_bag, matchable_candidate_bags,
                antonym_disamb_threshold, antonym_disamb_percentage_of_max, true);

            IList<SelectableSettingValue> selected_values = new List<SelectableSettingValue>();
            foreach (var bag in matching_bags)
            {
                if (value_search_index.TryGetValue(bag.CanonicalValueName, out var value))
                {
                    selected_values.Add(value);
                }
                else
                {
                    // Unreachable.
                    throw new Exception("Failed to find value with canonical name: " + bag.CanonicalValueName);
                }
            }

            return selected_values;
        }

        private IList<MatchResult> FindNearestMatchesWithin(MatchableBagOfTokens matchable_entity_bag, IList<MatchableBagOfTokens> matchable_names, double threshold)
        {
            IList<MatchResult> matches = new List<MatchResult>();
            foreach (var matchable_name in matchable_names)
            {
                double score = ComputeSimilarityScore(matchable_entity_bag, matchable_name);
                MatchResult match = new MatchResult
                {
                    Element = matchable_name,
                    Score = score
                };
                matches.Add(match);
            }

            matches = matches.OrderByDescending(match => match.Score).ToList();

            IList<MatchResult> selected_matches = new List<MatchResult>();
            if (matches.Any())
            {
                double max_score = matches[0].Score;
                if (max_score > 0)
                {
                    foreach (var match in matches)
                    {
                        if (match.Score >= threshold)
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
            text = Whitespace.Replace(text, " ");
            return text;
        }

        private IList<string> Tokenize(string text)
        {
            return Whitespace.Split(text).ToList();
        }

        private void AddNameToMatchable(MatchableBagOfTokens matchable, string pre_processed_name)
        {
            var tokens = Tokenize(pre_processed_name);
            IList<string> tokens_per_name = new List<string>();

            foreach (var token in tokens)
            {
                matchable.Tokens.Add(token);
                tokens_per_name.Add(token);
            }

            matchable.TokensList.Add(tokens_per_name);
        }

        private MatchableBagOfTokens MakeMatchableBagOfTokens(List<string> entity_types)
        {
            MatchableBagOfTokens matchable = new MatchableBagOfTokens();

            string extracted_setting_name = string.Join(" ", entity_types);

            AddNameToMatchable(matchable, extracted_setting_name);

            return matchable;
        }

        private MatchableBagOfTokens MakeMatchableBagOfTokens(string pre_processed_canonical_name, IList<string> alternative_names,
            string canonical_setting_name, string canonical_value_name)
        {
            MatchableBagOfTokens matchable = new MatchableBagOfTokens
            {
                CanonicalSettingName = canonical_setting_name,
                CanonicalValueName = canonical_value_name
            };

            AddNameToMatchable(matchable, pre_processed_canonical_name);
            foreach (var name in alternative_names)
            {
                AddNameToMatchable(matchable, PreProcessName(name));
            }

            return matchable;
        }

        private IList<MatchableBagOfTokens> FindSemanticMatches(
            MatchableBagOfTokens matchable_entity_bag, IList<MatchableBagOfTokens> matchable_names,
            double semantic_threshold)
        {
            var matches = FindNearestMatchesWithin(matchable_entity_bag, matchable_names, semantic_threshold);

            IList<MatchableBagOfTokens> matching_bags = new List<MatchableBagOfTokens>();
            foreach (var match in matches)
            {
                matching_bags.Add(match.Element);
            }

            return matching_bags;
        }

        // We disambiguate between antonyms using TF-IDF with the names of each candidate forming a "document."
        private IList<MatchableBagOfTokens> DisambiguateAntonyms(
            MatchableBagOfTokens matchable_entity_bag,
            IList<MatchableBagOfTokens> matchable_candidate_bags,
            double threshold,
            double percentage_of_max,
            bool use_coverage_filter)
        {
            // Precompute the document frequency.
            IDictionary<string, int> document_freq = new Dictionary<string, int>();
            foreach (var matchable_candidate_bag in matchable_candidate_bags)
            {
                foreach (var token in UniqueElements(matchable_candidate_bag.Tokens))
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
                foreach (var token in matchable_entity_bag.Tokens)
                {
                    // Using binary TF because the alternative names contain a lot of repeated tokens and are not weighted by
                    // how frequent each name is in natural language, so the numeric TF of a token is not representative of how
                    // much that token is associated with the setting/ value.
                    double tf_idf = 0.0;
                    if (matchable_candidate_bag.Tokens.Contains(token))
                    {
                        matching_tokens.Add(token);
                        if (document_freq.TryGetValue(token, out var freq))
                        {
                            tf_idf = 1.0 / freq;
                        }
                    }

                    tf_idf_sum += tf_idf;
                }

                var tf_idf_avg = tf_idf_sum / matchable_entity_bag.Tokens.Count();

                // Given two candidates with a similar score, we want to select the one that has the highest percentage of
                // matching tokens out of all its tokens. E.g., if one candidate is a substring of the other and the entities
                // match the shared substring, we want to select the shorter candidate because it has a better coverage.
                if (use_coverage_filter)
                {
                    ISet<string> unique_candidate_tokens = new HashSet<string>(UniqueElements(matchable_candidate_bag.Tokens));
                    var coverage_score = (double)matching_tokens.Count() / unique_candidate_tokens.Count();
                    if (!coverage_scores.TryGetValue(matchable_candidate_bag.CanonicalSettingName, out IDictionary<string, double> innerMap))
                    {
                        innerMap = new Dictionary<string, double>();
                    }

                    innerMap.Add(matchable_candidate_bag.CanonicalValueName, coverage_score);
                    coverage_scores[matchable_candidate_bag.CanonicalSettingName] = innerMap;
                }

                if (tf_idf_avg > threshold)
                {
                    ScoredMatchableBagOfTokens scoredBag = new ScoredMatchableBagOfTokens
                    {
                        Option = matchable_candidate_bag,
                        Score = tf_idf_avg
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
                        Option = tf_idf_scored_bag.Option,
                        Score = coverage_scores[tf_idf_scored_bag.Option.CanonicalSettingName][tf_idf_scored_bag.Option.CanonicalValueName]
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
                selected_candidate_bags.Add(scored_bag.Option);
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

            scored_options = scored_options.OrderByDescending(scoredOption => scoredOption.Score).ToList();

            IList<ScoredMatchableBagOfTokens> selected = new List<ScoredMatchableBagOfTokens>();
            var threshold = scored_options[0].Score * percentage_of_max;
            foreach (var scoredOption in scored_options)
            {
                // It's sorted by score, so we can stop once the score is no longer above the threshold.
                if (scoredOption.Score <= threshold)
                {
                    break;
                }

                selected.Add(scoredOption);
            }

            return selected;
        }

        private double ComputeSimilarityScore(MatchableBagOfTokens lhs, MatchableBagOfTokens rhs)
        {
            // Given two lists of tokenized setting names
            // Return the maximum similarity score between setting names from these two lists
            double score_final = -1;
            foreach (var lhs_tokens in lhs.TokensList)
            {
                double score_lhs = -1;
                foreach (var rhs_tokens in rhs.TokensList)
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
                    score = 0.5 * ((score / lhs_tokens.Count()) + (score / rhs_tokens.Count()));
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
}
