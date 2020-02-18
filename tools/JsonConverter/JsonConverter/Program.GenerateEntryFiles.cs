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
            if (locale == options.DefaultLocale)
            {
                outputFile = Path.Join(responsesAndTextsFolder, $"{options.EntryName}.lg");
            }
            else
            {
                outputFile = Path.Join(responsesAndTextsFolder, $"{options.EntryName}.{locale}.lg");
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

            if (options.UpdateProject)
            {
                AddFileWithCopyInProject(outputEntryFile);
            }
        }

        // after CopySharedLGFiles
        public void GenerateEntryFiles(params string[] folders)
        {
            var responsesAndTextsFolder = GetFullPath(folders);
            Directory.CreateDirectory(responsesAndTextsFolder);
            entryFolder = responsesAndTextsFolder;

            foreach (var pair in convertedTextsFiles)
            {
                GenerateEntryFile(responsesAndTextsFolder, pair.Key, pair.Value);
            }

            if (!options.UpdateProject)
            {
                help.AppendLine($"* Change 'Copy to Output Directory' to 'Copy if newer' for {options.EntryName}.lg");
            }
        }
    }
}
