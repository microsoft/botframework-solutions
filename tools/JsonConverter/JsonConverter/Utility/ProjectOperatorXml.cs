// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

namespace JsonConverter.Utility
{
    public class ProjectOperatorXml : IProjectOperator
    {
        private readonly string file;
        private readonly XElement project;
        private XElement copyGroup, toolGroup;
        private readonly StringBuilder help;

        public ProjectOperatorXml(string file, StringBuilder help)
        {
            this.file = file;
            this.help = help;
            project = XElement.Load(this.file);
        }

        public void DeleteFile(string file)
        {
            foreach(var group in project.Elements("ItemGroup"))
            {
                var toDel = new List<XElement>();
                foreach(var ele in group.Elements())
                {
                    foreach(var label in new string[] { "Include", "Exclude", "Update", "Remove" })
                    {
                        var attri = ele.Attribute(label);
                        if (attri != null)
                        {
                            if (attri.Value == file)
                            {
                                toDel.Add(ele);
                                break;
                            }
                        }
                    }
                }
                foreach(var ele in toDel)
                {
                    ele.Remove();
                }
            }
        }

        public void AddFileWithCopy(string file)
        {
            AddInGroup(ref copyGroup, file, "CopyToOutputDirectory", "PreserveNewest");            
        }

        public void AddFileWithTool(string file)
        {
            AddInGroup(ref toolGroup, file, "Generator", "TextTemplatingFileGenerator");
        }

        public void Save()
        {
            project.Save(file);
            help.AppendLine("* Remove xml version in csproj");
        }

        private void AddInGroup(ref XElement group, string file, string childName, string childValue)
        {
            if (group == null)
            {
                group = new XElement("ItemGroup");
                project.Add(group);
            }

            var ele = new XElement("None");
            ele.SetAttributeValue("Update", file);
            var eleIn = new XElement(childName);
            eleIn.Value = childValue;
            ele.Add(eleIn);
            group.Add(ele);
        }
    }
}
