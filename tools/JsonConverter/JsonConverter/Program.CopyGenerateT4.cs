// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;

namespace JsonConverter
{
    partial class Program
    {
        private static readonly string ttContent = @"<#@ template debug=""false"" hostspecific=""true"" language=""C#"" #>
<#@ output extension="".cs"" #>
<#@ include file=""{0}""#>
";

        // after all ConvertJsonFilesToLG
        public void CopyGenerateT4(params string[] folders)
        {
            var destFolder = GetFullPath(folders);
            Directory.CreateDirectory(destFolder);
            var target = Path.Join(destFolder, options.LgIdCollectionName);
            try
            {
                File.Copy("LgIdCollection.t4", target, !options.KeepOld);
            }
            catch (IOException ex)
            {
                Console.Write($"{target} already exists! {ex.Message}");
            }

            foreach (var file in filesForT4)
            {
                var ttFile = Path.ChangeExtension(file, options.KeepOld ? "ttnew" : "tt");
                var relative = Path.GetRelativePath(Path.GetDirectoryName(file), target);
                var content = string.Format(ttContent, relative);
                using(var sw = new StreamWriter(ttFile))
                {
                    sw.Write(content);
                }

                if (options.UpdateProject)
                {
                    if (!options.KeepOld)
                    {
                        DeleteFileInProject(Path.ChangeExtension(file, "tt"));
                    }
                    AddFileWithToolInProject(ttFile);
                }
            }

            haveDone.AppendLine($"* Copy LgIdCollection.t4 to {options.LgIdCollectionName}");

            if (options.KeepOld)
            {
                haveDone.AppendLine("* Create ttnew files for lg id");
            }
            else
            {
                haveDone.AppendLine("* Rewrite tt files for lg id");
            }

            if (!options.UpdateProject)
            {
                if (options.KeepOld)
                {
                    help.AppendLine("* Set Custom Tool to TextTemplatingFileGenerator for new ttnew files");
                }
            }
            else
            {
                if (options.KeepOld)
                {
                    haveDone.AppendLine("* Set Custom Tool to TextTemplatingFileGenerator for new ttnew files");
                }
            }
        }
    }
}
