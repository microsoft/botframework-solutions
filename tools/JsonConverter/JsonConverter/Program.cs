using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace JsonConverter
{
    partial class Program
    {
        public static string GetLocale(string file)
        {
            var localeDic = new Dictionary<string, string>
            {
                { "en","en-us" },
                { "de","de-de" },
                { "fr","fr-fr" },
                { "it","it-it" },
                { "es","es-es" },
                { "zh","zh-cn" },
            };
            var fileName = Path.GetFileName(file);
            var exts = fileName.Split(".");

            if (localeDic.ContainsKey(exts[exts.Length - 2]))
            {
                return localeDic[exts[1]];
            }
            else
            {
                return localeDic["en"];
            }
        }

        public static string GetDialogName(string file)
        {
            var fileName = Path.GetFileName(file).Split(".")[0];
            return fileName.Substring(0, fileName.Length - "Responses".Length);
        }

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

        public static string AbstractParameterString(string content)
        {
            string pattern = @"\{(\w+)\}";
            var parameters = new List<string>();
            foreach (Match match in Regex.Matches(content, pattern))
            {
                if (!parameters.Contains(match.Value))
                {
                    parameters.Add(match.Value.Substring(1, match.Value.Length - 2));
                }
            }

            if (parameters.Count > 0)
            {
                return string.Join(", ", parameters);
            }
            else
            {
                return string.Empty;
            }
        }

        public static bool textAndSpeakAreTheSame(List<Reply> replies)
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
            if (textAndSpeakAreTheSame(activity.Replies))
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
            if (!textAndSpeakAreTheSame(activity.Replies))
            {
                var speakParameters = AbstractParameterString(activity.Replies[0].Speak);
                sb.AppendLine(string.Format("# {0}.Speak({1})", templateName, speakParameters));
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
        // But only need to generate *Activities.lg once, because it is common for different languages.
        public static void Convert(string file)
        {
            var (outputActivitiesLGFile, outputTextsLGFile) = GetOutputLGFile(file);
            var sbActivities = new StringBuilder();
            var sbTexts = new StringBuilder();
            sbTexts.AppendLine(string.Format("[import]({0}Activities.lg)", GetDialogName(file))).AppendLine();
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

        public static void ConvertFiles(string folder)
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

        public static string GetOutputResponsesAndTextsFile(string locale, string rootFolder)
        {
            var responseAndTextsFolder = Path.Combine(rootFolder, "Responses", "ResponsesAndTexts");
            string outputFile;
            if (locale == "en-us")
            {
                outputFile = Path.Combine(responseAndTextsFolder, "ResponsesAndTexts.lg");
            }
            else
            {
                outputFile = Path.Combine(responseAndTextsFolder, string.Format("ResponsesAndTexts.{0}.lg", locale));
            }
            return outputFile;
        }

        public static void GenerateEntryFile(string locale, string rootFolder)
        {
            var outputFile = GetOutputResponsesAndTextsFile(locale, rootFolder);
            using (StreamWriter sw = new StreamWriter(outputFile))
            {
                sw.WriteLine(@"﻿[import] (../Shared/Shared.lg)");
                var completedDialogName = new List<string>();
                var jsonFiles = Directory.GetFiles(rootFolder, "*.json", SearchOption.AllDirectories);
                foreach (var file in jsonFiles)
                {
                    if (!isCardFile(file))
                    {
                        var dialogName = GetDialogName(file);

                        // Each locale, each dialog, one line in ResponsesAndTexts.lg.
                        if (!completedDialogName.Contains(dialogName))
                        {
                            string lgFileFolder = Path.GetDirectoryName(file).Split("\\").Last();
                            string lgfile;
                            if (locale == "en-us")
                            {
                                lgfile = string.Format("{0}Texts.lg", dialogName);
                            }
                            else
                            {
                                lgfile = string.Format("{0}Texts.{1}.lg", dialogName, locale);
                            }

                            // e.g [import] (../AddToDo/AddToDoTexts.lg);
                            sw.WriteLine(string.Format("[import] (../{0}/{1})", lgFileFolder, lgfile));

                            completedDialogName.Add(dialogName);
                        }
                    }
                }
            }
        }

        public static void GenerateEntryFiles(string rootFolder)
        {
            var responseFolder = Path.Combine(rootFolder, "Responses", "ResponsesAndTexts");
            Directory.CreateDirectory(responseFolder);

            List<string> locales = new List<string>() { "en-us", "zh-cn", "es-es", "fr-fr", "it-it", "de-de" };
            foreach (var locale in locales)
            {
                GenerateEntryFile(locale, rootFolder);
            }
        }

        public static void CopySharedLGFiles(string rootFolder)
        {
            var responseFolder = Path.Combine(rootFolder, "Responses", "Shared");
            Directory.CreateDirectory(responseFolder);

            File.Copy("Shared.lg", Path.Combine(responseFolder, "Shared.lg"), false);
        }

        public static bool isCardFile(string file)
        {
            var folderName = Path.GetDirectoryName(file).Split("\\").Last();
            if (folderName == "Content")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // Consider /content is the card file folder.
        public static void ModifyCardParameters(string rootFolder)
        {
            var jsonFiles = Directory.GetFiles(rootFolder, "*.json", SearchOption.AllDirectories);
            foreach (var file in jsonFiles)
            {
                if (isCardFile(file))
                {
                    string content;
                    using (StreamReader sr = new StreamReader(file))
                    {
                        content = sr.ReadToEnd();
                    }

                    string pattern = @"\{(\w+)\}";
                    content = Regex.Replace(content, pattern, "@{Data.$1}");

                    using (StreamWriter sw = new StreamWriter(file.Replace(".json", ".new.json")))
                    {
                        sw.WriteLine(content);
                    }
                }
            }
        }


        static void Main(string[] args)
        {
            Console.Write("Input skill proj root folder: ");
            var rootFolder = Console.ReadLine();

            ConvertFiles(rootFolder);

            GenerateEntryFiles(rootFolder);

            CopySharedLGFiles(rootFolder);

            ModifyCardParameters(rootFolder);

            Console.WriteLine("Done.");
        }
    }
}
