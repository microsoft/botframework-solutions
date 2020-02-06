using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Builder.Community.Adapters.Google.Model
{
    public class DialogFlowOptionSystemIntent : ISystemIntent
    {
        public DialogFlowOptionSystemIntent()
        {
            Intent = "actions.intent.OPTION";
        }

        public OptionIntentData Data { get; set; }
    }

    public class OptionIntent : ISystemIntent
    {
        public OptionIntent()
        {
            Intent = "actions.intent.OPTION";
        }

        public OptionIntentData InputValueData { get; set; }
    }

    public class SigninIntent : ISystemIntent
    {
        public SigninIntent()
        {
            Intent = "actions.intent.SIGNIN";
            InputValueData = new IntentData { Type = "type.googleapis.com/google.actions.v2.SignInValueSpec" };
        }

        public IntentData InputValueData { get; }
    }

    public class TextIntent : ISystemIntent
    {
        public TextIntent()
        {
            Intent = "actions.intent.TEXT";
        }
    }

    public class IntentData
    {
        [JsonProperty(PropertyName = "@type")]
        public string Type { get; set; }
    }

    public abstract class OptionIntentData : IntentData
    {
    }

    public class ListOptionIntentData : OptionIntentData
    {
        public ListOptionIntentData()
        {
            Type = "type.googleapis.com/google.actions.v2.OptionValueSpec";
        }

        public OptionIntentSelect ListSelect { get; set; }
    }

    public class CarouselOptionIntentData : OptionIntentData
    {
        [JsonProperty(PropertyName = "@type")]
        public string Type { get; set; }

        public CarouselOptionIntentData()
        {
            Type = "type.googleapis.com/google.actions.v2.OptionValueSpec";
        }

        public OptionIntentSelect CarouselSelect { get; set; }
    }

    public class OptionIntentSelect
    {
        public string Title { get; set; }

        public List<OptionItem> Items { get; set; }
    }

    public class OptionItem
    {
        public OptionItemInfo OptionInfo { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public OptionItemImage Image { get; set; }
    }

    public class OptionItemImage
    {
        public string Url { get; set; }
        public string AccessibilityText { get; set; }
    }

    public class OptionItemInfo
    {
        public string Key { get; set; }
        public List<string> Synonyms { get; set; }
    }
}
