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
                outputTextsLGFile = Path.Combine(currentFolder, string.Format("{0}Texts.lg", dialogName));
            }
            else
            {
                outputTextsLGFile = Path.Combine(currentFolder, string.Format("{0}Texts.{1}.lg", dialogName, locale));
            }
            outputActivitiesLGFile = Path.Combine(currentFolder, string.Format("{0}Activities.lg", dialogName));
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
            sb.AppendLine(string.Format("# {0}(Data, Cards, Layout)", templateName));
            sb.AppendLine("[Activity");

            // If text and speak are the same, only need one *.Text() to reduce duplicate code.
            if (AreTextAndSpeakTheSame(activity.Replies))
            {
                sb.AppendLine(string.Format("    Text = @{{{0}.Text(Data)}}", templateName));
                sb.AppendLine(string.Format("    Speak = @{{{0}.Text(Data)}}", templateName));
            }
            else
            {
                sb.AppendLine(string.Format("    Text = @{{{0}.Text(Data)}}", templateName));
                sb.AppendLine(string.Format("    Speak = @{{{0}.Speak(Data)}}", templateName));
            }

            if (activity.SuggestedActions != null)
            {
                var suggestedActions = "    SuggestedActions = ";
                var suggestedActionsTexts = new List<string>();
                var index = 0;
                foreach (var suggestAction in activity.SuggestedActions)
                {
                    suggestedActionsTexts.Add(string.Format("@{{{0}.S{1}(Data)}}", templateName, (++index).ToString()));
                }
                suggestedActions += string.Join(" | ", suggestedActionsTexts);
                sb.AppendLine(suggestedActions);
            }

            sb.AppendLine(@"    Attachments = @{if(Cards == null, null, foreach(Cards, Card, CreateCard(Card)))}");

            sb.AppendLine(@"    AttachmentLayout = @{if(Layout == null, 'list', Layout)}");

            sb.AppendLine(string.Format("    InputHint = {0}", activity.InputHint));

            sb.AppendLine("]").AppendLine();
        }

        public static void AddTexts(StringBuilder sb, string templateName, Activity activity)
        {
            sb.AppendLine(string.Format("# {0}.Text(Data)", templateName));
            foreach (var reply in activity.Replies)
            {
                var text = ModifyTextParameters(reply.Text);
                sb.AppendLine(string.Format("- {0}", text));
            }
            sb.AppendLine();

            // If text and speak are not the same, need a *.Speak()
            if (!AreTextAndSpeakTheSame(activity.Replies))
            {
                sb.AppendLine(string.Format("# {0}.Speak(Data)", templateName));
                foreach (var reply in activity.Replies)
                {
                    var speak = ModifyTextParameters(reply.Speak);
                    sb.AppendLine(string.Format("- {0}", speak));
                }
                sb.AppendLine();
            }

            if (activity.SuggestedActions != null)
            {
                var index = 0;
                foreach (var suggestedAction in activity.SuggestedActions)
                {
                    sb.AppendLine(string.Format("# {0}.S{1}(Data)", templateName, (++index).ToString()));
                    sb.AppendLine(string.Format("- {0}", suggestedAction)).AppendLine();
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
            sbTexts.AppendLine(string.Format("[import] ({0}Activities.lg)", GetDialogName(file))).AppendLine();
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

            if (GetLocale(file) == "en-us")
            {
                using (StreamWriter sw = new StreamWriter(outputActivitiesLGFile))
                {
                    sw.WriteLine(sbActivities.ToString());
                }
            }

            using (StreamWriter sw = new StreamWriter(outputTextsLGFile))
            {
                sw.WriteLine(sbTexts.ToString());
            }
        }

        public static void ConvertJsonFilesToLG(string folder)
        {
            var jsonFiles = Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories);
            foreach (var file in jsonFiles)
            {
                if (!isCardFile(file))
                {
                    Convert(file);
                }
            }
        }
    }
}
