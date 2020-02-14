// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace JsonConverter
{
    partial class Program
    {
        private readonly string root;
        private readonly string defaultLocale;
        private readonly bool keepOld;
        private readonly Dictionary<string, List<string>> convertedTextsFiles = new Dictionary<string, List<string>>();
        private readonly HashSet<string> convertedActivityFiles = new HashSet<string>();

        // defaultLocale: one of values of LocaleDic
        public Program(string root, bool keepOld = true, string defaultLocale = "en-us")
        {
            this.root = root;
            this.keepOld = keepOld;
            this.defaultLocale = defaultLocale;
        }

        static void Main(string[] args)
        {
            Console.Write("Input skill proj root folder: ");
            var rootFolder = Console.ReadLine();

            var program = new Program(rootFolder);
            // change to your structure if your project is not as same as the template
            program.ConvertJsonFilesToLG("Responses");
            program.CopySharedLGFiles("Responses", "Shared");
            program.ModifyCardParameters("Content");
            program.GenerateWrapper("Utilities");
            program.CopyGenerateT4("Responses", "Shared");
            program.GenerateEntryFiles("Responses", "ResponsesAndTexts");

            Console.WriteLine("Done.");
        }
    }
}
