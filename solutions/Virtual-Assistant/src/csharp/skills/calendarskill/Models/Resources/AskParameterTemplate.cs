using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CalendarSkill.Models.Resources
{
    public static class AskParameterTemplate
    {
        private static Dictionary<string, AskParameterType> templateMapping;

        static AskParameterTemplate()
        {
            templateMapping = new Dictionary<string, AskParameterType>();
            var dir = Path.GetDirectoryName(typeof(AskParameterTemplate).Assembly.Location);
            var resDir = Path.Combine(dir, @"Models\Resources\AskParameterTemplate.txt");
            StreamReader sr = new StreamReader(resDir, Encoding.Default);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith("#"))
                {
                    continue;
                }

                string[] parts = line.Split("\t");
                if (parts[0].Length > 0 && parts[1].Length > 0)
                {
                    AskParameterType askParameterType = Enum.Parse<AskParameterType>(parts[0], true);
                    templateMapping.Add(parts[1], askParameterType);
                }
            }
        }

        public static List<AskParameterType> GetAskParameterTypes(string content)
        {
            List<AskParameterType> types = new List<AskParameterType>();
            if (string.IsNullOrEmpty(content))
            {
                return types;
            }
            foreach (string key in templateMapping.Keys)
            {
                Regex regex = new Regex(key);
                if (regex.IsMatch(content))
                {
                    types.Add(templateMapping[key]);
                }
            }

            return types;
        }
    }
}
