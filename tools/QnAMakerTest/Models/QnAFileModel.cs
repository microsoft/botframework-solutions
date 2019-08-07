using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace QnAMakerTest.Models
{
    class QnAFileModel
    {
        [JsonProperty("files")]
        public List<FileDTO> Files { get; set; }

        [JsonProperty("name")]
        public string name { get; set; }

        [JsonProperty("urls")]
        public List<string> Urls { get; set; }

        [JsonProperty("qnaList")]
        public List<QnADTO> QnAList { get; set; }

        public class FileDTO
        {
            [JsonProperty("fileName")]
            public string FileName { get; set; }

            [JsonProperty("fileUri")]
            public string FileUri { get; set; }
        }

        public class QnADTO
        {
            [JsonProperty("answer")]
            public string Answer { get; set; }

            [JsonProperty("context")]
            public Context Context { get; set; }

            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("metadata")]
            public List<MetadataDTO> Metadata { get; set; }

            [JsonProperty("questions")]
            public List<string> Questions { get; set; }

            [JsonProperty("source")]
            public string Source { get; set; }
        }

        public class Context
        {
            [JsonProperty("isContextOnly ")]
            public Boolean isContextOnly { get; set; }

            [JsonProperty("prompts  ")]
            public List<PromptDTO> Prompts { get; set; }
        }

        public class PromptDTO
        {
            [JsonProperty("displayOrder")]
            public Int32 DisplayOrder { get; set; }

            [JsonProperty("displayText")]
            public string DisplayText { get; set; }

            [JsonProperty("qna")]
            public QnADTO QnA { get; set; }

            [JsonProperty("qnaId")]
            public Int32 QnAId { get; set; }
        }

        public class MetadataDTO
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }
        }
    }
}
