// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JsonConverter
{
    partial class Program
    {
        private string GetOutputResponsesAndTextsFile(string locale, string responsesAndTextsFolder)
        {
            string outputFile;
            if (locale == defaultLocale)
            {
                outputFile = Path.Combine(responsesAndTextsFolder, "ResponsesAndTexts.lg");
            }
            else
            {
                outputFile = Path.Combine(responsesAndTextsFolder, $"ResponsesAndTexts.{locale}.lg");
            }
            return outputFile;
        }

        private void GenerateEntryFile(string responsesAndTextsFolder, string locale, List<string> textsFiles)
        {
            var outputEntryFile = GetOutputResponsesAndTextsFile(locale, responsesAndTextsFolder);

            using (StreamWriter sw = new StreamWriter(outputEntryFile))
            {
                foreach (var file in textsFiles)
                {
                    // eg: [import] (../AddToDo/AddToDoTexts.lg);
                    var relativePath = Path.GetRelativePath(responsesAndTextsFolder, file);
                    sw.WriteLine($"[import] ({relativePath})");
                }
            }
        }

        // after everything
        public void GenerateEntryFiles(params string[] folders)
        {
            var responsesAndTextsFolder = GetFullPath(folders);
            Directory.CreateDirectory(responsesAndTextsFolder);

            foreach (var pair in convertedTextsFiles)
            {
                GenerateEntryFile(responsesAndTextsFolder, pair.Key, pair.Value);
            }
        }
    }
}
