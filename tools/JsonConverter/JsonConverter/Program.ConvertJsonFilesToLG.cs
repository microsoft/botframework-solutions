// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JsonConverter
{
    partial class Program
    {
        private (string, string) GetOutputLGFile(string file)
        {
            string locale = GetLocale(file);
            string dialogName = GetDialogName(file);
            string currentFolder = Path.GetDirectoryName(file);
            string outputActivitiesLGFile;
            string outputTextsLGFile;
            if (locale == options.DefaultLocale)
            {
                outputTextsLGFile = Path.Join(currentFolder, $"{dialogName}Texts.lg");
                outputActivitiesLGFile = Path.Join(currentFolder, $"{dialogName}.lg");
            }
            else
            {
                outputTextsLGFile = Path.Join(currentFolder, $"{dialogName}Texts.{locale}.lg");
                outputActivitiesLGFile = Path.Join(currentFolder, $"{dialogName}.{locale}.lg");
            }
            return (outputActivitiesLGFile, outputTextsLGFile);
        }

        private bool AreTextAndSpeakTheSame(List<Reply> replies)
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

        private void AddActivity(StringBuilder sb, string templateName, Activity activity, bool sameTextSpeak)
        {
            sb.AppendLine($"# {templateName}(Data, Cards, Layout)");
            sb.AppendLine("[Activity");

            string templateNameForDifferent = string.Empty;

            if (sameTextSpeak)
            {
                sb.AppendLine($"    Text = ${{{templateName}.Text(Data)}}");
                sb.AppendLine($"    Speak = ${{{templateName}.Text(Data)}}");
            }
            else
            {
                sb.AppendLine($"    ${{{templateName}.Text(Data)}}");
            }

            if (activity.SuggestedActions != null && activity.SuggestedActions.Count > 0)
            {
                var suggestedActions = "    SuggestedActions = ";
                var suggestedActionsTexts = new List<string>();
                var index = 0;
                foreach (var suggestAction in activity.SuggestedActions)
                {
                    suggestedActionsTexts.Add($"${{{templateName}.S{(++index).ToString()}(Data)}}");
                }
                suggestedActions += string.Join(" | ", suggestedActionsTexts);
                sb.AppendLine(suggestedActions);
            }

            sb.AppendLine("    Attachments = ${if(Cards == null, null, foreach(Cards, Card, CreateCard(Card)))}");

            sb.AppendLine("    AttachmentLayout = ${if(Layout == null, 'list', Layout)}");

            if (!string.IsNullOrEmpty(activity.InputHint))
            {
                sb.AppendLine($"    InputHint = {activity.InputHint}");
            }

            sb.AppendLine("]").AppendLine();
        }

        private void AddTexts(StringBuilder sb, string templateName, Activity activity, bool modifyParameter, bool sameTextSpeak)
        {
            if (sameTextSpeak)
            {
                sb.AppendLine($"# {templateName}.Text(Data)");
                foreach (var reply in activity.Replies)
                {
                    var text = modifyParameter ? ModifyTextParameters(reply.Text) : reply.Text;
                    sb.AppendLine($"- {text}");
                }
                sb.AppendLine();
            }
            else
            {
                // If not the same, follow this design:
                // https://github.com/microsoft/botbuilder-dotnet/issues/3354

                sb.AppendLine($"# {templateName}.Text(Data)");
                for (int i = 1; i <= activity.Replies.Count; i++)
                {
                    sb.AppendLine($"- ${{{templateName}TextAndSpeak{i.ToString()}(Data)}}");
                }
                sb.AppendLine();

                var index = 1;
                foreach (var reply in activity.Replies)
                {
                    sb.AppendLine($"# {templateName}TextAndSpeak{(index).ToString()}(Data)");
                    sb.AppendLine("[Activity");
                    var text = modifyParameter ? ModifyTextParameters(reply.Text) : reply.Text;
                    sb.AppendLine($"    Text = {text}");
                    var speak = modifyParameter ? ModifyTextParameters(reply.Speak) : reply.Speak;
                    sb.AppendLine($"    Speak = {speak}");
                    sb.AppendLine("]").AppendLine();
                    index++;
                }
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
        private void ConvertJson(string file)
        {
            ConvertToLg(file, (file, sbActivities, sbTexts) =>
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    var content = sr.ReadToEnd();
                    var jObject = JObject.Parse(content);
                    foreach (var jToken in jObject)
                    {
                        var templateName = jToken.Key;
                        var activity = jToken.Value.ToObject<Activity>();
                        activity.Correct();
                        bool same = AreTextAndSpeakTheSame(activity.Replies);
                        AddActivity(sbActivities, templateName, activity, same);
                        AddTexts(sbTexts, templateName, activity, true, same);
                    }
                }
            });
        }

        private void Convert(
            string locale,
            string outputActivitiesLGFile,
            StringBuilder sbActivities,
            string outputTextsLGFile,
            StringBuilder sbTexts)
        {
            if (locale == options.DefaultLocale)
            {
                filesForT4.Add(outputActivitiesLGFile);
            }

            if (!filesForEntry.ContainsKey(locale))
            {
                filesForEntry.Add(locale, new List<string>());
            }
            filesForEntry[locale].Add(outputActivitiesLGFile);

            using (StreamWriter sw = new StreamWriter(outputActivitiesLGFile))
            {
                sw.WriteLine(sbActivities.ToString());
            }

            if (options.UpdateProject)
            {
                AddFileWithCopyInProject(outputActivitiesLGFile);
            }

            using (StreamWriter sw = new StreamWriter(outputTextsLGFile))
            {
                sw.WriteLine(sbTexts.ToString());
            }

            if (options.UpdateProject)
            {
                AddFileWithCopyInProject(outputTextsLGFile);
            }
        }

        public void ConvertJsonFilesToLG(params string[] folders)
        {
            var responseFolder = GetFullPath(folders);
            var jsonFiles = Directory.GetFiles(responseFolder, "*.json", SearchOption.AllDirectories);
            foreach (var file in jsonFiles)
            {
                ConvertJson(file);
                if (!options.KeepOld)
                {
                    DeleteFile(file);
                }
            }

            if (jsonFiles.Length > 0)
            {
                haveDone.AppendLine("* Create lg files from json with {X} to ${if(Data.X == null, '', Data.X)}");

                if (!options.UpdateProject)
                {
                    help.AppendLine("* Change 'Copy to Output Directory' to 'Copy if newer' for lg files from json");
                    if (!options.KeepOld)
                    {
                        help.AppendLine("* Delete response json files from project manually");
                    }
                }
                else
                {
                    haveDone.AppendLine("* Change 'Copy to Output Directory' to 'Copy if newer' for lg files from json");
                }

                if (!options.KeepOld)
                {
                    haveDone.AppendLine("* Delete response json files");
                }
            }
        }
    }
}
