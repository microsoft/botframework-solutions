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
            string pattern = @"\{(\w+)\}";
            var jsonFiles = Directory.GetFiles(cardFolder, "*.json", SearchOption.AllDirectories);
            foreach (var file in jsonFiles)
            {
                string content;
                using (StreamReader sr = new StreamReader(file))
                {
                    content = sr.ReadToEnd();
                }

                content = Regex.Replace(content, pattern, "@{Data.$1}");

                var newFile = Path.ChangeExtension(file, "new.json");
                using (StreamWriter sw = new StreamWriter(newFile))
                {
                    sw.WriteLine(content);
                }
            }
        }
    }
}
