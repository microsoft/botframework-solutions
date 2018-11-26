using Microsoft.Bot.Builder;
using System.Collections.Generic;

namespace AutomotiveSkill
{
    /// <summary>
    /// Wrapper around RecognizerResult to provide easier access to the information contained in it.
    /// </summary>
    public class RecognizerResultWrapper
    {
        private readonly RecognizerResult result;
        private readonly IDictionary<string, IList<string>> entities = new Dictionary<string, IList<string>>();

        public RecognizerResultWrapper(RecognizerResult result) {
            if (result != null)
            {
                this.result = result;
                AddEntitiesToMap(this.entities, this.result);
            }
            else
            {
                this.result = new RecognizerResult();
            }
        }

        /// <summary>
        /// Add the entities from the given RecognizerResult to the given map.
        /// </summary>
        /// <param name="map">The map to add the entities to.</param>
        /// <param name="result">The RecognizerResult to read the entities from.</param>
        public static void AddEntitiesToMap(IDictionary<string, IList<string>> map, RecognizerResult result)
        {
            foreach (var entity in result.Entities)
            {
                if (!entity.Key.StartsWith("$"))
                {
                    if (!map.TryGetValue(entity.Key, out IList<string> entityValues))
                    {
                        entityValues = new List<string>();
                        map.Add(entity.Key, entityValues);
                    }
                    foreach (string value in entity.Value)
                    {
                        entityValues.Add(value);
                    }
                }
            }
        }

        public RecognizerResult Get()
        {
            return result;
        }

        public string GetIntent()
        {
            var (intent, score) = result.GetTopScoringIntent();
            return intent;
        }

        public IList<string> GetEntityValues(string entityType)
        {
            if (entities.TryGetValue(entityType, out IList<string> values))
            {
                return values;
            }
            return new List<string>();
        }

        public bool HasEntity(string entityType)
        {
            return entities.ContainsKey(entityType);
        }
    }
}
