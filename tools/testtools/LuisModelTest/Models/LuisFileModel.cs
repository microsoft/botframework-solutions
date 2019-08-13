using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LuisModelTest.Models
{
    public class LuisFileModel
    {
        [JsonProperty("luis_schema_version")]
        public string LuisSchemaVersion { get; set; }

        [JsonProperty("versionId")]
        public string VersionId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("desc")]
        public string Desc { get; set; }

        [JsonProperty("culture")]
        public string Culture { get; set; }

        [JsonProperty("intents")]
        public List<Intent> Intents { get; set; }

        [JsonProperty("entities")]
        public List<Entity> Entities { get; set; }

        [JsonProperty("composites")]
        public List<string> Composites { get; set; }

        [JsonProperty("closedLists")]
        public List<ClosedList> ClosedLists { get; set; }

        [JsonProperty("patternAnyEntities")]
        public Patternanyentity[] PatternAnyEntities { get; set; }

        [JsonProperty("regex_entities")]
        public List<string> RegexEntities { get; set; }

        [JsonProperty("prebuiltEntities")]
        public List<Entity> PrebuiltEntities { get; set; }

        [JsonProperty("model_features")]
        public List<string> ModelFeatures { get; set; }

        [JsonProperty("regex_features")]
        public List<string> RegexFeatures { get; set; }

        [JsonProperty("patterns")]
        public Pattern[] Patterns { get; set; }

        [JsonProperty("utterances")]
        public List<Utterance> Utterances { get; set; }

        [JsonProperty("settings")]
        public List<string> Settings { get; set; }

        public class Intent
        {
            [JsonProperty("name")]
            public string Name { get; set; }
        }

        public class Entity
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("roles")]
            public List<string> Roles { get; set; }
        }

        public class ClosedList
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("subLists")]
            public List<SubList> SubLists { get; set; }

            [JsonProperty("roles")]
            public List<string> Roles { get; set; }

            public class SubList
            {
                [JsonProperty("canonicalForm")]
                public string CanonicalForm { get; set; }

                [JsonProperty("list")]
                public List<string> List { get; set; }
            }
        }

        public class Utterance
        {
            [JsonProperty("text")]
            public string Text { get; set; }

            [JsonProperty("intent")]
            public string Intent { get; set; }

            [JsonProperty("entities")]
            public List<LabelEntity> Entities { get; set; }

            public class LabelEntity
            {
                [JsonProperty("entity")]
                public string Entity { get; set; }

                [JsonProperty("startPos")]
                public int StartPos { get; set; }

                [JsonProperty("endPos")]
                public int EndPos { get; set; }
            }
        }

        public class Patternanyentity
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("roles")]
            public object[] Roles { get; set; }

            [JsonProperty("explicitList")]
            public object[] ExplicitList { get; set; }
        }

        public class Pattern
        {
            [JsonProperty("pattern")]
            public string PatternString { get; set; }

            [JsonProperty("intent")]
            public string Intent { get; set; }
        }
    }
}
