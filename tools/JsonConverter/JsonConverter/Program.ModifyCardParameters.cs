using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace JsonConverter
{
    partial class Program
    {
        public static void ModifyCardParameters(string rootFolder)
        {
            var cardFolder = Path.Combine(rootFolder, "Content");
            var jsonFiles = Directory.GetFiles(cardFolder, "*.json", SearchOption.AllDirectories);
            foreach (var file in jsonFiles)
            {
                string content;
                using (StreamReader sr = new StreamReader(file))
                {
                    content = sr.ReadToEnd();
                }

                string pattern = @"\{(\w+)\}";
                content = Regex.Replace(content, pattern, "@{Data.$1}");

                using (StreamWriter sw = new StreamWriter(file.Replace(".json", ".new.json")))
                {
                    sw.WriteLine(content);
                }
            }
        }
    }
}
