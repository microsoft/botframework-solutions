// <auto-generated>
// Code generated by LUISGen todo.luis -cs Luis.ToDo -o ../../Dialogs/Shared/Resources
// Tool github: https://github.com/microsoft/botbuilder-tools
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
namespace Luis
{
    public class ToDo: IRecognizerConvert
    {
        public string Text;
        public string AlteredText;
        public enum Intent {
            AddToDo, 
            Cancel, 
            ConfirmNo, 
            ConfirmYes, 
            DeleteToDo, 
            Greeting, 
            Help, 
            Logout, 
            MarkToDo, 
            Next, 
            None, 
            Previous, 
            ShowToDo
        };
        public Dictionary<Intent, IntentScore> Intents;

        public class _Entities
        {
            // Simple entities
            public string[] ContainsAll;
            public string[] ListType;

            // Built-in entities
            public double[] ordinal;

            // Pattern.any
            public string[] TaskContent;

            // Instance
            public class _Instance
            {
                public InstanceData[] ContainsAll;
                public InstanceData[] ListType;
                public InstanceData[] ordinal;
                public InstanceData[] TaskContent;
            }
            [JsonProperty("$instance")]
            public _Instance _instance;
        }
        public _Entities Entities;

        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, object> Properties {get; set; }

        public void Convert(dynamic result)
        {
            var app = JsonConvert.DeserializeObject<ToDo>(JsonConvert.SerializeObject(result));
            Text = app.Text;
            AlteredText = app.AlteredText;
            Intents = app.Intents;
            Entities = app.Entities;
            Properties = app.Properties;
        }

        public (Intent intent, double score) TopIntent()
        {
            Intent maxIntent = Intent.None;
            var max = 0.0;
            foreach (var entry in Intents)
            {
                if (entry.Value.Score > max)
                {
                    maxIntent = entry.Key;
                    max = entry.Value.Score.Value;
                }
            }
            return (maxIntent, max);
        }
    }
}
