// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonConverter.Utility
{
    public class ProjectOperatorBuild : IProjectOperator
    {
        private readonly Project project;
        public ProjectOperatorBuild(string file)
        {
            var opts = new ProjectOptions();
            opts.LoadSettings = ProjectLoadSettings.IgnoreMissingImports;
            project = Project.FromFile(file, opts);
        }

        public void DeleteFile(string file)
        {
            var items = project.Items.Where((item) => item.EvaluatedInclude == file);
            project.RemoveItems(items);
        }

        public void AddFileWithCopy(string file)
        {
            throw new Exception("None with Include could not be copied!");
            project.AddItem("None", file, new KeyValuePair<string, string>[]
            {
                new KeyValuePair<string, string>("CopyToOutputDirectory", "PreserveNewest"),
            });
        }

        public void AddFileWithTool(string file)
        {
            throw new Exception("None with Include could not use custom tool!");
        }

        public void Save()
        {
            project.Save();
        }
    }
}
