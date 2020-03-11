// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;

namespace JsonConverter
{
    partial class Program
    {
        private string sharedFile;

        // before ConvertJsonFilesToLG
        public void CopySharedLGFiles(params string[] folders)
        {
            var responseFolder = GetFullPath(folders);
            Directory.CreateDirectory(responseFolder);
            sharedFile = Path.Join(responseFolder, options.SharedName);
            try
            {
                File.Copy("Shared.lg", sharedFile, false);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"{sharedFile} already exists! {ex.Message}");
            }

            if (options.UpdateProject)
            {
                AddFileWithCopyInProject(sharedFile);
            }

            haveDone.AppendLine($"* Copy Shared.lg to {options.SharedName}");

            if (options.UpdateProject)
            {
                haveDone.AppendLine($"* Change 'Copy to Output Directory' to 'Copy if newer' for {options.SharedName}");
            }
            else
            {
                help.AppendLine($"* Change 'Copy to Output Directory' to 'Copy if newer' for {options.SharedName}");
            }
        }
    }
}
