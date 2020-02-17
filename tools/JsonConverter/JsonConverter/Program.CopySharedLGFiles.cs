// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;

namespace JsonConverter
{
    partial class Program
    {
        // after all ConvertJsonFilesToLG
        public void CopySharedLGFiles(params string[] folders)
        {
            var responseFolder = GetFullPath(folders);
            Directory.CreateDirectory(responseFolder);
            var target = Path.Join(responseFolder, options.SharedName);
            try
            {
                File.Copy("Shared.lg", target, false);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"{target} already exists! {ex.Message}");
            }

            foreach (var pair in convertedTextsFiles)
            {
                pair.Value.Add(target);
            }
        }
    }
}
