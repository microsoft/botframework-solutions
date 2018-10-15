// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace TestFramework.Fakes
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class FakeIntentRecognizerMiddleware : IMiddleware
    {
        private readonly string defaultIntent;
        private readonly Dictionary<string, string> intentMapping = new Dictionary<string, string>();
        private readonly Dictionary<string, Dictionary<string, JObject>> entityMapping = new Dictionary<string, Dictionary<string, JObject>>();

        public FakeIntentRecognizerMiddleware(string defaultIntent)
        {
            this.defaultIntent = defaultIntent;
        }

        public async Task OnTurnAsync(ITurnContext context, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (context.Activity.Type == ActivityTypes.Message)
            {
                var utterance = context.Activity.AsMessageActivity().Text;

                // var json = "{'" + this.GetIntent(utterance) + "': {'score': 1}}";
                var dict = new Dictionary<string, IntentScore>();
                var intentScore = new IntentScore { Score = 1 };
                dict.Add(this.GetIntent(utterance), intentScore);
                var result = new RecognizerResult
                {
                    Text = utterance,
                    Intents = dict,
                };

                if (!string.IsNullOrEmpty(utterance) && this.entityMapping.ContainsKey(utterance))
                {
                    result.Entities = this.BuildEntitiesJObject(this.entityMapping[utterance]);
                }

                //context.TurnState.Add(LuisRecognizerMiddleware.LuisRecognizerResultKey, result);
            }

            await next(cancellationToken).ConfigureAwait(false);
        }

        private string GetIntent(string utterance)
        {
            if (!string.IsNullOrEmpty(utterance) && this.intentMapping.ContainsKey(utterance))
            {
                return this.intentMapping[utterance];
            }

            return this.defaultIntent;
        }

        public JObject BuildEntitiesJObject(Dictionary<string, JObject> entities)
        {
            if (entities.Keys.Count == 1)
            {
                return entities.ElementAt(0).Value;
            }
            else
            {
                var jsonString = new StringBuilder("{");
                for (var i = 0; i < entities.Count - 1; i++)
                {
                    jsonString.Append(JsonConvert.SerializeObject(entities.ElementAt(i).Value)
                            .Replace("{", string.Empty)
                            .Replace("}", string.Empty))
                        .Append(",");
                }

                jsonString.Append(JsonConvert.SerializeObject(entities.ElementAt(entities.Count - 1).Value)
                        .Replace("{", string.Empty)
                        .Replace("}", string.Empty))
                    .Append("}");

                return JObject.Parse(jsonString.ToString());
            }
        }

        public void AddIntentResult(string utterance, string intent)
        {
            this.intentMapping.Add(utterance, intent);
        }

        public void AddEntityResult(string utterance, Dictionary<string, JObject> entities)
        {
            this.entityMapping.Add(utterance, entities);
        }
    }
}