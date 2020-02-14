using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JsonConverter
{
    partial class Program
    {
        public static string GetOutputResponsesAndTextsFile(string locale, string responsesAndTextsFolder)
        {
            string outputFile;
            if (locale == "en-us")
            {
                outputFile = Path.Combine(responsesAndTextsFolder, "ResponsesAndTexts.lg");
            }
            else
            {
                outputFile = Path.Combine(responsesAndTextsFolder, $"ResponsesAndTexts.{locale}.lg");
            }
            return outputFile;
        }

        public static void GenerateEntryFile(string responsesAndTextsFolder, string locale, List<string> textsFiles)
        {
            var outputEntryFile = GetOutputResponsesAndTextsFile(locale, responsesAndTextsFolder);
            using (StreamWriter sw = new StreamWriter(outputEntryFile))
            {
                sw.WriteLine(@"﻿[import] (..\Shared\Shared.lg)");
                foreach (var file in textsFiles)
                {
                    // eg: [import] (../AddToDo/AddToDoTexts.lg);
                    var relativePath = Path.Combine("..", file.Split("\\Responses\\").Last());
                    sw.WriteLine($"[import] ({relativePath})");
                }
            }
        }

        public static void GenerateEntryFiles(string rootFolder)
        {
            var responsesAndTextsFolder = Path.Combine(rootFolder, "Responses", "ResponsesAndTexts");
            Directory.CreateDirectory(responsesAndTextsFolder);

            foreach (var locale in ConvertedTextsFiles.Keys)
            {
                GenerateEntryFile(responsesAndTextsFolder, locale, ConvertedTextsFiles[locale]);
            }
        }
    }
}
