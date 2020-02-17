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
        public class ProgramOptions
        {
            public string Root { get; set; }

            public string Namespace { get; set; }

            // one of values of LocaleDic
            public string DefaultLocale { get; set; } = "en-us";

            public bool KeepOld { get; set; } = true;

            public string EntryName { get; set; } = "ResponsesAndTexts";

            public string SharedName { get; set; } = "Shared.lg";

            public string LgIdCollectionName { get; set; } = "LgIdCollection.t4";

            public void ParseOptions()
            {
                if (string.IsNullOrEmpty(Namespace))
                {
                    Namespace = Path.GetFileName(Root);
                }
            }
        }

        private readonly ProgramOptions options;
        private string contentFolder;
        private string entryFolder;
        private readonly Dictionary<string, List<string>> convertedTextsFiles = new Dictionary<string, List<string>>();
        private readonly HashSet<string> convertedActivityFiles = new HashSet<string>();

        public Program(ProgramOptions options)
        {
            this.options = options;
            this.options.ParseOptions();
        }

        static void Main(string[] args)
        {
            Console.Write("Input skill proj root folder: ");
            var rootFolder = Console.ReadLine();

            var options = new ProgramOptions
            {
                Root = rootFolder,
                KeepOld = true,
            };

            if (!options.KeepOld)
            {
                options.LgIdCollectionName = "ResponseIdCollection.t4";
            }

            var program = new Program(options);
            // change to your structure if your project is not as same as the template
            program.ConvertJsonFilesToLG("Responses");
            program.ConvertResourceFilesToLG("Responses");
            program.CopySharedLGFiles("Responses", "Shared");
            program.CopyGenerateT4("Responses", "Shared");
            program.GenerateEntryFiles("Responses", "ResponsesAndTexts");
            program.ModifyCardParameters("Content");
            program.GenerateWrapper("Utilities");

            Console.WriteLine("Done.");
        }
    }
}
