using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JsonConverter
{
    partial class Program
    {
        public static (string, string) GetOutputLGFile(string file)
        {
            string locale = GetLocale(file);
            string dialogName = GetDialogName(file);
            string currentFolder = Path.GetDirectoryName(file);
            string outputActivitiesLGFile;
            string outputTextsLGFile;
            if (locale == "en-us")
            {
                outputTextsLGFile = Path.Combine(currentFolder, $"{dialogName}Texts.lg");
                outputActivitiesLGFile = file.Replace(".json", ".lg");
            }
            else
            {
                outputTextsLGFile = Path.Combine(currentFolder, $"{dialogName}Texts.{locale}.lg");
                outputActivitiesLGFile = file.Substring(0, file.Length - ".xx.json".Length) + ".lg";
            }
            return (outputActivitiesLGFile, outputTextsLGFile);
        }

        public static string ModifyTextParameters(string text)
        {
            return text.Replace("{", "@{Data.");
        }

        public static bool AreTextAndSpeakTheSame(List<Reply> replies)
        {
            foreach (var reply in replies)
            {
                if (reply.Text != reply.Speak)
                {
                    return false;
                }
            }
            return true;
        }

        public static void AddActivity(StringBuilder sb, string templateName, Activity activity)
        {
            sb.AppendLine($"# {templateName}(Data, Cards, Layout)");
            sb.AppendLine("[Activity");

            // If text and speak are the same, only need one *.Text() to reduce duplicate code.
            if (AreTextAndSpeakTheSame(activity.Replies))
            {
                sb.AppendLine($"    Text = @{{{templateName}.Text(Data)}}");
                sb.AppendLine($"    Speak = @{{{templateName}.Text(Data)}}");
            }
            else
            {
                sb.AppendLine($"    Text = @{{{templateName}.Text(Data)}}");
                sb.AppendLine($"    Speak = @{{{templateName}.Speak(Data)}}");
            }

            if (activity.SuggestedActions != null)
            {
                var suggestedActions = "    SuggestedActions = ";
                var suggestedActionsTexts = new List<string>();
                var index = 0;
                foreach (var suggestAction in activity.SuggestedActions)
                {
                    suggestedActionsTexts.Add($"@{{{templateName}.S{(++index).ToString()}(Data)}}");
                }
                suggestedActions += string.Join(" | ", suggestedActionsTexts);
                sb.AppendLine(suggestedActions);
            }

            sb.AppendLine(@"    Attachments = @{if(Cards == null, null, foreach(Cards, Card, CreateCard(Card)))}");

            sb.AppendLine(@"    AttachmentLayout = @{if(Layout == null, 'list', Layout)}");

            sb.AppendLine($"    InputHint = {activity.InputHint}");

            sb.AppendLine("]").AppendLine();
        }

        public static void AddTexts(StringBuilder sb, string templateName, Activity activity)
        {
            sb.AppendLine($"# {templateName}.Text(Data)");
            foreach (var reply in activity.Replies)
            {
                var text = ModifyTextParameters(reply.Text);
                sb.AppendLine($"- {text}");
            }
            sb.AppendLine();

            // If text and speak are not the same, need a *.Speak()
            if (!AreTextAndSpeakTheSame(activity.Replies))
            {
                sb.AppendLine($"# {templateName}.Speak(Data)");
                foreach (var reply in activity.Replies)
                {
                    var speak = ModifyTextParameters(reply.Speak);
                    sb.AppendLine($"- {speak}");
                }
                sb.AppendLine();
            }

            if (activity.SuggestedActions != null)
            {
                var index = 0;
                foreach (var suggestedAction in activity.SuggestedActions)
                {
                    sb.AppendLine($"# {templateName}.S{(++index).ToString()}(Data)");
                    sb.AppendLine($"- {suggestedAction}").AppendLine();
                }
            }
        }

        // One file generates a *Activities.lg and a *Texts.lg.
        // But only need to generate *Activities.lg once, because in a dialog it is common for different languages.
        public static void Convert(string file)
        {
            var (outputActivitiesLGFile, outputTextsLGFile) = GetOutputLGFile(file);
            var sbActivities = new StringBuilder();
            var sbTexts = new StringBuilder();
            sbTexts.AppendLine($"[import] ({Path.GetFileName(outputActivitiesLGFile)})").AppendLine();
            using (StreamReader sr = new StreamReader(file))
            {
                var content = sr.ReadToEnd();
                var jObject = JObject.Parse(content);
                foreach (var jToken in jObject)
                {
                    var templateName = jToken.Key;
                    var activity = jToken.Value.ToObject<Activity>();
                    AddActivity(sbActivities, templateName, activity);
                    AddTexts(sbTexts, templateName, activity);
                }
            }

            var locale = GetLocale(file);

            // Gereate DialogNameResponses.lg
            if (locale == "en-us")
            {
                using (StreamWriter sw = new StreamWriter(outputActivitiesLGFile))
                {
                    sw.WriteLine(sbActivities.ToString());
                }
            }

            using (StreamWriter sw = new StreamWriter(outputTextsLGFile))
            {
                if (!ConvertedTextsFiles.ContainsKey(locale))
                {
                    ConvertedTextsFiles.Add(locale, new List<string>());
                }
                ConvertedTextsFiles[locale].Add(outputTextsLGFile);

                sw.WriteLine(sbTexts.ToString());
            }
        }

        public static void ConvertJsonFilesToLG(string folder)
        {
            var responseFolder = Path.Combine(folder, "Responses");
            var jsonFiles = Directory.GetFiles(responseFolder, "*.json", SearchOption.AllDirectories);
            foreach (var file in jsonFiles)
            {
                Convert(file);
            }
        }
    }
}
