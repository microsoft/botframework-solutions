using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JsonConverter
{
    partial class Program
    {
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

                            // eg: [import] (../AddToDo/AddToDoTexts.lg);
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
    }
}
