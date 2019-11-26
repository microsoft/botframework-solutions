﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest;
using Microsoft.Bot.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.Solutions.Skills
{
    public class SkillManifestGenerator
    {
        private const string _skillRoute = "/api/skill/messages";
        private readonly HttpClient _httpClient;

        public SkillManifestGenerator(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<SkillManifest> GenerateManifestAsync(string manifestFile, string appId, Dictionary<string, BotSettingsBase.CognitiveModelConfiguration> cognitiveModels, string uriBase, bool inlineTriggerUtterances = false)
        {
            SkillManifest skillManifest = null;

            if (string.IsNullOrEmpty(manifestFile))
            {
                throw new ArgumentNullException(nameof(manifestFile));
            }

            if (string.IsNullOrEmpty(appId))
            {
                throw new ArgumentNullException(nameof(appId));
            }

            if (cognitiveModels == null)
            {
                throw new ArgumentNullException(nameof(cognitiveModels));
            }

            if (string.IsNullOrEmpty(uriBase))
            {
                throw new ArgumentNullException(nameof(uriBase));
            }

            // Each skill has a manifest template in the root directory and is used as foundation for the generated manifest
            using (StreamReader reader = new StreamReader(manifestFile))
            {
                skillManifest = JsonConvert.DeserializeObject<SkillManifest>(reader.ReadToEnd());

                if (string.IsNullOrEmpty(skillManifest.Id))
                {
                    throw new Exception("Skill manifest ID property was not present in the template manifest file.");
                }

                if (string.IsNullOrEmpty(skillManifest.Name))
                {
                    throw new Exception("Skill manifest Name property was not present in the template manifest file.");
                }

                skillManifest.MSAappId = appId;
                skillManifest.Endpoint = new Uri($"{uriBase}{_skillRoute}");

                if (skillManifest.IconUrl != null)
                {
                    skillManifest.IconUrl = new Uri($"{uriBase}/{skillManifest.IconUrl.ToString()}");
                }

                // The manifest can either return a pointer to the triggering utterances or include them inline in the manifest
                // If the developer has requested inline, we need to go through all utteranceSource references and retrieve the utterances and insert inline
                if (inlineTriggerUtterances)
                {
                    Dictionary<string, dynamic> localeLuisModels = new Dictionary<string, dynamic>();

                    // Retrieve all of the LUIS model definitions deployed and configured for the skill which could have multiple locales
                    // These are used to match the model name and intent so we can retrieve the utterances
                    foreach (var localeSet in cognitiveModels)
                    {
                        // Download/cache all the LUIS models configured for this locale (key is the locale name)
                        await PreFetchLuisModelContentsAsync(localeLuisModels, localeSet.Key, localeSet.Value.LanguageModels).ConfigureAwait(false);
                    }

                    foreach (var action in skillManifest.Actions)
                    {
                        // Is this Action triggerd by LUIS utterances rather than events?
                        if (action.Definition.Triggers.UtteranceSources != null)
                        {
                            // We will retrieve all utterances from the referenced source and aggregate into one new aggregated list of utterances per action
                            action.Definition.Triggers.Utterances = new List<Utterance>();
                            var utterancesToAdd = new List<string>();

                            // Iterate through each utterance source, one per locale.
                            foreach (UtteranceSource utteranceSource in action.Definition.Triggers.UtteranceSources)
                            {
                                // There may be multiple intents linked to this
                                foreach (var source in utteranceSource.Source)
                                {
                                    // Retrieve the intent mapped to this action trigger
                                    var intentIndex = source.IndexOf('#');
                                    if (intentIndex == -1)
                                    {
                                        throw new Exception($"Utterance source for action: {action.Id} didn't include an intent reference: {source}");
                                    }

                                    // We now have the name of the LUIS model and the Intent
                                    var modelName = source.Substring(0, intentIndex);
                                    string intentToMatch = source.Substring(intentIndex + 1);

                                    // Find the LUIS model from our cache by matching on the locale/modelname
                                    var model = localeLuisModels.SingleOrDefault(m => string.Equals(m.Key, $"{utteranceSource.Locale}_{modelName}", StringComparison.CurrentCultureIgnoreCase)).Value;
                                    if (model == null)
                                    {
                                        throw new Exception($"Utterance source (locale: {utteranceSource.Locale}) for action: '{action.Id}' references the '{modelName}' model which cannot be found in the currently deployed configuration.");
                                    }

                                    // Validate that the intent in the manifest exists in this LUIS model
                                    IEnumerable<JToken> intents = model.intents;

                                    if (!intents.Any(i => string.Equals(i["name"].ToString(), intentToMatch, StringComparison.CurrentCultureIgnoreCase)))
                                    {
                                        throw new Exception($"Utterance source for action: '{action.Id}' references the '{modelName}' model and '{intentToMatch}' intent which does not exist.");
                                    }

                                    // Retrieve the utterances that match this intent
                                    IEnumerable<JToken> utterancesList = model.utterances;
                                    var utterances = utterancesList.Where(s => string.Equals(s["intent"].ToString(), intentToMatch, StringComparison.CurrentCultureIgnoreCase));

                                    if (!utterances.Any())
                                    {
                                        throw new Exception($"Utterance source for action: '{action.Id}' references the '{modelName}' model and '{intentToMatch}' intent which has no utterances.");
                                    }

                                    foreach (JObject utterance in utterances)
                                    {
                                        utterancesToAdd.Add(utterance["text"].Value<string>());
                                    }
                                }

                                action.Definition.Triggers.Utterances.Add(new Utterance(utteranceSource.Locale, utterancesToAdd.ToArray()));
                            }
                        }
                    }
                }
            }

            return skillManifest;
        }

        /// <summary>
        /// Retrieve the LUIS model definition for each LUIS model registered in this skill so we have the utterance training data.
        /// </summary>
        /// <param name="luisServices">List of LuisServices.</param>
        /// <returns>Collection of LUIS model definitions grouped by model name.</returns>
        private async Task PreFetchLuisModelContentsAsync(Dictionary<string, dynamic> localeModelUtteranceCache, string locale, List<LuisService> luisServices)
        {
            // For each luisSource we identify the Intent and match with available luisServices to identify the LuisAppId which we update
            foreach (LuisService luisService in luisServices)
            {
                string exportModelUri = string.Format("https://{0}.api.cognitive.microsoft.com/luis/api/v2.0/apps/{1}/versions/{2}/export", luisService.Region, luisService.AppId, luisService.Version);
                _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", luisService.AuthoringKey);
                var httpResponse = await _httpClient.GetAsync(exportModelUri).ConfigureAwait(false);

                if (httpResponse.IsSuccessStatusCode)
                {
                    string json = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var luisApp = JsonConvert.DeserializeObject<dynamic>(json);

                    localeModelUtteranceCache.Add($"{locale}_{luisService.Id}", luisApp);
                }
            }
        }
    }
}