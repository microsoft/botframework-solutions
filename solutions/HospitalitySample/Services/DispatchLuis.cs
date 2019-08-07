// <auto-generated>
// Code generated by LUISGen C:\Users\t-luitof\source\repos\botframework-solutions\solutions\HospitalitySample\Deployment\Resources\Dispatch\en\LunaHospitalityVAen_Dispatch.json -cs Luis.DispatchLuis -o C:\Users\t-luitof\source\repos\botframework-solutions\solutions\HospitalitySample\Services
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
    public partial class DispatchLuis: IRecognizerConvert
    {
        public string Text;
        public string AlteredText;
        public enum Intent {
            l_General, 
            q_Chitchat, 
            q_Faq, 
            q_hotel_FAQ, 
            hospitalitySkill, 
            newsSkill, 
            restaurantBookingSkill, 
            None
        };
        public Dictionary<Intent, IntentScore> Intents;

        public class _Entities
        {
            // Simple entities
            public string[] Item;
            public string[] topic;
            public string[] site;

            // Instance
            public class _Instance
            {
                public InstanceData[] Item;
                public InstanceData[] topic;
                public InstanceData[] site;
            }
            [JsonProperty("$instance")]
            public _Instance _instance;
        }
        public _Entities Entities;

        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, object> Properties {get; set; }

        public void Convert(dynamic result)
        {
            var app = JsonConvert.DeserializeObject<DispatchLuis>(JsonConvert.SerializeObject(result, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
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
