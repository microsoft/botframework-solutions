// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace JsonConverter
{
    partial class Program
    {
        // Copy card json files to .new.json with {X} changed to @{Data.X}
        public void ModifyCardParameters(params string[] folders)
        {
            var cardFolder = GetFullPath(folders);
            contentFolder = cardFolder;
            var jsonFiles = Directory.GetFiles(cardFolder, "*.json", SearchOption.AllDirectories);
            foreach (var file in jsonFiles)
            {
                string content;
                using (StreamReader sr = new StreamReader(file))
                {
                    content = sr.ReadToEnd();
                }

                content = ModifyTextParameters(content);

                var newFile = options.KeepOld ? Path.ChangeExtension(file, "new.json") : file;
                using (StreamWriter sw = new StreamWriter(newFile))
                {
                    sw.WriteLine(content);
                }

                if (options.UpdateProject)
                {
                    DeleteFileInProject(file);
                    AddFileWithCopyInProject(newFile);
                }
                else
                {
                    help.AppendLine($"* Change 'Copy to Output Directory' to 'Copy if newer' for {(options.KeepOld ? "card.new.json" : "card.json")}");
                    if (!options.KeepOld)
                    {
                        help.AppendLine("* Delete card json files from project manually");
                    }
                }
            }
        }
    }
}
