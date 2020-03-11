// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JsonConverter.Utility;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Newtonsoft.Json.Linq;

namespace JsonConverter
{
    partial class Program
    {
        public class ProgramOptions
        {
            public string Root { get; set; }

            // Namepsace is folder's name by default
            public string Namespace { get; set; }

            // one of values of LocaleDic
            public string DefaultLocale { get; set; } = "en-us";

            public bool KeepOld { get; set; } = true;

            public string EntryName { get; set; } = "ResponsesAndTexts";

            public string SharedName { get; set; } = "Shared.lg";

            public string LgIdCollectionName { get; set; } = "LgIdCollection.t4";

            public bool UpdateProject { get; set; } = false;

            // ProjectName is folder's name + .csproj by default
            public string ProjectName { get; set; }

            public string WrapperName { get; set; } = "LocaleTemplateManagerWrapper";
        }

        private readonly ProgramOptions options;
        private string contentFolder;
        private string entryFolder;
        private readonly Dictionary<string, List<string>> filesForEntry = new Dictionary<string, List<string>>();
        private readonly HashSet<string> filesForT4 = new HashSet<string>();
        private IProjectOperator project;
        private StringBuilder help = new StringBuilder(), haveDone = new StringBuilder();

        public Program(ProgramOptions options)
        {
            this.options = options;
            ProcessOptions();
        }

        private void ProcessOptions()
        {
            if (string.IsNullOrEmpty(options.Namespace))
            {
                options.Namespace = Path.GetFileName(options.Root);
            }

            if (options.UpdateProject)
            {
                if (string.IsNullOrEmpty(options.ProjectName))
                {
                    options.ProjectName = Path.GetFileName(options.Root);
                }

                project = new ProjectOperatorXml(Path.Join(options.Root, options.ProjectName + ".csproj"), help);
            }
        }

        public void Finish()
        {
            if (options.UpdateProject)
            {
                project.Save();
            }

            if (haveDone.Length != 0)
            {
                Console.WriteLine("This tool has done the following for you:");
                Console.Write(haveDone.ToString());
            }

            if (help.Length != 0)
            {
                Console.WriteLine("You should do the following after this tool:");
                help.AppendLine("* Add .lg to .filenesting.json");
                Console.Write(help.ToString());
            }
        }

        static void Main(string[] args)
        {
            Console.Write("Input skill proj root folder: ");
            var rootFolder = Console.ReadLine();

            var options = new ProgramOptions
            {
                Root = rootFolder,
                Namespace = string.Empty,
                KeepOld = true,
                UpdateProject = false,
            };

            if (!options.KeepOld)
            {
                options.LgIdCollectionName = "ResponseIdCollection.t4";
            }

            var program = new Program(options);
            // change to your structure if your project is not as same as the template
            program.CopySharedLGFiles("Responses", "Shared");
            program.ConvertJsonFilesToLG("Responses");
            program.ConvertResourceFilesToLG("Responses");
            program.CopyGenerateT4("Responses", "Shared");
            program.GenerateEntryFiles("Responses", "ResponsesAndTexts");
            program.ModifyCardParameters("Content");
            program.GenerateWrapper("Utilities");
            program.Finish();

            Console.WriteLine("Done.");
        }
    }
}
