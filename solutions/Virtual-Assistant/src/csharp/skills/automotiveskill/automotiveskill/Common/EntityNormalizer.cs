// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Common
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    public class EntityNormalizer
    {
        private readonly IDictionary<string, string> map;

        public EntityNormalizer(Assembly resourceAssembly, string path)
        {
            using (StreamReader reader = new StreamReader(resourceAssembly.GetManifestResourceStream(path)))
            {
                List<string> lines = new List<string>();

                while (!reader.EndOfStream)
                {
                    lines.Add(reader.ReadLine());
                }

                map = lines
                    .Where(FilterIsBlankOrComment)
                    .Select(line => line.Split("\t"))
                    .ToDictionary(pair => PreProcess(pair[1]), pair => pair[0]);
            }
        }

        public string Normalize(string entity)
        {
            var formated = PreProcess(entity);
            if (map.TryGetValue(formated, out string norm))
            {
                return norm;
            }

            return formated;
        }

        public string NormalizeOrNull(string entity)
        {
            var preprocessed = PreProcess(entity);
            if (map.TryGetValue(preprocessed, out string normalized))
            {
                return normalized;
            }

            return null;
        }

        private static bool FilterIsBlankOrComment(string line)
        {
            return !(string.IsNullOrWhiteSpace(line) || line.StartsWith("#"));
        }

        private static string PreProcess(string text)
        {
            return text.Trim().ToLowerInvariant();
        }
    }
}