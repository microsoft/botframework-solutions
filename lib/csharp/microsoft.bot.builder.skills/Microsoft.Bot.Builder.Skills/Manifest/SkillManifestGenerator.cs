using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Configuration;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills
{
    public class SkillManifestGenerator
    {
        private const string _skillRoute = "/api/skill/messages";
        private const string _manifestTemplateFilename = "manifestTemplate.json";

        public async Task<SkillManifest> GenerateManifest(string appId, List<LuisService> luisServices, string uriBase)
        {
            SkillManifest skillManifest = null;

            // Each skill has a manifest template in the root directory and is used as foundation for the generated manifest
            using (StreamReader reader = new StreamReader(_manifestTemplateFilename))
            {
                skillManifest = JsonConvert.DeserializeObject<SkillManifest>(reader.ReadToEnd());

                // Perform validation;

                skillManifest.MSAappId = appId;
                skillManifest.Endpoint = new Uri($"{uriBase}{_skillRoute}");
                skillManifest.IconUrl = new Uri($"{uriBase}/{skillManifest.IconUrl.ToString()}");

                // Retrieve the list of intents for each Luis Model registered for this skill
                // This is then used to match intents referenced in the manifest to deployed Luis Models
                var intentListCache = await PreFetchLuisModelIntents(luisServices);

                foreach (var action in skillManifest.Actions)
                {
                    // Is this Action triggerd by LUIS utterances rather than events?
                    if (action.Definition.Triggers.Utterances != null)
                    {
                        foreach (UtteranceSource utteranceSource in action.Definition.Triggers.UtteranceSources)
                        {
                            // There could be multiple LUIS models/intents as part of this action definition
                            // Numeric iterator as we need to update the underlying source as we go
                            for (int i = 0; i < utteranceSource.Source.Count(); ++i)
                            {
                                var luisSource = utteranceSource.Source[i];

                                // Retrieve the intent mapped to this action trigger
                                var intentIndex = luisSource.IndexOf('#');
                                if (intentIndex == -1)
                                {
                                    throw new Exception($"Utterance source for action: {action.Id} didn't include an intent reference: {luisSource}");
                                }

                                string intentToMatch = luisSource.Substring(intentIndex + 1);

                                // Find a match for the manifest intent against the deployed LUIS models
                                string selectedLuisService = null;
                                foreach (var intentList in intentListCache)
                                {
                                    var intents = intentList.Value;
                                    var found = intents.Select(intent => intent.name == intentToMatch);
                                    if (found != null)
                                    {
                                        selectedLuisService = intentList.Key;
                                        break;
                                    }
                                }

                                // Update the {AppId} placeholder with the actual LUIS AppId
                                if (selectedLuisService != null)
                                {
                                    utteranceSource.Source[i] = luisSource.Replace("{AppId}", selectedLuisService);
                                }
                                else
                                {
                                    throw new Exception($"Could not identify the LUIS AppID containing the {intentToMatch} intent.");
                                }
                            }
                        }
                    }
                }
            }

            return skillManifest;
        }

        private async Task<Dictionary<string, Intent[]>> PreFetchLuisModelIntents(List<LuisService> luisServices)
        {
            HttpClient httpClient = new HttpClient();

            Dictionary<string, Intent[]> intentListCache = new Dictionary<string, Intent[]>();

            // For each luisSource we identify the Intent and match with available luisServices to identify the LuisAppId which we update
            foreach (LuisService luisService in luisServices)
            {
                string getIntentsUri = string.Format("https://westus.api.cognitive.microsoft.com/luis/api/v2.0/apps/{0}/versions/{1}/intents?skip=0&take=500", luisService.AppId, luisService.Version);
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", luisService.AuthoringKey);
                var httpResponse = await httpClient.GetAsync(getIntentsUri);

                if (httpResponse.IsSuccessStatusCode)
                {
                    string json = await httpResponse.Content.ReadAsStringAsync();
                    intentListCache.Add(luisService.AppId, await httpResponse.Content.ReadAsAsync<Intent[]>());
                }
            }

            return intentListCache;
        }
    }
}
